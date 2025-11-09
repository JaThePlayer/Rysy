using Hexa.NET.ImGui;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;
using Rysy.Mods;
using Rysy.Platforms;

namespace Rysy.Gui.Windows;
public sealed class SettingsWindow : Window {
    private const string RequiresReload = "Requires a reload. Changing this value might immediately reload Rysy.";

    private readonly SettingWindowData _data = new();

    private ModMeta? _selectedMod = null;

    private sealed class SettingWindowData {
        public bool ProfileSettingsChanged = false;
        public string[]? ProfileListDirectories = null;
        public string? ProfileCelesteDir;

        public IReadOnlyList<Themes.FoundTheme>? ThemeList = null;
        public string ThemeListSearch = "";
        public ComboCache<Themes.FoundTheme> ThemeListCache = new();

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

        ImGui.EndTabItem();
    }

    private void VisualBar() {
        if (!ImGui.BeginTabItem("Visual"))
            return;

        var fps = Settings.Instance.TargetFps;
        if (ImGui.InputInt("Target FPS", ref fps, 10, 30)
            .WithTooltip("The maximum FPS that Rysy will attempt to reach. Higher values increase CPU/GPU usage in exchange for smoother visuals.")) {
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
        if (ImGuiManager.TranslatedInputInt("rysy.settings.general.maxBackups", ref backups)) {
            Settings.Instance.MaxBackups = backups.AtLeast(0);
            Settings.Instance.Save();
        }

        var alpha = Settings.Instance.HiddenLayerAlpha;
        if (ImGuiManager.TranslatedInputFloat("rysy.settings.general.hiddenLayerAlpha", ref alpha, step: 0.1f)) {
            Settings.Instance.HiddenLayerAlpha = alpha.SnapBetween(0f, 1f);
            Settings.Instance.Save();
        }

        var minifyClipboard = Settings.Instance.MinifyClipboard;
        if (ImGuiManager.TranslatedCheckbox("rysy.settings.general.minifyClipboard", ref minifyClipboard)) {
            Settings.Instance.MinifyClipboard = minifyClipboard;
            Settings.Instance.Save();
        }

        var trim = Settings.Instance.TrimEntities;
        if (ImGuiManager.TranslatedCheckbox("rysy.settings.general.trimEntities", ref trim)) {
            Settings.Instance.TrimEntities = trim;
            Settings.Instance.Save();
        }

        var wrap = Settings.Instance.MouseWrapping;
        if (ImGuiManager.TranslatedCheckbox("rysy.settings.general.mouseWrap", ref wrap)) {
            Settings.Instance.MouseWrapping = wrap;
            Settings.Instance.Save();
        }
        
        var panSpeed = Settings.Instance.TouchpadPanSpeed;
        if (ImGuiManager.TranslatedInputFloat("rysy.settings.general.touchpadPanSpeed", ref panSpeed, step: 25f, "%g%%")) {
            Settings.Instance.TouchpadPanSpeed = panSpeed.SnapBetween(25f, 300f);
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
                    if (_data.EditedHotkeys is { } h && h.TryGetValue(name, out var changed)) {
                        hotkey = changed;
                        invalid |= ImGuiManager.PushInvalidStyleIf(!HotkeyHandler.IsValid(hotkey).IsOk);
                    }

                    if (ImGui.InputText(name.Humanize(), ref hotkey, 64)) {
                        var edited = _data.EditedHotkeys ??= new();

                        edited[name] = hotkey;
                    }
                    ImGuiManager.PopInvalidStyle();
                }
            },
            renderBottomBar: () => {
                ImGui.BeginDisabled(invalid || _data.EditedHotkeys is not { });

                if (ImGui.Button("Apply Changes") && _data.EditedHotkeys is { } hotkeys) {
                    foreach (var (name, newVal) in hotkeys) {
                        Settings.Instance.Hotkeys[name] = newVal;
                        Settings.Instance.Save();

                        RysyEngine.Scene.SetupHotkeys();
                    }
                    _data.EditedHotkeys = null;
                }
                ImGui.EndDisabled();
            }
        );

