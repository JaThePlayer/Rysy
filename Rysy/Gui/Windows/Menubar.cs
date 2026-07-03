using Hexa.NET.ImGui;
using Rysy.Components;
using Rysy.Graphics;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Mods;
using Rysy.Platforms;
using Rysy.Scenes;
using System.Diagnostics.CodeAnalysis;

namespace Rysy.Gui.Windows;

/// <summary>
/// Represents a menubar entry in a specific menubar tab.
/// </summary>
public interface IMenubarEntry {
    public string Tab { get; }
    
    public void RenderGui(Menubar menubar);
}

/// <summary>
/// Allows rendering additional status indicators on the menubar.
/// </summary>
public interface IMenubarIndicator {
    public void RenderMenubarIndicator(Menubar menubar);
}

public class MenubarButtonEntry(string tab, string langKey, Action run, Func<bool>? disabled = null, string? hotkeyId = null, 
    bool addToCommandPalette = true, ImGuiIcons? icon = null) : IMenubarEntry, ICommandPaletteCommand {
    public string Tab => tab;
    
    public Searchable Searchable { get; } = new Searchable(langKey.Translate());
    
    public XnaWidgetDef? CreatePreview() {
        return null;
    }

    public bool HasPreview => false;

    public ITooltip? Tooltip { get; init; } = new TranslatedOrNullTooltip($"{langKey}.tooltip", null);
    
    public void RenderGui(Menubar menubar) {
        using var _ = ScopedImGui.Disabled(disabled?.Invoke() ?? false);
        
        if (hotkeyId is not null) {
            if (ImGuiManager.TranslatedMenuItemHotkey(langKey, Settings.Instance.GetOrCreateHotkey(hotkeyId), icon))
                Run();
        } else {
            if (ImGuiManager.TranslatedMenuItem(langKey, icon))
                Run();
        }
    }
    
    public void Run() {
        if (!(disabled?.Invoke() ?? false))
            run();
    }
    
    bool ICommandPaletteCommand.Enabled => addToCommandPalette;
}

public class MenubarDropdownEntry<T>(string tab, string langKey, Func<IEnumerable<T>> entries, Func<T, Searchable> tToString, Action<T> run,
    Func<bool>? disabled = null, bool addToCommandPalette = true, ImGuiIcons? icon = null) : IMenubarEntry, ICommandPaletteCommand {
    private readonly Func<T, Searchable> _tToString = tToString;
    private readonly Action<T> _run = run;

    public string Tab => tab;
    
    public ITooltip? Tooltip { get; init; } = new TranslatedOrNullTooltip($"{langKey}.tooltip", null);
    
    public Searchable Searchable { get; } = new Searchable(langKey.Translate());
    
    public void RenderGui(Menubar menubar) {
        using var _ = ScopedImGui.Disabled(disabled?.Invoke() ?? false);
        ImGuiManager.DropdownMenu(langKey.Translate(), entries(), t => _tToString(t).TextWithMods, _run, icon);
    }
    
    public XnaWidgetDef? CreatePreview() {
        return null;
    }

    public bool HasPreview => false;
    
    public void Run() {
        if (!(disabled?.Invoke() ?? false)) {
            CommandPaletteWindow.ChangeCommands(RysyState.Scene, entries().Select(x => new Subcommand(this, x)));
        }
    }
    
    bool ICommandPaletteCommand.Enabled => addToCommandPalette;

    private sealed class Subcommand(MenubarDropdownEntry<T> dropdown, T key) : ICommandPaletteCommand {
        public Searchable Searchable => dropdown._tToString(key);
        
        public XnaWidgetDef? CreatePreview() {
            return null;
        }

        public bool HasPreview => false;

        public ITooltip? Tooltip => null;
        
        public void Run() {
            dropdown._run(key);
        }
    }
}

public class Menubar : SceneComponent {
    private const string TabNameLangPrefix = "rysy.menubar.tab";

    private sealed class Tab {
        public string Name;
        public string DisplayName => Name.TranslateOrHumanize(TabNameLangPrefix);
        
