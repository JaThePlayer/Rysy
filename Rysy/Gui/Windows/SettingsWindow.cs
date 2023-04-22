using ImGuiNET;
using Rysy.Extensions;
using Rysy.Platforms;

namespace Rysy.Gui.Windows;
public class SettingsWindow : Window {
    private const string REQUIRES_RELOAD = "Requires a reload. Changing this value might immediately reload Rysy.";

    private SettingWindowData Data = new();

    private class SettingWindowData {
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
            VisualBar();
            ThemeBar();
            HotkeyBar();
            DebugBar();

            ImGui.EndTabBar();
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

        var vsync = Settings.Instance.VSync;
        if (ImGui.Checkbox("VSync", ref vsync).WithTooltip("Whether to use VSync or not")) {
            Settings.Instance.VSync = vsync;
            Settings.Instance.Save();
        }

        var smart = Settings.Instance.SmartFramerate;
        if (ImGui.Checkbox("Smart Framerate", ref smart).WithTooltip("Reduces target FPS when you haven't moved the mouse or pressed any keys for 1 second, to reduce CPU/GPU usage.")) {
            Settings.Instance.SmartFramerate = smart;
            Settings.Instance.Save();
        }

        // has issues with mouse position
        //var fullscreen = Settings.Instance.BorderlessFullscreen;
        //if (ImGui.Checkbox("Borderless Fullscreen", ref fullscreen).WithTooltip("Toggles borderless fullscreen mode.")) {
        //    Settings.Instance.BorderlessFullscreen = fullscreen;
        //    Settings.Instance.Save();
        //}


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

        ImGuiManager.BeginWindowBottomBar(!invalid && Data.EditedHotkeys is { });
        if (ImGui.Button("Apply Changes") && Data.EditedHotkeys is { } hotkeys) {
            foreach (var (name, newVal) in hotkeys) {
                Settings.Instance.Hotkeys[name] = newVal;
                Settings.Instance.Save();

                RysyEngine.Scene.SetupHotkeys();
            }
            Data.EditedHotkeys = null;
        }
        ImGuiManager.EndWindowBottomBar();

        ImGui.EndTabItem();
    }

    private void ThemeBar() {
        var windowData = Data;

        if (ImGui.BeginTabItem("Themes")) {
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


            ImGui.EndTabItem();
        }
    }

    private void ProfileBar() {
        var windowData = Data;

        if (ImGui.BeginTabItem("Profile")) {
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
            ref var celesteDir = ref windowData.ProfileCelesteDir;
            if (ImGui.InputText("Celeste Install", ref celesteDir, 512, ShowPaths ? ImGuiInputTextFlags.None : ImGuiInputTextFlags.Password)) {
                windowData.ProfileSettingsChanged = true;
            }


            ImGuiManager.BeginWindowBottomBar(windowData.ProfileSettingsChanged);
            if (ImGui.Button("Apply Changes")) {
                Profile.Instance.CelesteDirectory = celesteDir;
                Profile.Instance.Save();

                QueueReload();
            }
            ImGuiManager.EndWindowBottomBar();

            ImGui.EndTabItem();
        }
    }

    private static bool ShowPaths;

    public SettingsWindow(string name, NumVector2? size = null) : base(name, size) {
    }

    private static void SetProfile(string name, bool isNew) {
        Settings.Instance.Profile = name;
        Settings.Save(Settings.Instance);

        if (isNew) {
            var profile = new Profile(Profile.Instance);
            profile.Save();
        }

        QueueReload();
    }

    private static void QueueReload() {
        RysyEngine.OnEndOfThisFrame += async () => {
            await RysyEngine.Instance.ReloadAsync();
        };
    }
}
