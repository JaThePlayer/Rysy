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
    }

    public static void Add(Scene scene) {
        var wind = new Window("Settings", Render, new(720, 480)) {
            Userdata = new SettingWindowData(),
        };
        scene.AddWindow(wind);
    }

    private static void Render(Window window) {
        if (ImGui.BeginTabBar("Tabbar")) {
            ProfileBar(window);
            ThemeBar(window);
            DebugBar(window);

            ImGui.EndTabBar();
        }
    }

    private static void ThemeBar(Window window) {
        var windowData = window.Data<SettingWindowData>();

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
            }

            ImGui.EndTabItem();
        }
    }

    private static void DebugBar(Window window) {
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

    private static void ProfileBar(Window window) {
        var windowData = window.Data<SettingWindowData>();

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

            if (windowData.ProfileSettingsChanged && ImGui.Button("Apply Changes")) {
                Profile.Instance.CelesteDirectory = celesteDir;
                Profile.Instance.Save();

                QueueReload();
            }

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