        public Action<Scene> Render;

        public Tab(string name) {
            Name = name;
        }
    }

    public const string TabFile = "file";
    public const string TabEdit = "edit";
    public const string TabMap = "map";
    public const string TabView = "view";
    public const string TabDebug = "debug";

    private static readonly List<Tab> Tabs = [
        new(TabFile) { Render = FileMenu, },
        new(TabEdit) { Render = EditMenu, },
        new(TabMap) { Render = MapMenu, },
        new(TabView) { Render = ViewMenu, },
        new(TabDebug) { Render = DebugMenu, }
    ];
    
    /// <summary>
    /// Adds a new tab to the menubar, or adds a callback to an existing tab.
    /// </summary>
    public static void AddTab(string name, Action<Scene> imguiCallback) {
        if (Tabs.FirstOrDefault(t => t.Name == name) is { } existing) {
            existing.Render += imguiCallback;
        } else {
            Tabs.Add(new(name) {
                Render = imguiCallback
            });
        }
    }

    /// <summary>
    /// Removes the given callback from the tab, completely removing the tab if there are no callbacks left.
    /// </summary>
    public static void RemoveTab(string name, Action<Scene> imguiCallback) {
        if (Tabs.FirstOrDefault(t => t.Name == name) is not { } existing) {
            return;
        }

        var newFunc = existing.Render - imguiCallback;
        if (newFunc is null) {
            Tabs.Remove(existing);
            return;
        }

        existing.Render = newFunc;
    }

