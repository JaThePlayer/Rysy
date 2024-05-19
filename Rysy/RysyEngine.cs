using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Gui.Windows;
using Rysy.Helpers;
using Rysy.Loading;
using Rysy.Mods;
using Rysy.Platforms;
using Rysy.Scenes;
using Rysy.Selections;
#if FNA
using SDL2;
using System.Runtime.InteropServices;
using System.Text;
#endif

namespace Rysy;

public sealed class RysyEngine : Game {
    public static RysyEngine Instance { get; private set; } = null!;

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

    private static SmartFramerate smartFramerate = new(5);

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

    private static bool _lastActive;

    /// <summary>
    /// The current FPS
    /// </summary>
    public static double CurrentFPS { get; set; }

    public static float ForceActiveTimer { get; set; } = 0.0f;

    public static bool ImGuiAvailable { get; internal set; }

    public RysyEngine() {
        Instance = this;
        Window.AllowUserResizing = true;

        var gdm = new GraphicsDeviceManager(this);
        RysyState.GraphicsDeviceManager = gdm;
        RysyState.Game = this;
        
        IsMouseVisible = true;

        Window.ClientSizeChanged += Window_ClientSizeChanged;

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

        SetTargetFps(60);
        ToggleVSync(false);
        IsFixedTimeStep = true;
    }

    public static void SetTargetFps(int fps) {
        if (Instance is not { } instance) {
            return;
        }

        OnEndOfThisFrame += () => {
            instance.TargetElapsedTime = TimeSpan.FromSeconds(1d / (double) fps);
        };
    }

    public static void ToggleVSync(bool toggle) {
        OnEndOfThisFrame += () => {
            if (RysyState.GraphicsDeviceManager is { } gdm) {
                gdm.SynchronizeWithVerticalRetrace = toggle;
                gdm.ApplyChanges();
            }
        };
    }

    public static void ToggleBorderlessFullscreen(bool toggle) {
        if (RysyState.GraphicsDeviceManager is not { } gdm || Instance is not { } instance) {
            return;
        }

#if !FNA
        OnEndOfThisFrame += () => {
            instance.Window.IsBorderless = toggle;
            if (toggle) {
                // ??? needed to make mouse position correct
                GDM.HardwareModeSwitch = false;
                GDM.IsFullScreen = true;
                GDM.ApplyChanges();
            } else {
                // ??? needed to properly regain the border...
                GDM.IsFullScreen = false;
                GDM.HardwareModeSwitch = true;
                GDM.ApplyChanges();
                instance.Window.IsBorderless = false;
                GDM.ApplyChanges();
                instance.ResizeWindowUsingSettings();
            }
        };
#else
        OnEndOfThisFrame += () => {
            RysyState.GraphicsDeviceManager.IsFullScreen = toggle;
            RysyState.GraphicsDeviceManager.ApplyChanges();
            instance.Window.IsBorderlessEXT = toggle;
        };
#endif
    }

    public static Action<Viewport>? OnViewportChanged { get; set; }

    private void Window_ClientSizeChanged(object? sender, EventArgs e) {
        OnViewportChanged?.Invoke(RysyState.GraphicsDevice.Viewport);

        if (Settings.Instance is { } settings && !RysyState.Window.IsBorderlessShared()) {
            settings.StartingWindowWidth = RysyState.GraphicsDevice.Viewport.Width;
            settings.StartingWindowHeight = RysyState.GraphicsDevice.Viewport.Height;
            settings.StartingWindowX = Window.GetPosition().X;
            settings.StartingWindowY = Window.GetPosition().Y;
            settings.Save();
        }
    }

    protected override void Initialize() {
        base.Initialize();

        RysyPlatform.Current.Init();
        Logger.Init();
        ImGuiManager.Load();

        QueueReload();
    }

    /// <summary>
    /// Queues a full reload of Rysy
    /// </summary>
    public static void QueueReload() {
        OnEndOfThisFrame += () => {
            lock (Instance) {
                GFX.LoadEssencials();
                //Scene = new LoadingScene();
            }

            Task.Run(async () => {
                try {
                    await Instance.ReloadAsync();
                } catch (Exception e) {
                    Logger.Error(e, $"Unhandled exception during (re)load!");
                    Scene = new CrashScene(Scene, e);
                }
            });
        };
    }

