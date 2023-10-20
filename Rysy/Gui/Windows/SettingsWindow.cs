using ImGuiNET;
using Rysy.Extensions;
using Rysy.Mods;
using Rysy.Platforms;

namespace Rysy.Gui.Windows;
public sealed class SettingsWindow : Window {
    private const string REQUIRES_RELOAD = "Requires a reload. Changing this value might immediately reload Rysy.";

    private SettingWindowData Data = new();

    private ModMeta? SelectedMod = null;

    private sealed class SettingWindowData {
        public bool ProfileSettingsChanged = false;
        public string[]? ProfileListDirectories = null;
        public string? ProfileCelesteDir;

        public string[]? ThemeList = null;

        public Dictionary<string, string>? EditedHotkeys = null;
    }

    public static void Add(Scene scene) {
        var wind = new SettingsWindow("Settings", new(720, 480));
        scene.AddWindow(wind);
    }

    protected override void Render() {
        if (ImGui.BeginTabBar("Tabbar")) {
            ProfileBar();
            GeneralBar();
            ModBar();
            VisualBar();
            ThemeBar();
            HotkeyBar();
            PerformanceBar();
            DebugBar();

            ImGui.EndTabBar();
        }
    }

    private void PerformanceBar() {
        if (!ImGui.BeginTabItem("Performance"))
            return;

        var b = Settings.Instance.ClearRenderCacheForOffScreenRooms;
        if (ImGuiManager.TranslatedCheckbox("rysy.settings.perf.clearRenderCacheForOffScreenRooms", ref b)) {
            Settings.Instance.ClearRenderCacheForOffScreenRooms = b;
            Settings.Instance.Save();
        }
    }

    private void VisualBar() {
        if (!ImGui.BeginTabItem("Visual"))
            return;

        var fps = Settings.Instance.TargetFps;
        if (ImGui.InputInt("Target FPS", ref fps, 10, 30, ImGuiInputTextFlags.EnterReturnsTrue).WithTooltip("The maximum FPS that Rysy will attempt to reach. Higher values increase CPU/GPU usage in exchange for smoother visuals.")) {
            Settings.Instance.TargetFps = fps.AtLeast(20);
            Settings.Instance.Save();
        }

        var b = Settings.Instance.VSync;
        if (ImGui.Checkbox("VSync", ref b).WithTooltip("Whether to use VSync or not")) {
            Settings.Instance.VSync = b;
            Settings.Instance.Save();
        }

        b = Settings.Instance.SmartFramerate;
        if (ImGui.Checkbox("Smart Framerate", ref b).WithTooltip("Reduces target FPS when you haven't moved the mouse or pressed any keys for 1 second, to reduce CPU/GPU usage.")) {
            Settings.Instance.SmartFramerate = b;
            Settings.Instance.Save();
        }

        b = Settings.Instance.ShowPlacementIcons;
        if (ImGui.Checkbox("Show Placement Icons", ref b).WithTooltip("Whether to show icons next to placements in the material list.")) {
            Settings.Instance.ShowPlacementIcons = b;
            Settings.Instance.Save();
        }

        var fullscreen = Settings.Instance.BorderlessFullscreen;
        if (ImGui.Checkbox("Borderless Fullscreen", ref fullscreen).WithTooltip("Toggles borderless fullscreen mode.")) {
            Settings.Instance.BorderlessFullscreen = fullscreen;
            Settings.Instance.Save();
        }

        ImGui.EndTabItem();
    }

    private void GeneralBar() {
        if (!ImGui.BeginTabItem("General"))
            return;

        var backups = Settings.Instance.MaxBackups;
        if (ImGui.InputInt("Max Backups", ref backups).WithTooltip("The maximum amount of backups stored at once. Once this limit is exceeded, old backups will get deleted.")) {
            Settings.Instance.MaxBackups = backups.AtLeast(0);
            Settings.Instance.Save();
        }

        var alpha = Settings.Instance.HiddenLayerAlpha;
        if (ImGui.InputFloat("Hidden Layer Alpha", ref alpha, step: 0.1f).WithTooltip("The alpha value used for tinting entities that are not in the currently visible editor layer.")) {
            Settings.Instance.HiddenLayerAlpha = alpha.SnapBetween(0f, 1f);
            Settings.Instance.Save();
        }

        var minifyClipboard = Settings.Instance.MinifyClipboard;
        if (ImGui.Checkbox("Minify Clipboard", ref minifyClipboard).WithTooltip("Minifies selections copied to the clipboard to reduce their size.")) {
            Settings.Instance.MinifyClipboard = minifyClipboard;
            Settings.Instance.Save();
        }

        ImGui.EndTabItem();
    }