        ImGui.EndTabItem();
    }

    private PathField? _fontDropdown;

    private void ThemeBar() {
        var windowData = _data;

        if (!ImGui.BeginTabItem("Themes")) {
            return;
        }

        var themeName = Settings.Instance.Theme;

        windowData.ThemeList ??= Themes.FindThemes();

        Themes.FoundTheme theme
            = windowData.ThemeList.FirstOrDefault(x => x.Filename == themeName) ?? new Themes.FoundTheme(themeName, new Searchable(themeName, null));

        if (ImGuiManager.Combo("Theme", ref theme, windowData.ThemeList, x => x.Searchable,
                ref windowData.ThemeListSearch, default, windowData.ThemeListCache)) {
            Settings.Instance.Theme = theme.Filename;
            Settings.Instance.Save();

            Themes.LoadThemeFromFile(theme.Filename);
        }

        /*
        if (ImGui.BeginCombo("Theme", theme)) {
            foreach (var themeName in windowData.ThemeList) {
                if (ImGui.Selectable(themeName)) {
                    Settings.Instance.Theme = themeName;
                    Settings.Instance.Save();

                    Themes.LoadThemeFromFile(themeName);
                }
            }

            ImGui.EndCombo();
        }
        */

        ImGui.Separator();
        
        var font = Settings.Instance.Font;
        if (_fontDropdown is null) {
            var fs = new LayeredFilesystem();
            fs.AddMod(ModRegistry.RysyMod);
            if (RysyPlatform.Current.GetSystemFontsFilesystem() is {} systemFonts)
                fs.AddFilesystem(systemFonts, "System");
            
            var fontPathToName = RysyPlatform.Current.GetFontFilenameToDisplayName();
            _fontDropdown = Fields.Path(font, "", "ttf", fs, filter: x => 
                (ModRegistry.RysyMod.Filesystem.FileExists(x.Path) || RysyPlatform.Current.IsSystemFontValid(x.Path))
                && x.Path != "fa-solid-900.ttf");
            _fontDropdown.DisplayNameGetter = (path, s) => path.Path.TranslateOrNull("rysy.fonts.name") ?? fontPathToName.GetValueOrDefault(path.Path)?.TrimPostfix("(TrueType)").Trim();
        }
        
        if (_fontDropdown.RenderGui("Font", font) is { } newFont) {
            Settings.Instance.Font = newFont?.ToString() ?? "";
            Settings.Instance.Save();
        }

        var fontSize = Settings.Instance.FontSize;
        if (ImGui.InputInt("Font Size", ref fontSize)) {
            Settings.Instance.FontSize = fontSize;
            Settings.Instance.Save();
        }
        
        var triggerFontSize = Settings.Instance.TriggerFontScale;
        if (ImGui.InputFloat("Trigger Font Scale", ref triggerFontSize)) {
            if (triggerFontSize > 0f) {
                Settings.Instance.TriggerFontScale = triggerFontSize;
                Settings.Instance.Save();
            }
        }

        var useBold = Settings.Instance.UseBoldFontByDefault;
        if (ImGui.Checkbox("Use Bold font by default", ref useBold)) {
            Settings.Instance.UseBoldFontByDefault = useBold;
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
        ImGui.TextDisabled("Mod Settings");

        var modsWithSettings = ModRegistry.Mods.Values
            .Where(m => m.Settings?.HasAnyData() ?? false)
            .ToList();

        var mod = _selectedMod;
        if (mod is null && modsWithSettings.Count > 0) {
            mod = modsWithSettings.First();
        }

        if (ImGuiManager.Combo("Selected Mod", ref mod!, modsWithSettings, m => new Searchable(m?.DisplayName ?? ""), ref ModBarSearch)) {
            _selectedMod = mod;
        }

        if (mod is { }) {
            ImGui.Text(mod.EverestYaml.First().ToString());

            if (ImGuiManager.TranslatedButton("rysy.settings.mods.editSettings")) {
                RysyEngine.Scene.AddWindow(new ModSettingsWindow(mod));
            }
        }

        ImGui.EndTabItem();
    }

    private void DebugBar() {
        if (ImGui.BeginTabItem("Debug")) {
            var m = Settings.Instance.LogMissingEntities;
            if (ImGuiManager.TranslatedCheckbox("rysy.settings.debug.missingEntities", ref m)) {
                Settings.Instance.LogMissingEntities = m;
                Settings.Instance.Save();
            }

            m = Settings.Instance.LogMissingTextures;
            if (ImGuiManager.TranslatedCheckbox("rysy.settings.debug.missingTextures", ref m)) {
                Settings.Instance.LogMissingTextures = m;
                Settings.Instance.Save();
            }

            m = Settings.Instance.LogTextureLoadTimes;
            if (ImGuiManager.TranslatedCheckbox("rysy.settings.debug.textureLoadTimes", ref m)) {
                Settings.Instance.LogTextureLoadTimes = m;
                Settings.Instance.Save();
            }

            m = Settings.Instance.LogSpriteCachingTimes;
            if (ImGuiManager.TranslatedCheckbox("rysy.settings.debug.spriteCacheTimes", ref m)) {
                Settings.Instance.LogSpriteCachingTimes = m;
                Settings.Instance.Save();
            }

            m = Settings.Instance.LogPreloadingTextures;
            if (ImGuiManager.TranslatedCheckbox("rysy.settings.debug.logPreloadTimes", ref m)) {
                Settings.Instance.LogPreloadingTextures = m;
                Settings.Instance.Save();
            }
            
            m = Settings.Instance.LogMissingFieldTypes;
            if (ImGuiManager.TranslatedCheckbox("rysy.settings.debug.logMissingFieldTypes", ref m)) {
                Settings.Instance.LogMissingFieldTypes = m;
                Settings.Instance.Save();
            }


            ImGui.EndTabItem();
        }
    }

    private void ProfileBar() {
        if (RysyPlatform.Current.HasForcedProfile || !ImGui.BeginTabItem("Profile")) {
            return;
        }

        var windowData = _data;
        var celesteDir = windowData.ProfileCelesteDir;
        ImGuiManager.WithBottomBar(
            () => {
                if (ImGui.BeginCombo("Current Profile", Settings.Instance.CurrentProfile)) {
                    #region Profile Picker

                    var fs = RysyPlatform.Current.GetRysyAppDataFilesystem(null);
                    
                    var profileDir = "Profiles";
                    var dirs = windowData.ProfileListDirectories ??= fs.FindDirectories(profileDir).ToArray();
                    foreach (var dir in dirs) {
                        var name = Path.GetRelativePath(profileDir, dir);
                        if (ImGui.Selectable(name).WithTooltip(RequiresReload)) {
                            SetProfile(name, isNew: false);
                        }
                    }

                    if (ImGui.Button("New")) {
                        string text = "";
                        RysyEngine.Scene.AddWindow(new ScriptedWindow("New Profile Name", (w) => {
                            ImGui.InputText("Name", ref text, 64);
                            if (ImGui.Button("Create").WithTooltip(RequiresReload)) {
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
        Settings.ChangeProfile(name, isNew);
    }

    private static void QueueReload() {
        RysyEngine.QueueReload();
    }
}