    private async ValueTask ReloadAsync() {
        var reloadTimer = new ScopedStopwatch("Loading");
        
        var loadTasks = new LoadTaskManager([
            new SimpleLoadTask("Load Settings", LoadSettingsTask),
            new SimpleLoadTask("Load Mods", LoadModsTask),
            new ParallelLoadTask("Load Assets", [
                new SimpleLoadTask("Load GFX", LoadGfxTask),
                new SimpleLoadTask("Load Entities", LoadEntitiesTask),
                new SimpleLoadTask("Load Lang Files", LoadLangFilesTask),
            ]),
            new SimpleLoadTask("Load Decal Registry", LoadDecalRegistryTask),
            new SimpleLoadTask("Call OnNextReload", task => {
                if (OnNextReload is { } onNextReload) {
                    task.SetMessage("Calling OnReload");
                    OnNextReload = null;
                    onNextReload.Invoke();
                }

                return Task.FromResult(LoadTaskResult.Success());
            }),
            new SimpleLoadTask("Load Map from Persistence", async (t) => {
                SelectionContextWindowRegistry.Init();
                reloadTimer.Dispose();
                var editor = new EditorScene();
                await editor.LoadFromPersistence();
                
                Scene = editor;
                
                return LoadTaskResult.Success();
            }),
        ]);

        Logger.Write("Reload", LogLevel.Info, $"Staring full reload...");
        
        lock (this) {
            GFX.LoadEssencials();
            Scene = new LoadingScene(loadTasks, onCompleted: () => {

            });
        }
    }

    private async Task<LoadTaskResult> LoadSettingsTask(SimpleLoadTask task) {
        task.SetMessage("Loading settings");
        try {
            Settings.Instance = Settings.Load();
        } catch (Exception ex) {
            return LoadTaskResult.Error(ex); // No point in loading any further. Error is already logged by .Load()
        }

        ResizeWindowUsingSettings();

        try {
            Profile.Instance = Profile.Load();
        } catch {
            Profile.Instance = new Profile().Save();
        }

        try {
            Persistence.Instance = Persistence.Load();
        } catch {
            // persistence will be `new()`'d up. Oh well.
            Persistence.Instance = Persistence.Save(new());
        }

        var celesteDir = Profile.Instance.CelesteDirectory;
        if (!string.IsNullOrWhiteSpace(celesteDir) && !Path.Exists(celesteDir)) {
            Profile.Instance.CelesteDirectory = "";
        }

        if (Profile.Instance.CelesteDirectory is null or "") {
            var picker = new PickCelesteInstallScene(Scene);
            Scene = picker;
            await picker.AwaitInstallPickedAsync();
        }

        return LoadTaskResult.Success();
    }

    private async Task<LoadTaskResult> LoadModsTask(SimpleLoadTask task) {
        await ModRegistry.LoadAllAsync(Profile.Instance.ModsDirectory, task);
        
        return LoadTaskResult.Success();
    }
    
    private async Task<LoadTaskResult> LoadGfxTask(SimpleLoadTask task) {
        await GFX.LoadAsync(task);
        
        return LoadTaskResult.Success();
    }
    
    private Task<LoadTaskResult> LoadDecalRegistryTask(SimpleLoadTask task) {
        GFX.LoadDecalRegistry(task);
        
        return Task.FromResult(LoadTaskResult.Success());
    }
    
    private async Task<LoadTaskResult> LoadEntitiesTask(SimpleLoadTask task) {
        await EntityRegistry.RegisterAsync(task: task);
        
        return LoadTaskResult.Success();
    }
    
    private async Task<LoadTaskResult> LoadLangFilesTask(SimpleLoadTask task) {
        await LangRegistry.LoadAllAsync(task);
        
        return LoadTaskResult.Success();
    }

    private void ResizeWindowUsingSettings() {
        if (Settings.Instance is not { } settings)
            return;

        ResizeWindow(
            settings.StartingWindowWidth ?? 800,
            settings.StartingWindowHeight ?? 480,
            settings.StartingWindowX ?? Window.GetPosition().X,
            settings.StartingWindowY ?? Window.GetPosition().Y
        );
    }

    private void ResizeWindow(int w, int h, int x, int y) {
        OnEndOfThisFrame += () => {
            RysyPlatform.Current.ResizeWindow(x, y, w, h);
        };
    }

    protected override void Update(GameTime gameTime) {
        base.Update(gameTime);
        DispatchUpdate((float) gameTime.ElapsedGameTime.TotalSeconds);
    }

    public static void DispatchUpdate(float elapsed) {
        try {
            if (_lastActive != RysyState.Game.IsActive) {
                OnLoseFocus?.Invoke();
            }

            if (true) {
                Time.Update(elapsed);

                if (RysyState.Game.IsActive)
                    Input.Global.Update(elapsed);

                // todo: refactor into proper keybind
                if (Input.Global.Keyboard.IsKeyClicked(Microsoft.Xna.Framework.Input.Keys.F12)) {
                    HideUI = !HideUI; 
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

        _lastActive = RysyState.Game.IsActive;
    }

    protected override unsafe void Draw(GameTime gameTime) {
        base.Draw(gameTime);
        //IsActive || ForceActiveTimer > 0f
        if (true) {
            ForceActiveTimer -= Time.Delta;

            GraphicsDevice.Clear(Color.Black);

            var renderUI = !ShouldHideUI();


            try {
                ImGuiManager.GuiRenderer.BeforeLayout((float)gameTime.ElapsedGameTime.TotalSeconds);
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

            smartFramerate.Update(gameTime.ElapsedGameTime.TotalSeconds);

            CurrentFPS = smartFramerate.framerate;
        }
    }

    private static bool HideUI = false;
    private bool ShouldHideUI() => HideUI;

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