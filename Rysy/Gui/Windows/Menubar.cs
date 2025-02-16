using ImGuiNET;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;
using Rysy.Mods;
using Rysy.Platforms;
using Rysy.Scenes;

namespace Rysy.Gui.Windows;

public static class Menubar {
    private const string TabNameLangPrefix = "rysy.menubar.tab";

    private sealed record class Tab {
        public string Name;
        public Action Render;

        public Tab(string name) {
            Name = name.TranslateOrHumanize(TabNameLangPrefix);
        }
    }

    private static List<Tab> Tabs = new() {
        new("file") {
            Render = FileMenu,
        },
        new("edit") {
            Render = EditMenu,
        },
        new("map") {
            Render = MapMenu,
        },
        new("view") {
            Render = ViewMenu,
        },
        new("debug") {
            Render = DebugMenu,
        },
    };

    /// <summary>
    /// Adds a new tab to the menubar, or adds a callback to an existing tab.
    /// </summary>
    public static void AddTab(string name, Action imguiCallback) {
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
    public static void RemoveTab(string name, Action imguiCallback) {
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

    public static void Render(EditorScene editor) {
        ImGuiManager.PushWindowStyle();
        if (!ImGui.BeginMainMenuBar())
            return;
        ImGuiManager.PopWindowStyle();
        ImGuiManager.MenubarHeight = ImGui.GetWindowHeight();

        foreach (var tab in Tabs) {
            if (ImGui.BeginMenu(tab.Name)) {
                tab.Render();

                ImGui.EndMenu();
            }
        }

        ImGui.EndMainMenuBar();
    }

    private static void MapMenu() {
        if (RysyEngine.Scene is not EditorScene editor)
            return;

        if (EditorState.Map is { } map && EditorState.History is { } history) {
            if (ImGui.MenuItem("metadata".TranslateOrHumanize("rysy.menubar.tab.map"))) {
                editor.AddWindowIfNeeded(() => new MetadataWindow(history, map));
            }

            if (ImGui.MenuItem("stylegrounds".TranslateOrHumanize("rysy.menubar.tab.map"))) {
                editor.AddWindowIfNeeded(() => new StylegroundWindow(history));
            }

            ImGui.BeginDisabled(map.Mod is not { });
            if (ImGui.MenuItem("decalRegistry".TranslateOrHumanize("rysy.menubar.tab.map"))) {
                editor.AddWindowIfNeeded(() => new DecalRegistryWindow(map));
            }
            
            if (ImGui.MenuItem("tilesets".TranslateOrHumanize("rysy.menubar.tab.map"))) {
                editor.AddWindowIfNeeded(() => new TilesetWindow());
            }
            ImGui.EndDisabled();
            
            if (ImGui.MenuItem("sizeoscope".TranslateOrHumanize("rysy.menubar.tab.map"))) {
                editor.AddWindow(new MapSizeoscopeWindow(map, history));
            }
        }
    }

    private static PathField? ColorgradePreviewField;

    private static void ViewMenu() {
        if (RysyEngine.Scene is not EditorScene editor)
            return;

        ViewWindowsMenu();
        ViewVisibilityMenu();

        var settings = Settings.Instance;

        if (settings is { } && ImGui.BeginMenu("Stylegrounds")) {
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
    }

    private static void ViewVisibilityMenu() {
        if (ImGui.BeginMenu("Visibility")) {
            var p = Persistence.Instance;
            bool b;

            b = p.FGTilesVisible;
            if (ImGui.Checkbox("FG Tiles", ref b)) {
                p.FGTilesVisible = b;
            }

            b = p.BGTilesVisible;
            if (ImGui.Checkbox("BG Tiles", ref b)) {
                p.BGTilesVisible = b;
            }

            b = p.EntitiesVisible;
            if (ImGui.Checkbox("Entities", ref b)) {
                p.EntitiesVisible = b;
            }

            b = p.TriggersVisible;
            if (ImGui.Checkbox("Triggers", ref b)) {
                p.TriggersVisible = b;
            }

            b = p.FGDecalsVisible;
            if (ImGui.Checkbox("FG Decals", ref b)) {
                p.FGDecalsVisible = b;
            }

            b = p.BGDecalsVisible;
            if (ImGui.Checkbox("BG Decals", ref b)) {
                p.BGDecalsVisible = b;
            }

            /*
            var currLayer = p.EditorLayer ?? 0;
            var allLayers = p.EditorLayer is null;
            if (ImGui.InputInt("Layer", ref currLayer)) {
                p.EditorLayer = currLayer;
            }
            if (ImGui.Checkbox("All layers", ref allLayers)) {
                p.EditorLayer = allLayers ? null : 0;
            }*/

            ImGui.EndMenu();
        }
    }

    private static void ViewWindowsMenu() {
        if (!ImGui.BeginMenu("Windows")) {
            return;
        }

        if (ImGui.Button("Filesystem Explorer")) {
            RysyEngine.Scene.ToggleWindow<FilesystemExplorerWindow>();
        }

        if (ImGui.Button(MapAnalyzerWindow.Name)) {
            RysyEngine.Scene.ToggleWindow<MapAnalyzerWindow>();
        }

        ImGui.EndMenu();
    }

    private static void DebugMenu() {
        if (RysyEngine.Scene is not EditorScene editor)
            return;

        if (ImGui.MenuItem("Style Editor").WithTooltip("WARNING: For development purposes only, changes done in this window don't save!")) {
            editor.AddWindow(new ScriptedWindow("Style Editor", (w) => {
                ImGui.ShowStyleEditor();
            }));
        }
        
        if (ImGui.Checkbox("History Window (DEBUG)", ref Persistence.Instance.HistoryWindowOpen)) {
            RysyEngine.Scene.AddWindowIfNeeded<HistoryWindow>();
        }

        if (editor.Map is { } && ImGui.MenuItem("Clear Render Cache").WithTooltip("Clears the render cache of all rooms in the map")) {
            editor.Map.Rooms.ForEach(r => r.ClearRenderCacheAggressively());
        }

        if (editor.Map is { } && ImGui.MenuItem("Map as JSON").WithTooltip("Copies the map as JSON to your clipboard")) {
            ImGui.SetClipboardText(editor.Map.Pack().ToJson());
        }

        if (ImGui.MenuItem("GC").WithTooltip("Causes a very aggressive GC call")) {
            GCHelper.VeryAggressiveGC();
        }

        /*
#if WINDOWS

        if (ImGui.MenuItem("focus")) {
            FocusProcess();
        }
#endif*/

        bool b = DebugInfoWindow.Enabled;
        if (ImGui.Checkbox("Debug Info Window", ref b))
            DebugInfoWindow.Enabled = b;
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

    private static void EditMenu() {
        if (RysyEngine.Scene is not EditorScene editor)
            return;

        if (ImGui.MenuItem("Settings")) {
            SettingsWindow.Add(editor);
        }

        if (ImGui.MenuItem("Undo", Settings.Instance.GetOrCreateHotkey("undo")))
            editor.Undo();

        if (ImGui.MenuItem("Redo", Settings.Instance.GetOrCreateHotkey("redo")))
            editor.Redo();
    }

    private static void FileMenu() {
        if (RysyEngine.Scene is not EditorScene editor)
            return;

        if (ImGui.MenuItem("New Map", Settings.Instance.GetOrCreateHotkey("newMap"))) {
            editor.LoadNewMap();
        }
        
        if (ImGui.MenuItem("New Mod")) {
            editor.AddWindowIfNeeded<NewModWindow>();
        }

        if (ImGui.MenuItem("Open", Settings.Instance.GetOrCreateHotkey("openMap"))) {
            editor.Open();
        }

        ImGuiManager.DropdownMenu("Recent", Persistence.Instance.RecentMaps,
            p => Persistence.Instance.RecentMaps.Count(p2 => p2.Name == p.Name) > 1
                ? $"{p.Name} [{p.Filename.Censor().CorrectSlashes()}]"
                : p.Name,
            p => editor.LoadMapFromBin(p.Filename));

        if (ImGui.MenuItem("Save", Settings.Instance.GetOrCreateHotkey("saveMap")).WithTooltip(editor.Map?.Filepath?.Censor())) {
            editor.Save();
        }
        if (ImGui.MenuItem("Save as")) {
            editor.Save(true);
        }

        ImGuiManager.DropdownMenu("Load Backup", BackupHandler.GetBackups(), 
            (b) => $"{b.MapName} ({b.Time}) [{b.Filesize.Value.ToFilesize()}]",
            onClick: (b) => {
                RysyEngine.Scene = new EditorScene(b.BackupFilepath, fromBackup: true, overrideFilepath: b.OrigFilepath);
            });

        if (ImGui.MenuItem("Exit"))
            RysyPlatform.Current.ExitProcess();
    }
}