    public override void OnAdded() {
        base.OnAdded();
        
        #region FileTab
        Scene?.Add(new MenubarButtonEntry(TabFile, "rysy.menubar.file.newMap", 
            () => (Scene as EditorScene)?.LoadNewMap(),
            disabled: () => Scene is not EditorScene,
            hotkeyId: "newMap",
            addToCommandPalette: true,
            icon: ImGuiIcons.FileCirclePlus
        ));
        
        Scene?.Add(new MenubarButtonEntry(TabFile, "rysy.menubar.file.newMod", 
            () => Scene.AddWindowIfNeeded<NewModWindow>(),
            hotkeyId: null,
            addToCommandPalette: true,
            icon: ImGuiIcons.FolderPlus
        ));
        
        Scene?.Add(new MenubarButtonEntry(TabFile, "rysy.menubar.file.openMap", 
            () => (Scene as EditorScene)?.Open(),
            disabled: () => Scene is not EditorScene,
            hotkeyId: "openMap",
            addToCommandPalette: true,
            icon: ImGuiIcons.FolderOpen
        ));
        
        Scene?.Add(new MenubarDropdownEntry<Persistence.RecentMap>(TabFile, "rysy.menubar.file.openRecent", 
            () => Persistence.Instance.RecentMaps,
            tToString: p => new Searchable(Persistence.Instance.RecentMaps.Count(p2 => p2.Name == p.Name) > 1
                    ? $"{p.Name} [{p.Filename.Censor().CorrectSlashes()}]"
                    : p.Name),
            run: p => (Scene as EditorScene)?.LoadMapFromBin(p.Filename),
            disabled: () => Scene is not EditorScene,
            addToCommandPalette: true,
            icon: ImGuiIcons.FolderOpen
        ));
        
        Scene?.Add(new MenubarDropdownEntry<BackupInfo>(TabFile, "rysy.menubar.file.loadBackup", 
            BackupHandler.GetBackups,
            tToString: b => new Searchable($"{b.MapName} ({b.Time}) [{b.Filesize.Value.ToFilesize()}]"),
            run: b => RysyEngine.Scene = new EditorScene(b.BackupFilepath, fromBackup: true, overrideFilepath: b.OrigFilepath),
            addToCommandPalette: true,
            icon: ImGuiIcons.FolderOpen
        ));
        
        Scene?.Add(new MenubarButtonEntry(TabFile, "rysy.menubar.file.save", 
            () => (Scene as EditorScene)?.Save(),
            disabled: () => Scene is not EditorScene,
            hotkeyId: "saveMap",
            addToCommandPalette: true,
            icon: ImGuiIcons.Save
        ));
        
        Scene?.Add(new MenubarButtonEntry(TabFile, "rysy.menubar.file.saveAs", 
            () => (Scene as EditorScene)?.Save(saveAs: true),
            disabled: () => Scene is not EditorScene,
            hotkeyId: null,
            addToCommandPalette: true,
            icon: ImGuiIcons.Save
        ));
        
        Scene?.Add(new MenubarButtonEntry(TabFile, "rysy.menubar.file.exit", 
            () => RysyPlatform.Current.ExitProcess(),
            hotkeyId: null,
            addToCommandPalette: false,
            icon: ImGuiIcons.CircleXMark
        ));
        #endregion
        
        #region EditTab
        Scene?.Add(new MenubarButtonEntry(TabEdit, "rysy.menubar.edit.settings", 
            () => SettingsWindow.Add(Scene),
            hotkeyId: null,
            addToCommandPalette: true,
            icon: ImGuiIcons.Gear
        ));
        
        Scene?.Add(new MenubarButtonEntry(TabEdit, "rysy.menubar.edit.commandPalette", 
            () => Scene.AddWindowIfNeeded<CommandPaletteWindow>(),
            hotkeyId: "commandPalette",
            addToCommandPalette: false,
            icon: ImGuiIcons.Terminal
        ));

        if (RysyPlatform.Current.CanOpenCeleste) {
            Scene?.Add(new MenubarButtonEntry(TabEdit, "rysy.menubar.edit.openCeleste",
                () => RysyPlatform.Current.OpenCeleste(),
                disabled: () => !RysyPlatform.Current.CanOpenCeleste,
                addToCommandPalette: true,
                icon: ImGuiIcons.Play
            ));
        }
        
        Scene?.Add(new MenubarButtonEntry(TabEdit, "rysy.menubar.edit.undo", 
            () => Scene.Get<IHistoryHandler>()?.Undo(),
            hotkeyId: "undo",
            addToCommandPalette: false,
            icon: ImGuiIcons.RotateLeft
        ));
        
        Scene?.Add(new MenubarButtonEntry(TabEdit, "rysy.menubar.edit.redo", 
            () => Scene.Get<IHistoryHandler>()?.Redo(),
            disabled: () => Scene.Get<IHistoryHandler>() is null,
            hotkeyId: "redo",
            addToCommandPalette: false,
            icon: ImGuiIcons.RotateRight
        ));
        #endregion
        
        #region MapTab

        Scene?.Add(new MenubarButtonEntry(TabMap, "rysy.menubar.map.metadata",
            () => {
                if (HasHistoryAndMap(Scene, out var history, out var map))
                    Scene.AddWindowIfNeeded(() => new MetadataWindow(history, map));
            },
            disabled: () => !HasHistoryAndMap(Scene, out _, out _),
            hotkeyId: null,
            addToCommandPalette: true
        ));
        
        Scene?.Add(new MenubarButtonEntry(TabMap, "rysy.menubar.map.stylegrounds",
            () => {
                if (HasHistoryAndMap(Scene, out var history, out _))
                    Scene.AddWindowIfNeeded(() => new StylegroundWindow(Scene.GetRequired<EditorState>(), history));
            },
            disabled: () => !HasHistoryAndMap(Scene, out _, out _),
            hotkeyId: null,
            addToCommandPalette: true
        ));
        
        Scene?.Add(new MenubarButtonEntry(TabMap, "rysy.menubar.map.decalRegistry",
            () => {
                if (HasHistoryAndMap(Scene, out _, out var map))
                    Scene.AddWindowIfNeeded(() => new DecalRegistryWindow(map));
            },
            disabled: () => !HasHistoryAndMap(Scene, out _, out var map) || map.Mod is null,
            hotkeyId: null,
            addToCommandPalette: true
        ));
        
        Scene?.Add(new MenubarButtonEntry(TabMap, "rysy.menubar.map.tilesets",
            () => {
                if (HasHistoryAndMap(Scene, out _, out var map))
                    Scene.AddWindowIfNeeded(() => new TilesetWindow(Scene.GetRequired<EditorState>()));
            },
            disabled: () => !HasHistoryAndMap(Scene, out _, out var map) || map.Mod is null,
            hotkeyId: null,
            addToCommandPalette: true
        ));
        
        Scene?.Add(new MenubarButtonEntry(TabMap, "rysy.menubar.map.everest.yaml",
            () => {
                if (HasHistoryAndMap(Scene, out _, out var map))
                    Scene.AddWindowIfNeeded(() => new EverestYamlWindow(Scene.GetRequired<EditorState>()));
            },
            disabled: () => !HasHistoryAndMap(Scene, out _, out var map) || map.Mod is null,
            hotkeyId: null,
            addToCommandPalette: true
        ));
        
        Scene?.Add(new MenubarButtonEntry(TabMap, "rysy.menubar.map.saveRoomToImage",
            () => {
                if (Scene.Get<EditorState>() is { } editorState && FileDialogHelper.TrySave("png", out var filepath)) {
                    PopupNotificationWindow.ShowOnException(new LangKey("rysy.menubar.tab.map.saveRoomToImage.saveFailed"), 
                        () => SaveMapToImageHelper.RenderMapToImage(filepath, RysyState.GlobalServices, [ editorState.CurrentRoom! ]));
                }
            },
            disabled: () => !HasHistoryAndMap(Scene, out _, out _) || Scene.Get<EditorState>() is null or { CurrentRoom: null },
            hotkeyId: null,
            addToCommandPalette: true
        ));
        
        Scene?.Add(new MenubarButtonEntry(TabMap, "rysy.menubar.map.saveMapToImage",
            () => {
                if (HasHistoryAndMap(Scene, out _, out var map) && FileDialogHelper.TrySave("png", out var filepath)) {
                    PopupNotificationWindow.ShowOnException(new LangKey("rysy.menubar.tab.map.saveMapToImage.saveFailed"), 
                        () => SaveMapToImageHelper.RenderMapToImage(filepath, RysyState.GlobalServices, map.Rooms));
                }
            },
            disabled: () => !HasHistoryAndMap(Scene, out _, out _),
            hotkeyId: null,
            addToCommandPalette: true
        ));
        #endregion

        return;

        bool HasHistoryAndMap(Scene scene, [NotNullWhen(true)] out IHistoryHandler? history, [NotNullWhen(true)] out Map? map) {
            var editorState = scene.Get<EditorState>();
            history = scene.Get<IHistoryHandler>();
            map = editorState?.Map;

            return history is not null && map is not null;
        }
    }

