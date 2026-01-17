//#define NO_CATCH_RENDER_EXCEPTIONS

using Rysy.Gui;
using Rysy.Gui.Windows;
using Rysy.Scenes;
#if FNA
using Rysy.Helpers;
using SDL2;
using System.Runtime.InteropServices;
using System.Text;
#endif

namespace Rysy;

/// <summary>
/// Contains XNA state such as the current Game instance or GraphicsDevice.
/// </summary>
public static class RysyState {
    public static GraphicsDeviceManager GraphicsDeviceManager { get; private set; } = null!;
    
    public static Game Game { get; private set; }

    public static GraphicsDevice GraphicsDevice => GraphicsDeviceManager.GraphicsDevice;

    public static GameWindow Window => Game.Window;

    private static readonly Lock SceneChangeLock = new();

    public static Scene Scene {
        get;
        set {
            lock (SceneChangeLock) {
                var persistedWindows = field.ActiveWindows.Where(w => w.PersistBetweenScenes).ToList();
                field.OnEnd();
                foreach (var w in persistedWindows) {
                    value.AddWindow(w);
                }

                value.OnBegin();
                field = value;
            }
        }
    } = new BlankScene();

    #region Events
    /// <summary>
    /// Action that will be called on the UI thread once this frame ends.
    /// Afterwards, this will be set to null.
    /// </summary>
    public static event Action? OnEndOfThisFrame = null;

    /// <summary>
    /// Register an action that will be called on the UI thread once this frame ends.
    /// Used to avoid capturing loop locals into capture groups if the event registration is conditional.
    /// </summary>
    public static void RegisterOnEndOfThisFrame<TState>(TState state, Action<TState> action) {
        OnEndOfThisFrame += () => action.Invoke(state);
    }
    
    /// <inheritdoc cref="RegisterOnEndOfThisFrame{TState}(TState,System.Action{TState})"/>
    public static void RegisterOnEndOfThisFrame<TState1, TState2>(TState1 state1, TState2 state2, Action<TState1, TState2> action) {
        OnEndOfThisFrame += () => action.Invoke(state1, state2);
    }

    /// <summary>
    /// Called when the window loses focus.
    /// </summary>
    public static event Action? OnLoseFocus = null;

    /// <summary>
    /// Called each frame in the Update method.
    /// </summary>
    public static event Action? OnUpdate = null;

    /// <summary>
    /// Called each frame in the Render method, after the scene gets rendered, but before ImGui.
    /// </summary>
    public static event Action? OnRender = null;

    public static event Action? OnNextReload;
    
    public static Action<Viewport>? OnViewportChanged { get; set; }
    #endregion
    
    /// <summary>
    /// The current FPS
    /// </summary>
    public static double CurrentFps { get; set; }

    public static float ForceActiveTimer { get; set; } = 0.0f;

    public static bool ImGuiAvailable { get; internal set; }

    public static CommandlineArguments CmdArguments { get; internal set; } = new([]);
    
    private static bool _hideUi;
    private static bool _lastActive;
    private static SmartFramerate _smartFramerate = new(5);
    
    public static void Initialize(Game game, GraphicsDeviceManager gdm) {
        Game = game;
        GraphicsDeviceManager = gdm;

        EnableEventListeners();

        Window.ClientSizeChanged += Window_ClientSizeChanged;
    }
    
    internal static void Window_ClientSizeChanged(object? sender, EventArgs e) {
        OnViewportChanged?.Invoke(GraphicsDevice.Viewport);

        if (Settings.Instance is { } settings && !Window.IsBorderlessShared()) {
            settings.StartingWindowWidth = GraphicsDevice.Viewport.Width;
            settings.StartingWindowHeight = GraphicsDevice.Viewport.Height;
            settings.StartingWindowX = Window.GetPosition().X;
            settings.StartingWindowY = Window.GetPosition().Y;
            settings.Save();
        }
    }
    
    public static void DispatchUpdate(float elapsed) {
        try {
            if (_lastActive != Game.IsActive) {
                OnLoseFocus?.Invoke();
            }

            if (true) {
                Time.Update(elapsed);

                if (Game.IsActive)
                    Input.Global.Update(elapsed);

                // todo: refactor into proper keybind
                if (Input.Global.Keyboard.IsKeyClicked(Microsoft.Xna.Framework.Input.Keys.F12)) {
                    _hideUi = !_hideUi; 
                }

                SmartFpsHandler.Update();

                Scene.Update();

                if (Scene is not CrashScene)
                    OnUpdate?.Invoke();
            }

            if (OnEndOfThisFrame is { } onFrameEnd) {
                lock (OnEndOfThisFrame) {
                    OnEndOfThisFrame = null;
                    onFrameEnd.Invoke();
                }

            }
        } catch (Exception e) {
            Logger.Error(e, $"Unhandled exception during update!");
            Scene = new CrashScene(Scene, e);
        }

        _lastActive = Game.IsActive;
    }

