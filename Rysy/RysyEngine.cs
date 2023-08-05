using Rysy;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Gui.Windows;
using Rysy.Helpers;
using Rysy.Mods;
using Rysy.Platforms;
using Rysy.Scenes;
using Rysy.Selections;

public sealed class RysyEngine : Game {
    public static RysyEngine Instance { get; private set; } = null!;

    public static GraphicsDeviceManager GDM { get; private set; } = null!;

    private static Scene _scene = new BlankScene();
    public static Scene Scene {
        get => _scene;
        set {
            _scene = value;
            value.OnBegin();
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

    private static bool _lastActive;

    /// <summary>
    /// The current FPS
    /// </summary>
    public static double CurrentFPS { get; set; }

    public static float ForceActiveTimer { get; set; } = 0.0f;

    public static bool ImGuiAvailable { get; internal set; }

    public RysyEngine() {
        Instance = this;

        GDM = new GraphicsDeviceManager(this);

        Window.AllowUserResizing = true;
        IsMouseVisible = true;

        Window.ClientSizeChanged += Window_ClientSizeChanged;
        Window.FileDrop += Window_FileDrop;

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
            if (GDM is { } gdm) {
                gdm.SynchronizeWithVerticalRetrace = toggle;
                gdm.ApplyChanges();
            }
        };
    }

    public static void ToggleBorderlessFullscreen(bool toggle) {
        if (GDM is not { } gdm || Instance is not { } instance) {
            return;
        }

        OnEndOfThisFrame += () => {
            instance.Window.IsBorderless = toggle;
            if (toggle) {
                var monitorSize = GDM.GraphicsDevice.DisplayMode;

                instance.ResizeWindow(monitorSize.Width, monitorSize.Height, 0, 0);
            } else {
                instance.ResizeWindowUsingSettings();
            }
        };
    }

    private void Window_FileDrop(object? sender, FileDropEventArgs e) {
        Console.WriteLine(string.Join(' ', e.Files));

        Scene?.OnFileDrop(e);
    }

    public static Action<Viewport>? OnViewportChanged { get; set; }

    private void Window_ClientSizeChanged(object? sender, EventArgs e) {
        OnViewportChanged?.Invoke(GDM.GraphicsDevice.Viewport);

        if (Settings.Instance is { } settings && !Instance.Window.IsBorderless) {
            settings.StartingWindowWidth = GDM.GraphicsDevice.Viewport.Width;
            settings.StartingWindowHeight = GDM.GraphicsDevice.Viewport.Height;
            settings.StartingWindowX = Window.Position.X;
            settings.StartingWindowY = Window.Position.Y;
            settings.Save();
        }
    }

    protected override void Initialize() {
        base.Initialize();

        RysyPlatform.Current.Init();
        Logger.Init();
        ImGuiManager.Load(this);

        QueueReload();
    }

    /// <summary>
    /// Queues a full reload of Rysy
    /// </summary>
    public static void QueueReload() {
        OnEndOfThisFrame += () => {
            lock (Instance) {
                GFX.LoadEssencials(Instance);
                Scene = new LoadingScene();
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
        lock (this) {
            GFX.LoadEssencials(this);
            Scene = new LoadingScene();
        }
        var reloadTimer = new ScopedStopwatch("Loading");

        Logger.Write("Reload", LogLevel.Info, $"Staring full reload...");

        LoadingScene.Text = "Loading settings";
        try {
            Settings.Instance = Settings.Load();
        } catch {
            return; // No point in loading any further. Error is already logged by .Load()
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

        SelectionContextWindowRegistry.Init();

        await ModRegistry.LoadAllAsync(Profile.Instance.ModsDirectory);

        await GFX.LoadAsync();

        await EntityRegistry.RegisterAsync();

        await LangRegistry.LoadAllAsync();

        reloadTimer.Dispose();

        Scene = new EditorScene();
    }

    private void ResizeWindowUsingSettings() {
        if (Settings.Instance is not { } settings)
            return;

        ResizeWindow(
            settings.StartingWindowWidth ?? 800,
            settings.StartingWindowHeight ?? 480,
            settings.StartingWindowX ?? Window.Position.X,
            settings.StartingWindowY ?? Window.Position.Y
        );
    }

    private void ResizeWindow(int w, int h, int x, int y) {
        OnEndOfThisFrame += () => {
            GDM.PreferredBackBufferWidth = w;
            GDM.PreferredBackBufferHeight = h;
            GDM.ApplyChanges();

            var monitorSize = GDM.GraphicsDevice.DisplayMode;
            // just in case persistence got a messed up value, snap these back in range
            if (!x.IsInRange(0, monitorSize.Width - w - 32))
                x = 0;

            // todo: get rid of that hardcoded 32, though that's not easy cross-platform...
            var minY = Window.IsBorderless ? 0 : 32;
            if (!y.IsInRange(minY, monitorSize.Height - h - 32))
                y = 32;

            Window.Position = new(x, y);
            Window_ClientSizeChanged(null, new());
            GDM.ApplyChanges();
        };
    }

    protected override void Update(GameTime gameTime) {
        base.Update(gameTime);
        try {
            if (_lastActive != IsActive) {
                OnLoseFocus?.Invoke();
            }

            //IsActive
            if (true) {
                Time.Update(gameTime);

                if (IsActive)
                    Input.UpdateGlobal(gameTime);

                SmartFPSHandler.Update();

                Scene.Update();

                if (Scene is not CrashScene)
                    OnUpdate?.Invoke();
            }

            if (OnEndOfThisFrame is { } onFrameEnd) {
                OnEndOfThisFrame = null;
                onFrameEnd.Invoke();
            }
        } catch (Exception e) {
            Logger.Error(e, $"Unhandled exception during update!");
            Scene = new CrashScene(Scene, e);
        }

        _lastActive = IsActive;
    }

    protected override unsafe void Draw(GameTime gameTime) {
        base.Draw(gameTime);
        //IsActive || ForceActiveTimer > 0f
        if (true) {
            ForceActiveTimer -= Time.Delta;

            GraphicsDevice.Clear(Color.Black);

            ImGuiManager.GuiRenderer.BeforeLayout(gameTime);

            try {
                Scene.RenderImGui();
                Scene.Render();

                if (Scene is not CrashScene)
                    OnRender?.Invoke();

                if (DebugInfoWindow.Enabled)
                    DebugInfoWindow.Instance.RenderGui();
            } catch (Exception e) {
                Logger.Error(e, $"Unhandled exception during render!");
                Scene = new CrashScene(Scene, e);
            }

            ImGuiManager.GuiRenderer.AfterLayout();

            smartFramerate.Update(gameTime.ElapsedGameTime.TotalSeconds);

            CurrentFPS = smartFramerate.framerate;
        }
    }

    //https://stackoverflow.com/a/44689035
    class SmartFramerate {
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