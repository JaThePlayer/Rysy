using Rysy;
using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Gui.Windows;
using Rysy.Mods;
using Rysy.Platforms;
using Rysy.Scenes;
using System;

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
    public static event Action? OnFrameEnd = null;

    private static bool _lastActive;
    public static event Action? OnLoseFocus = null;

    public static double Framerate;

    public static float ForceActiveTimer = 0.0f;

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
        if (Instance is { } instance)
            instance.TargetElapsedTime = TimeSpan.FromSeconds(1d / (double)fps);
    }

    public static void ToggleVSync(bool toggle) {
        if (GDM is { } gdm) {
            gdm.SynchronizeWithVerticalRetrace = toggle;
            gdm.ApplyChanges();
        }
    }

    private void Window_FileDrop(object? sender, FileDropEventArgs e) {
        Console.WriteLine(string.Join(' ', e.Files));

        Scene?.OnFileDrop(e);
    }

    public static event Action<Viewport>? OnViewportChanged;

    private void Window_ClientSizeChanged(object? sender, EventArgs e) {
        OnViewportChanged?.Invoke(GDM.GraphicsDevice.Viewport);

        if (Persistence.Instance is { }) {
            Persistence.Instance.Set("StartingWindowWidth", GDM.GraphicsDevice.Viewport.Width);
            Persistence.Instance.Set("StartingWindowHeight", GDM.GraphicsDevice.Viewport.Height);
            Persistence.Instance.Set("StartingWindowX", Window.Position.X);
            Persistence.Instance.Set("StartingWindowY", Window.Position.Y);
            Persistence.Save(Persistence.Instance);
        }
    }

    protected override async void Initialize() {
        base.Initialize();

        RysyPlatform.Current.Init();
        Logger.Init();
        ImGuiManager.Load(this);

        await ReloadAsync();
    }

    public async ValueTask ReloadAsync() {

        using var reloadTimer = new ScopedStopwatch("Loading");
        GFX.LoadEssencials(this);
        LoadingScene loading = new() { Text = "Loading settings" };
        Scene = loading;

        try {
            Settings.Instance = Settings.Load();
        } catch {
            return; // No point in loading any further. Error is already logged by .Load()
        }

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

        //ResizeWindow(Settings.Instance.StartingWindowWidth, Settings.Instance.StartingWindowHeight);
        ResizeWindow(
            Persistence.Instance.Get("StartingWindowWidth", 800),
            Persistence.Instance.Get("StartingWindowHeight", 480),
            Persistence.Instance.Get("StartingWindowX", Window.Position.X),
            Persistence.Instance.Get("StartingWindowY", Window.Position.Y)
        );

        if (Profile.Instance.CelesteDirectory is null or "") {
            var picker = new PickCelesteInstallScene(Scene);
            Scene = picker;
            await picker.AwaitInstallPickedAsync();
        }

        await ModRegistry.LoadAllAsync(Profile.Instance.ModsDirectory);

        await GFX.LoadAsync();

        await EntityRegistry.RegisterAsync();

        Scene = new EditorScene();
    }

    private void ResizeWindow(int w, int h, int x, int y) {
        OnFrameEnd += () => {
            GDM.PreferredBackBufferWidth = w;
            GDM.PreferredBackBufferHeight = h;
            GDM.ApplyChanges();
            Window.Position = new(x, Math.Max(32, y));
            Window_ClientSizeChanged(null, new());
        };
    }

    protected override void Update(GameTime gameTime) {
        base.Update(gameTime);

        if (_lastActive != IsActive) {
            OnLoseFocus?.Invoke();
        }

        if (IsActive) {
            Time.Update(gameTime);
            Input.Update(gameTime);

            SmartFPSHandler.Update();

            Scene.Update();

            if (OnFrameEnd is { } onFrameEnd) {
                OnFrameEnd = null;
                onFrameEnd.Invoke();
            }
        }

        _lastActive = IsActive;
    }

    protected override unsafe void Draw(GameTime gameTime) {
        if (IsActive || ForceActiveTimer > 0f) {
            ForceActiveTimer -= Time.Delta;
            base.Draw(gameTime);

            GraphicsDevice.Clear(Color.Black);

            ImGuiManager.GuiRenderer.BeforeLayout(gameTime);

            Scene.RenderImGui();
            Scene.Render();

            if (DebugInfoWindow.Enabled)
                DebugInfoWindow.Instance.RenderGui();
            ImGuiManager.GuiRenderer.AfterLayout();

            smartFramerate.Update(gameTime.ElapsedGameTime.TotalSeconds);

            Framerate = smartFramerate.framerate;

            
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