    public override void RenderImGui() {
        if (Scene is null)
            return;
        
        ImGuiManager.PushWindowStyle();
        if (!ImGui.BeginMainMenuBar())
            return;
        ImGuiManager.PopWindowStyle();
        ImGuiManager.MenubarHeight = ImGui.GetContentRegionAvail().Y;

        var entries = Scene.Components.GetAll<IMenubarEntry>();
        foreach (var tab in Tabs) {
            if (ImGui.BeginMenu(tab.DisplayName)) {
                foreach (var entry in entries) {
                    if (entry.Tab == tab.Name)
                        entry.RenderGui(this);
                }
                tab.Render(Scene);

                ImGui.EndMenu();
            }
        }

        foreach (var indicator in Scene.EnumerateAllLocked<IMenubarIndicator>()) {
            ImGui.SameLine();
            ImGui.Separator();
            indicator.RenderMenubarIndicator(this);
        }

        ImGui.EndMainMenuBar();
    }

    private static void MapMenu(Scene scene) {
    }

    private static PathField? ColorgradePreviewField;

    private static void ViewMenu(Scene scene) {
        if (RysyEngine.Scene is not EditorScene editor)
            return;

        ViewWindowsMenu(scene);
        ViewVisibilityMenu();

        var settings = Settings.Instance;

        if (settings is { } && ImGui.BeginMenu("rysy.menubar.view.stylegrounds".Translate())) {
            bool b;

            ImGuiManager.TranslatedText("rysy.menubar.view.stylegrounds.warning");
            ImGuiManager.TranslatedText("rysy.menubar.view.stylegrounds.F12tip");

            b = settings.StylegroundPreview;
            if (ImGuiManager.TranslatedCheckbox("rysy.menubar.view.stylegrounds.showPreview", ref b)) {
                settings.StylegroundPreview = b;
                settings.Save();
            }

            b = settings.AnimateStylegrounds;
            if (ImGuiManager.TranslatedCheckbox("rysy.menubar.view.stylegrounds.animate", ref b)) {
                settings.AnimateStylegrounds = b;
                settings.Save();
            }

            b = settings.RenderFgStylegroundsInFront;
            if (ImGuiManager.TranslatedCheckbox("rysy.menubar.view.stylegrounds.fgInFront", ref b)) {
                settings.RenderFgStylegroundsInFront = b;
                settings.Save();
            }

            b = settings.OnlyRenderStylesAtRealScale;
            if (ImGuiManager.TranslatedCheckbox("rysy.menubar.view.stylegrounds.onlyAtRealScale", ref b)) {
                settings.OnlyRenderStylesAtRealScale = b;
                settings.Save();
            }

            ImGui.EndMenu();
        }

        if (ImGuiManager.TranslatedButton("rysy.menubar.view.realScale")) {
            editor.Camera.Zoom(6f);
        }
        
        if (settings is { }) {
            var animate = settings.Animate;
            if (ImGuiManager.TranslatedCheckbox("rysy.menubar.view.animate", ref animate)) {
                settings.Animate = animate;
                settings.Save();
            }
        }

        if (ColorgradePreviewField is null) {
            ColorgradePreviewField = Fields.Path(Persistence.ColorgradePreviewMapDefaultValue, "Graphics/ColorGrading", "png", ModRegistry.Filesystem);
            ColorgradePreviewField.Translated("rysy.menubar.view.colorgrade");
            
            ColorgradePreviewField.DisplayNameGetter = ((path, s) => path.Captured switch {
                "none" => "rysy.menubar.view.colorgrade.none".Translate(),
                Persistence.ColorgradePreviewMapDefaultValue => "rysy.menubar.view.colorgrade.mapDefault".Translate(),
                _ => s,
            });

            var defaultResolver = ColorgradePreviewField.ModResolver;
            ColorgradePreviewField.ModResolver = (path) => path.Captured == "none" ? null : defaultResolver(path);
            
            ColorgradePreviewField.AdditionalEntries = [
                new FoundPath(Persistence.ColorgradePreviewMapDefaultValue,Persistence.ColorgradePreviewMapDefaultValue, null)
            ];
        }
        
        if (ColorgradePreviewField.RenderGui("rysy.menubar.view.colorgrade".Translate(),
                Persistence.Instance.ColorgradePreview) is string newPreview) {
            Persistence.Instance.ColorgradePreview = newPreview;
        }
        
        if (settings is { }) {
            var gridSize = settings.GridSize;
            if (ImGuiManager.TranslatedInputInt("rysy.menubar.view.gridSize", ref gridSize)) {
                settings.GridSize = gridSize;
                settings.Save();
            }
        }
    }