    public static void DispatchRender(float elapsed) {
        ForceActiveTimer -= Time.Delta;

        GraphicsDevice.Clear(Color.Black);

        var renderUi = !_hideUi;
        var uiRenderingSuccessful = false;
        
        if (ImGuiAvailable) {
#if !NO_CATCH_RENDER_EXCEPTIONS
            try {
#endif
                ImGuiManager.GuiRenderer.BeforeLayout(elapsed);
                if (renderUi)
                    Scene.RenderImGui();
                if (DebugInfoWindow.Enabled)
                    DebugInfoWindow.Instance.RenderGui();

                uiRenderingSuccessful = true;
#if !NO_CATCH_RENDER_EXCEPTIONS
            } catch (Exception e) {
                Logger.Error(e, $"Unhandled exception during ImGui rendering!");
                Scene = new CrashScene(Scene, e);
            }
#endif
        }

        try {
            Scene.Render();

            if (Scene is not CrashScene)
                OnRender?.Invoke();
        } catch (Exception e) {
            Logger.Error(e, $"Unhandled exception during render!");
            Scene = new CrashScene(Scene, e);
        }
        
        if (ImGuiAvailable)
            ImGuiManager.GuiRenderer.AfterLayout();

        _smartFramerate.Update(elapsed);

        CurrentFps = _smartFramerate.Framerate;
    }

    public static void DispatchOnNextReload() {
        if (OnNextReload is { } onNextReload) {
            OnNextReload = null;
            onNextReload.Invoke();
        }
    }

    internal static Vector2 TouchpadPan;

    internal static readonly DateTime[] MouseDoubleClicks = new DateTime[8];
    internal static readonly Vector2[] LastMouseClickPoints = new Vector2[8];

    private static void EnableEventListeners() {
#if !FNA
        Window.FileDrop += (object? sender, FileDropEventArgs e) {
            Console.WriteLine(string.Join(' ', e.Files));

            foreach (var file in e.Files) {
                Scene?.OnFileDrop(file);
            }
        }
#else
        unsafe {
            SDL.SDL_AddEventWatch(MyEventFunction, IntPtr.Zero);
        
            static int MyEventFunction(IntPtr userdata, IntPtr sdlEventPtr) {
                var sdlEvent = (SDL.SDL_Event*) sdlEventPtr;
                switch (sdlEvent->type)
                {
                    case SDL.SDL_EventType.SDL_DROPFILE:
                    {
                        var droppedFileDir = sdlEvent->drop.file;
                        var pathSpanUtf8 = MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)droppedFileDir);
                        var pathString = Encoding.UTF8.GetString(pathSpanUtf8);
                        Sdl2Ext.SDL_free(droppedFileDir);

                        Scene?.OnFileDrop(pathString);
                        break;
                    }
                    case SDL.SDL_EventType.SDL_MOUSEWHEEL: {
                        var wheel = sdlEvent->wheel;
                        
                        // TODO: test on linux

                        // XNA does not expose horizontal mouse wheel.
                        // We'll use this event instead to store precise wheel state for later use
                        // in detecting touch pad panning, which Windows exposes as mouse wheel movement.
                        if (!float.IsInteger(wheel.preciseX)) {
                            TouchpadPan.X = wheel.preciseX;
                        }
                        if (!float.IsInteger(wheel.preciseY)) {
                            TouchpadPan.Y = wheel.preciseY;
                        }
                        break;
                    }
                    case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN: {
                        var button = sdlEvent->button;
                        
                        var rysyButtonId = button.button switch {
                            1 => 0, // left
                            3 => 1, // right
                            _ => int.MaxValue
                        };

                        if (rysyButtonId == int.MaxValue)
                            break;

                        if (button.clicks % 2 == 0) {
                           var last = LastMouseClickPoints[rysyButtonId];

                           if (Vector2.DistanceSquared(last, new(button.x, button.y)) <= 10f) {
                               MouseDoubleClicks[rysyButtonId] = DateTime.Now;
                           }
                        }
                        
                        LastMouseClickPoints[rysyButtonId] = new (button.x, button.y);
                        break;
                    }
                        /*
                    default:
                        if (sdlEvent->type is SDL.SDL_EventType.SDL_POLLSENTINEL
                            or SDL.SDL_EventType.SDL_MOUSEMOTION
                            or SDL.SDL_EventType.SDL_WINDOWEVENT)
                            break;
                        Console.WriteLine(sdlEvent->type);
                        if (sdlEvent->type == SDL.SDL_EventType.SDL_MOUSEBUTTONUP) {
                            
                        }
                        break;
                        */
                }
                
                return 0; // Value will be ignored
            }
        }
#endif
    }
    
    //https://stackoverflow.com/a/44689035
    sealed class SmartFramerate {
        double _currentFrametimes;
        double _weight;
        int _numerator;

        public double Framerate => (_numerator / _currentFrametimes);

        public SmartFramerate(int oldFrameWeight) {
            _numerator = oldFrameWeight;
            _weight = (double) oldFrameWeight / ((double) oldFrameWeight - 1d);
        }

        public void Update(double timeSinceLastFrame) {
            _currentFrametimes /= _weight;
            _currentFrametimes += timeSinceLastFrame;
        }
    }
}