    private void HotkeyBar() {
        if (!ImGui.BeginTabItem("Hotkeys"))
            return;

        var invalid = false;

        ImGuiManager.WithBottomBar(
            renderMain: () => {
                foreach (var (name, origHotkey) in Settings.Instance.Hotkeys) {
                    var hotkey = origHotkey;
                    if (Data.EditedHotkeys is { } h && h.TryGetValue(name, out var changed)) {
                        hotkey = changed;
                        invalid |= ImGuiManager.PushInvalidStyleIf(!HotkeyHandler.IsValid(hotkey));
                    }

                    if (ImGui.InputText(name.Humanize(), ref hotkey, 64)) {
                        var edited = Data.EditedHotkeys ??= new();

                        edited[name] = hotkey;
                    }
                    ImGuiManager.PopInvalidStyle();
                }
            },
            renderBottomBar: () => {
                ImGui.BeginDisabled(invalid || Data.EditedHotkeys is not { });

                if (ImGui.Button("Apply Changes") && Data.EditedHotkeys is { } hotkeys) {
                    foreach (var (name, newVal) in hotkeys) {
                        Settings.Instance.Hotkeys[name] = newVal;
                        Settings.Instance.Save();

                        RysyEngine.Scene.SetupHotkeys();
                    }
                    Data.EditedHotkeys = null;
                }
                ImGui.EndDisabled();
            }
        );

        ImGui.EndTabItem();
    }

    private void ThemeBar() {
        var windowData = Data;

        if (!ImGui.BeginTabItem("Themes")) {
            return;
        }

        var theme = Settings.Instance.Theme;

        windowData.ThemeList ??= Directory.EnumerateFiles("Assets/themes", "*.json").Select(f => Path.GetRelativePath("Assets/themes", f).TrimEnd(".json")).ToArray();

        if (ImGui.BeginCombo("Theme", theme)) {
            foreach (var themeName in windowData.ThemeList) {
                if (ImGui.Selectable(themeName)) {
                    Settings.Instance.Theme = themeName;
                    Settings.Instance.Save();

                    ImGuiThemer.LoadTheme(themeName);
                }
            }

            ImGui.EndCombo();
        }

        ImGui.Separator();

        var fontSize = Settings.Instance.FontSize;
        if (ImGui.InputInt("Font Size", ref fontSize)) {
            Settings.Instance.FontSize = fontSize;

            Settings.Instance.Save();
        }

        ImGui.EndTabItem();
    }

    private string ModBarSearch = "";
    private void ModBar() {
        if (!ImGui.BeginTabItem("Mods")) {
            return;
        }

        var readBlacklist = Settings.Instance.ReadBlacklist;
        if (ImGui.Checkbox("Read blacklist", ref readBlacklist).WithTooltip("Whether Rysy should read the Everest blacklist file.")) {
            Settings.Instance.ReadBlacklist = readBlacklist;
            Settings.Instance.Save();
        }

        ImGui.Separator();

        var modsWithSettings = ModRegistry.Mods.Where(m => m.Value.Settings?.HasAnyData() ?? false).ToDictionary(m => m.Value, m => m.Key);
        var mod = SelectedMod ?? modsWithSettings.First().Key;
        if (ImGuiManager.Combo("Selected Mod", ref mod, modsWithSettings, ref ModBarSearch)) {
            SelectedMod = mod;
        }

        ImGui.Text(mod.EverestYaml.First().ToString());

        if (ImGui.Button("rysy.settings.mods.editSettings.name".Translate()).WithTooltip("rysy.settings.mods.editSettings.tooltip")) {
            RysyEngine.Scene.AddWindow(new ModSettingsWindow(mod));
        }

        ImGui.EndTabItem();
    }