    private static void ViewVisibilityMenu() {
        if (ImGui.BeginMenu("rysy.menubar.view.visibility".Translate())) {
            var p = Persistence.Instance;
            bool b;

            b = p.FgTilesVisible;
            if (ImGui.Checkbox("FG Tiles", ref b)) {
                p.FgTilesVisible = b;
            }

            b = p.BgTilesVisible;
            if (ImGui.Checkbox("BG Tiles", ref b)) {
                p.BgTilesVisible = b;
            }

            b = p.EntitiesVisible;
            if (ImGui.Checkbox("Entities", ref b)) {
                p.EntitiesVisible = b;
            }

            b = p.TriggersVisible;
            if (ImGui.Checkbox("Triggers", ref b)) {
                p.TriggersVisible = b;
            }

            b = p.FgDecalsVisible;
            if (ImGui.Checkbox("FG Decals", ref b)) {
                p.FgDecalsVisible = b;
            }

            b = p.BgDecalsVisible;
            if (ImGui.Checkbox("BG Decals", ref b)) {
                p.BgDecalsVisible = b;
            }

            ImGui.EndMenu();
        }
    }

    private static void ViewWindowsMenu(Scene scene) {
        if (!ImGui.BeginMenu("rysy.menubar.view.windows".Translate())) {
            return;
        }

        if (ImGui.Button("Filesystem Explorer")) {
            RysyEngine.Scene.ToggleWindow<FilesystemExplorerWindow>();
        }

        if (ImGui.Button(MapAnalyzerWindow.Name)) {
            RysyEngine.Scene.ToggleWindow<MapAnalyzerWindow>();
        }
        
        if (ImGuiManager.TranslatedButton(NotificationsWindow.TitleId)) {
            RysyEngine.Scene.ToggleWindow<NotificationsWindow>();
        }

        foreach (var persister in scene.EnumerateAllLocked<IWindowPersister>()) {
            persister.RenderImGuiToggle(scene);
        }

        ImGui.EndMenu();
    }

