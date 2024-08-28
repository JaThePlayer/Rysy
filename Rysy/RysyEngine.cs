using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Gui.Windows;
using Rysy.Helpers;
using Rysy.Loading;
using Rysy.Mods;
using Rysy.Platforms;
using Rysy.Scenes;
using Rysy.Selections;

namespace Rysy;

public sealed class RysyEngine : Game {
    public static RysyEngine Instance { get; private set; } = null!;

    public static Scene Scene {
        get => RysyState.Scene;
        set => RysyState.Scene = value;
    }

    public RysyEngine() {
        Instance = this;
        Window.AllowUserResizing = true;

        var gdm = new GraphicsDeviceManager(this);
        RysyState.Initialize(this, gdm);
        
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

        RysyPlatform.Current.Init();
        //Logger.Init();
        if (RysyPlatform.Current.SupportImGui)
            ImGuiManager.Load();

        QueueReload();
    }

    /// <summary>
    /// Queues a full reload of Rysy
    /// </summary>
    public static void QueueReload() {
        RysyState.OnEndOfThisFrame += () => {
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
            new SimpleLoadTask("Load Mods", t => DefaultLoadTasks.LoadMods(t)),
            new ParallelLoadTask("Load Assets", [
                new SimpleLoadTask("Load GFX", DefaultLoadTasks.LoadGfx),
                new SimpleLoadTask("Load Entities", t => DefaultLoadTasks.LoadEntities(t)),
                new SimpleLoadTask("Load Lang Files", DefaultLoadTasks.LoadLangFiles),
            ]),
            new SimpleLoadTask("Load Decal Registry", DefaultLoadTasks.LoadDecalRegistry),
            new SimpleLoadTask("Call OnNextReload", DefaultLoadTasks.CallOnNextReload),
            new SimpleLoadTask("Initiale SelectionContextWindowRegistry", DefaultLoadTasks.InitializeSelectionContextWindowRegistry),
            new SimpleLoadTask("Load Map from Persistence", t => {
                reloadTimer.Dispose();
                return DefaultLoadTasks.LoadMapFromPersistence(t);
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
            RysyState.Window_ClientSizeChanged(null, null!);
        };
    }

    protected override void Update(GameTime gameTime) {
        base.Update(gameTime);
        RysyState.DispatchUpdate((float) gameTime.ElapsedGameTime.TotalSeconds);
    }

    protected override unsafe void Draw(GameTime gameTime) {
        base.Draw(gameTime);
        //IsActive || ForceActiveTimer > 0f
        if (true) {
            RysyState.DispatchRender((float) gameTime.ElapsedGameTime.TotalSeconds);
        }
    }
}