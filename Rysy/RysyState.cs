
using Rysy.Gui;
using Rysy.Gui.Windows;
using Rysy.Scenes;
#if FNA
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
    
    private static Scene _scene = new BlankScene();
    public static Scene Scene {
        get => _scene;
        set {
            lock (_scene) {
                _scene?.OnEnd();
                value.OnBegin();
                _scene = value;
            }
        }
    }
    
    #region Events
    /// <summary>
    /// Action that will be called on the UI thread once this frame ends.
    /// Afterwards, this will be set to null.
    /// </summary>
    public static event Action? OnEndOfThisFrame = null;

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
    #endregion

    /// <summary>
    /// The current FPS
    /// </summary>
    public static double CurrentFPS { get; set; }

    public static float ForceActiveTimer { get; set; } = 0.0f;

    public static bool ImGuiAvailable { get; internal set; }
    
    private static bool _hideUi;
    private static bool _lastActive;
    private static SmartFramerate _smartFramerate = new(5);
    
    public static void Initialize(Game game, GraphicsDeviceManager gdm) {
        Game = game;
        GraphicsDeviceManager = gdm;

        EnableFileDrop();
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

                SmartFPSHandler.Update();

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

        var renderUI = !_hideUi;
        
        try {
            ImGuiManager.GuiRenderer.BeforeLayout(elapsed);
            if (renderUI)
                Scene.RenderImGui();
            if (DebugInfoWindow.Enabled)
                DebugInfoWindow.Instance.RenderGui();
            Scene.Render();

            /*
            if (renderUI) {
                GFX.BeginBatch();
                PicoFont.Print(CurrentFPS.ToString("FPS:0", CultureInfo.CurrentCulture), new Vector2(4, 68), Color.Pink, 4);
                GFX.EndBatch();
            }*/

            if (Scene is not CrashScene)
                OnRender?.Invoke();

            ImGuiManager.GuiRenderer.AfterLayout();
        } catch (Exception e) {
            Logger.Error(e, $"Unhandled exception during render!");
            Scene = new CrashScene(Scene, e);
        }

        _smartFramerate.Update(elapsed);

        CurrentFPS = _smartFramerate.framerate;
    }

    public static void DispatchOnNextReload() {
        if (OnNextReload is { } onNextReload) {
            OnNextReload = null;
            onNextReload.Invoke();
        }
    }

    private static void EnableFileDrop() {
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
                if (sdlEvent->type == SDL.SDL_EventType.SDL_DROPFILE) {
                    var droppedFileDir = sdlEvent->drop.file;
                    var pathSpanUtf8 = MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)droppedFileDir);
                    var pathString = Encoding.UTF8.GetString(pathSpanUtf8);
                    SDL_free(droppedFileDir);
                    
                    Console.WriteLine(pathString);
                    Scene?.OnFileDrop(pathString);
                }
                
                return 0; // Value will be ignored
            }
            
            [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
            static extern void SDL_free(IntPtr memblock);
        }
#endif
    }
    
    //https://stackoverflow.com/a/44689035
    sealed class SmartFramerate {
        double currentFrametimes;
        double weight;
        int numerator;

        public double framerate => (numerator / currentFrametimes);

        public SmartFramerate(int oldFrameWeight) {
            numerator = oldFrameWeight;
            weight = (double) oldFrameWeight / ((double) oldFrameWeight - 1d);
        }

        public void Update(double timeSinceLastFrame) {
            currentFrametimes /= weight;
            currentFrametimes += timeSinceLastFrame;
        }
    }
}