    private static void DebugMenu(Scene scene) {
        var editorState = scene.Get<EditorState>();
        var map = editorState?.Map;
        
        if (ImGui.MenuItem("Style Editor").WithTooltip("WARNING: For development purposes only, changes done in this window don't save!")) {
            scene.AddWindow(new ScriptedWindow("Style Editor", (w) => {
                ImGui.ShowStyleEditor();
            }));
        }
        
        if (ImGui.Checkbox("History Window (DEBUG)", ref Persistence.Instance.HistoryWindowOpen)) {
            RysyEngine.Scene.AddWindowIfNeeded<HistoryWindow>();
        }
        
        if (ImGui.MenuItem("Lua REPL")) {
            RysyEngine.Scene.AddWindowIfNeeded<LuaReplWindow>();
        }

        if (map is { } && editorState?.History is { } history) {
            if (ImGui.MenuItem("sizeoscope".TranslateOrHumanize("rysy.menubar.tab.map"))) {
                scene.AddWindow(new MapSizeoscopeWindow(map, history));
            }
        }
        
        if (map is { } && ImGui.MenuItem("Clear Render Cache").WithTooltip("Clears the render cache of all rooms in the map")) {
            map.Rooms.ForEach(r => r.ClearRenderCacheAggressively());
        }

        if (map is { } && ImGui.MenuItem("Map as JSON").WithTooltip("Copies the map as JSON to your clipboard")) {
            ImGui.SetClipboardText(map.Pack().ToJson());
        }

        if (ImGui.MenuItem("GC").WithTooltip("Causes a very aggressive GC call")) {
            GcHelper.VeryAggressiveGc();
        }

        /*
#if WINDOWS

        if (ImGui.MenuItem("focus")) {
            FocusProcess();
        }
#endif*/

        if (ImGui.MenuItem("Debug Info Window")) {
            scene.AddWindowIfNeeded<DebugInfoWindow>();
        }

        if (RysyPlatform.Current.CanOpenLogDirectory && ImGuiManager.TranslatedButton("rysy.menubar.debug.openLog")) {
            RysyPlatform.Current.OpenLogDirectory();
        }
    }
    /*
    #if WINDOWS
        [DllImport("user32.dll")]
        internal static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static void FocusProcess() {
            Process[] processRunning = Process.GetProcesses();
            foreach (Process pr in processRunning) {
                if (pr.ProcessName.Equals("Celeste", StringComparison.OrdinalIgnoreCase)) {
                    FocusProcess(pr);
                }
            }

            Thread.Sleep(1000);

            FocusProcess(Process.GetCurrentProcess());
        }

        private static void FocusProcess(Process pr) {
            var hWnd = pr.MainWindowHandle;
            ShowWindow(hWnd, 3);
            SetForegroundWindow(hWnd);
        }
    #endif*/

    private static void EditMenu(Scene scene) {
    }

    private static void FileMenu(Scene scene) {
    }
}