    private void DebugBar() {
        if (ImGui.BeginTabItem("Debug")) {
            var m = Settings.Instance.LogMissingEntities;
            if (ImGui.Checkbox("Log Missing Entities", ref m).WithTooltip("Logs any entities without Rysy plugins to the console.")) {
                Settings.Instance.LogMissingEntities = m;
                Settings.Instance.Save();
            }

            m = Settings.Instance.LogMissingTextures;
            if (ImGui.Checkbox("Log Missing Textures", ref m).WithTooltip("Logs any missing textures to the console")) {
                Settings.Instance.LogMissingTextures = m;
                Settings.Instance.Save();
            }

            m = Settings.Instance.LogTextureLoadTimes;
            if (ImGui.Checkbox("Log Texture Load Times", ref m).WithTooltip("Logs time spent loading textures in the background.")) {
                Settings.Instance.LogTextureLoadTimes = m;
                Settings.Instance.Save();
            }

            m = Settings.Instance.LogSpriteCachingTimes;
            if (ImGui.Checkbox("Log Sprite Caching Times", ref m).WithTooltip("Logs time spent calling GetSprites on entities during rendering.")) {
                Settings.Instance.LogSpriteCachingTimes = m;
                Settings.Instance.Save();
            }

            m = Settings.Instance.LogPreloadingTextures;
            if (ImGui.Checkbox("Log Texture Preloading", ref m).WithTooltip("Logs whenever a sprite has to be preloaded due to requesting its size before it finished lazily loading.")) {
                Settings.Instance.LogPreloadingTextures = m;
                Settings.Instance.Save();
            }


            ImGui.EndTabItem();
        }
    }

    private void ProfileBar() {
        if (!ImGui.BeginTabItem("Profile")) {
            return;
        }

        var windowData = Data;
        var celesteDir = windowData.ProfileCelesteDir;
        ImGuiManager.WithBottomBar(
            () => {
                if (ImGui.BeginCombo("Current Profile", Settings.Instance.Profile)) {
                    #region Profile Picker
                    var profileDir = $"{RysyPlatform.Current.GetSaveLocation()}/Profiles";
                    var dirs = windowData.ProfileListDirectories ??= Directory.GetDirectories(profileDir);
                    foreach (var dir in dirs) {
                        var name = Path.GetRelativePath(profileDir, dir);
                        if (ImGui.Selectable(name).WithTooltip(REQUIRES_RELOAD)) {
                            SetProfile(name, isNew: false);
                        }
                    }

                    if (ImGui.Button("New")) {
                        string text = "";
                        RysyEngine.Scene.AddWindow(new ScriptedWindow("New Profile Name", (w) => {
                            ImGui.InputText("Name", ref text, 64);
                            if (ImGui.Button("Create").WithTooltip(REQUIRES_RELOAD)) {
                                SetProfile(text, isNew: true);
                                w.RemoveSelf();
                            }
                        }, new(300, ImGui.GetFrameHeight() * 2 + ImGui.GetTextLineHeightWithSpacing() * 3)));
                    }
                    ImGui.EndCombo();
                    #endregion
                }

                ImGui.Separator();
                ImGui.Text("Profile Settings");

                ImGui.Checkbox("Show paths", ref ShowPaths);

                windowData.ProfileCelesteDir ??= Profile.Instance.CelesteDirectory;
                celesteDir = windowData.ProfileCelesteDir;
                if (ImGui.InputText("Celeste Install", ref celesteDir, 512, ShowPaths ? ImGuiInputTextFlags.None : ImGuiInputTextFlags.Password)) {
                    windowData.ProfileSettingsChanged = true;
                }
            },
            renderBottomBar: () => {
                ImGui.BeginDisabled(!windowData.ProfileSettingsChanged);
                if (ImGui.Button("Apply Changes")) {
                    Profile.Instance.CelesteDirectory = celesteDir!;
                    Profile.Instance.Save();

                    QueueReload();
                }
                ImGui.EndDisabled();
            }
            );
        

        ImGui.EndTabItem();
    }

    private static bool ShowPaths;

    public SettingsWindow(string name, NumVector2? size = null) : base(name, size) {
    }

    private static void SetProfile(string name, bool isNew) {
        Settings.Instance.Profile = name;
        Settings.Save(Settings.Instance);

        if (isNew) {
            var profile = new Profile();
            profile.Save();
            Profile.Instance = profile;
            Persistence.Instance = new();
            Persistence.Save(Persistence.Instance);
        }

        QueueReload();
    }

    private static void QueueReload() {
        RysyEngine.QueueReload();
    }
}
