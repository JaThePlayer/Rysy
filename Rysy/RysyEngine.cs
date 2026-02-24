using Rysy.Components;
using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Loading;
using Rysy.Platforms;
using Rysy.Scenes;
using Rysy.Signals;

namespace Rysy;

public sealed class RysyEngine : Game, ISignalListener<SettingsChanged<int>>, ISignalListener<SettingsChanged<bool>> {
    public RysyState State { get; }
    
    public static RysyEngine Instance { get; private set; } = null!;

    public static Version Version { get; } = typeof(RysyEngine).Assembly.GetName().Version ?? new Version(0, 0);

    public static Scene Scene {
        get => RysyState.Scene;
        set => RysyState.Scene = value;
    }

    public RysyEngine(ComponentRegistry globalComponents) {
        State = globalComponents.GetRequired<RysyState>();
        Instance = this;
        Window.AllowUserResizing = true;

        var gdm = new GraphicsDeviceManager(this);
        State.Initialize(this, gdm, globalComponents);
        
        IsMouseVisible = true;

        SetTargetFps(60);
        ToggleVSync(false);
        IsFixedTimeStep = true;
    }

    public static void SetTargetFps(int fps) {
        if (Instance is not { } instance) {
            return;
        }

        RysyState.OnEndOfThisFrame += () => {
            instance.TargetElapsedTime = TimeSpan.FromSeconds(1d / (double) fps);
        };
    }

    public static void ToggleVSync(bool toggle) {
        RysyState.OnEndOfThisFrame += () => {
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
        RysyState.OnEndOfThisFrame += () => {
            RysyState.GraphicsDeviceManager.IsFullScreen = toggle;
            RysyState.GraphicsDeviceManager.ApplyChanges();
            instance.Window.IsBorderlessEXT = toggle;
        };
#endif
    }

    protected override void Initialize() {
        base.Initialize();
        
        Logger.Write("Rysy", LogLevel.Info, $"Starting Rysy {Version}");

        RysyPlatform.Current.Init();

        QueueReload();
    }

    /// <summary>
    /// Queues a full reload of Rysy
    /// </summary>
    public static void QueueReload() {
        RysyState.OnEndOfThisFrame += () => {
            lock (Instance) {
                Gfx.LoadEssencials();
                //Scene = new LoadingScene();
            }

            Task.Run(async () => {
                try {
                    await Instance.ReloadAsync();
                } catch (Exception e) {
                    Logger.Error("Reload", e, $"Unhandled exception during (re)load!");
                    Scene = new CrashScene(Scene, e);
                }
            });
        };
    }

    private async ValueTask ReloadAsync() {
#pragma warning disable CA2000
        var reloadTimer = new ScopedStopwatch("Loading");
#pragma warning restore CA2000
        
        var loadTasks = new LoadTaskManager([
            new SimpleLoadTask("Load Settings", t => LoadSettingsTask(State.GlobalComponents, t)),
            new SimpleLoadTask("Load Mods", t => DefaultLoadTasks.LoadMods(State.GlobalComponents, t)),
            new SimpleLoadTask("Load Theme", t => DefaultLoadTasks.LoadTheme(State.GlobalComponents, t)),
            new ParallelLoadTask("Load Assets", [
                new SimpleLoadTask("Load GFX", DefaultLoadTasks.LoadGfx),
                new SimpleLoadTask("Load Entities", t => DefaultLoadTasks.LoadEntities(t)),
                new SimpleLoadTask("Load Lang Files", DefaultLoadTasks.LoadLangFiles),
            ]),
            new SimpleLoadTask("Load Decal Registry", DefaultLoadTasks.LoadDecalRegistry),
            new SimpleLoadTask("Call OnNextReload", DefaultLoadTasks.CallOnNextReload),
            new SimpleLoadTask("Initialize SelectionContextWindowRegistry", DefaultLoadTasks.InitializeSelectionContextWindowRegistry),
            new SimpleLoadTask("Load Map from Persistence", t => {
                reloadTimer.Dispose();
                return DefaultLoadTasks.LoadMapFromPersistence(t);
            }),
        ]);

        Logger.Write("Reload", LogLevel.Info, $"Staring full reload...");
        
        lock (this) {
            Gfx.LoadEssencials();
            Scene = new LoadingScene(loadTasks, onCompleted: () => {

            });
        }
    }

    private async Task<LoadTaskResult> LoadSettingsTask(IComponentRegistry globalComponents, SimpleLoadTask task) {
        task.SetMessage("Loading settings");
        try {
            Settings.Instance = Settings.Load();
        } catch (Exception ex) {
            return LoadTaskResult.Error(ex); // No point in loading any further. Error is already logged by .Load()
        }
        globalComponents.Add(Settings.Instance);

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
        
        // Initialize Imgui now that we have settings
        if (RysyPlatform.Current.SupportImGui && !RysyState.ImGuiAvailable)
            ImGuiManager.Load(globalComponents);

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
        RysyState.OnEndOfThisFrame += () => {
            RysyPlatform.Current.ResizeWindow(x, y, w, h);
            State.Window_ClientSizeChanged(null, null!);
        };
    }

    protected override void Update(GameTime gameTime) {
        base.Update(gameTime);
        State.DispatchUpdate((float) gameTime.ElapsedGameTime.TotalSeconds);
    }

    protected override void Draw(GameTime gameTime) {
        base.Draw(gameTime);
        //IsActive || ForceActiveTimer > 0f
        if (true) {
            State.DispatchRender((float) gameTime.ElapsedGameTime.TotalSeconds);
        }
    }

    public void OnSignal(SettingsChanged<int> signal) {
        switch (signal.SettingName) {
            case nameof(Settings.TargetFps):
                SetTargetFps(signal.Value);
                break;
        }
    }

    public void OnSignal(SettingsChanged<bool> signal) {
        switch (signal.SettingName) {
            case nameof(Settings.VSync):
                ToggleVSync(signal.Value);
                break;
            case nameof(Settings.BorderlessFullscreen):
                ToggleBorderlessFullscreen(signal.Value);
                break;
        }
    }
}