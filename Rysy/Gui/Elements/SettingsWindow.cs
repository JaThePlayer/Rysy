using ImGuiNET;
using Rysy.Platforms;

namespace Rysy.Gui.Elements;
public static class SettingsWindow {
    private const string REQUIRES_RELOAD = "Requires a reload. Changing this value might immediately reload Rysy.";

    private class SettingWindowData {
        public bool ProfileSettingsChanged = false;
        public string[]? ProfileListDirectories = null;
        public string? ProfileCelesteDir;

        public string[]? ThemeList = null;

        public Dictionary<string, string>? EditedHotkeys = null;
    }

    public static void Add(Scene scene) {
        var wind = new Window<SettingWindowData>("Settings", new(), Render, new(720, 480));
        scene.AddWindow(wind);
    }

    private static void Render(Window<SettingWindowData> window) {
        if (ImGui.BeginTabBar("Tabbar")) {
            ProfileBar(window);
            GeneralBar(window);
            ThemeBar(window);
            HotkeyBar(window);
            DebugBar(window);

            ImGui.EndTabBar();
        }
    }

    private static void GeneralBar(Window<SettingWindowData> window) {
        if (!ImGui.BeginTabItem("General"))
            return;

        var backups = Settings.Instance.MaxBackups;
        if (ImGui.InputInt("Max Backups", ref backups).WithTooltip("The maximum amount of backups stored at once. Once this limit is exceeded, old backups will get deleted.")) {
            Settings.Instance.MaxBackups = backups;
            Settings.Instance.Save();
        }

        ImGui.EndTabItem();
    }

    private static void HotkeyBar(Window<SettingWindowData> window) {
        if (!ImGui.BeginTabItem("Hotkeys"))
            return;

        var invalid = false;
        foreach (var (name, origHotkey) in Settings.Instance.Hotkeys) {
            var hotkey = origHotkey;
            if (window.Data.EditedHotkeys is { } h && h.TryGetValue(name, out var changed)) {
                hotkey = changed;
                invalid |= ImGuiManager.PushInvalidStyleIf(!HotkeyHandler.IsValid(hotkey));
            }
            
            if (ImGui.InputText(name.Humanize(), ref hotkey, 64)) {
                var edited = window.Data.EditedHotkeys ??= new();

                edited[name] = hotkey;
            }
            ImGuiManager.PopInvalidStyle();
        }

        ImGuiManager.BeginWindowBottomBar(!invalid && window.Data.EditedHotkeys is { });
        if (ImGui.Button("Apply Changes") && window.Data.EditedHotkeys is { } hotkeys) {
            foreach (var (name, newVal) in hotkeys) {
                Settings.Instance.Hotkeys[name] = newVal;
                Settings.Instance.Save();

                RysyEngine.Scene.SetupHotkeys();
            }
            window.Data.EditedHotkeys = null;
        }
        ImGuiManager.EndWindowBottomBar();

        ImGui.EndTabItem();
    }

    private static void ThemeBar(Window<SettingWindowData> window) {
        var windowData = window.Data;

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

    private static void DebugBar(Window<SettingWindowData> window) {
        if (ImGui.BeginTabItem("Debug")) {
            var m = Settings.Instance.LogMissingEntities;
            if (ImGui.Checkbox("Log Missing Entities", ref m).WithTooltip("Logs any entities without Rysy plugins to the console.")) {
                Settings.Instance.LogMissingEntities = m;
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

    private static void ProfileBar(Window<SettingWindowData> window) {
        var windowData = window.Data;

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
                    RysyEngine.Scene.AddWindow(new("New Profile Name", (w) => {
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
        RysyEngine.OnFrameEnd += async () => {
            await RysyEngine.Instance.ReloadAsync();
        };
    }
}
