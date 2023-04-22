﻿using ImGuiNET;
using Rysy.Extensions;
using Rysy.Gui.Windows;
using Rysy.Helpers;
using Rysy.Scenes;

namespace Rysy.Gui;
public static class Menubar {
    private record class Tab {
        public string Name;
        public Action Render;
    }

    private static List<Tab> Tabs = new() {
        new() { 
            Name = "File",
            Render = FileMenu,
        },
        new() {
            Name = "Edit",
            Render = EditMenu,
        },
        new() {
            Name = "View",
            Render = ViewMenu,
        },
        new() {
            Name = "Debug",
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
            Tabs.Add(new() { 
                Name = name,
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

    private static void ViewMenu() {
        if (RysyEngine.Scene is not EditorScene editor)
            return;

        ViewWindowsMenu();

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

        var currLayer = p.EditorLayer ?? 0;
        var allLayers = p.EditorLayer is null;
        if (ImGui.InputInt("Layer", ref currLayer)) {
            p.EditorLayer = currLayer;
        }
        if (ImGui.Checkbox("All layers", ref allLayers)) {
            p.EditorLayer = allLayers ? null : 0;
        }
    }

    private static void ViewWindowsMenu() {
        if (!ImGui.BeginMenu("Windows")) {
            return;
        }

        if (ImGui.Checkbox("History", ref Persistence.Instance.HistoryWindowOpen)) {
            RysyEngine.Scene.AddWindowIfNeeded<HistoryWindow>();
        }

        if (ImGui.Button("Filesystem Explorer")) {
            RysyEngine.Scene.AddWindow(new FilesystemExplorerWindow());
        }

        ImGui.EndMenu();
    }

    private static void DebugMenu() {
        if (RysyEngine.Scene is not EditorScene editor)
            return;

        if (ImGui.MenuItem("Style Editor")) {
            editor.AddWindow(new ScriptedWindow("Style Editor", (w) => {
                ImGui.ShowStyleEditor();
            }));
        }

        if (ImGui.MenuItem("Clear Render Cache").WithTooltip("Clears the render cache of all rooms in the map")) {
            editor.Map.Rooms.ForEach(r => r.ClearRenderCache());
        }

        if (ImGui.MenuItem("Map as JSON").WithTooltip("Copies the map as JSON to your clipboard")) {
            ImGui.SetClipboardText(editor.Map.Pack().ToJson());
        }

        if (ImGui.MenuItem("GC").WithTooltip("Causes a very aggressive GC call")) {
            GCHelper.VeryAggressiveGC();
        }

        bool b = DebugInfoWindow.Enabled;
        if (ImGui.Checkbox("Debug Info Window", ref b))
            DebugInfoWindow.Enabled = b;
    }

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

        if (ImGui.MenuItem("New", Settings.Instance.GetOrCreateHotkey("newMap"))) {
            editor.LoadNewMap();
        }

        if (ImGui.MenuItem("Open", Settings.Instance.GetOrCreateHotkey("openMap"))) {
            editor.Open();
        }

        ImGuiManager.DropdownMenu("Recent", Persistence.Instance.RecentMaps,
            p => Persistence.Instance.RecentMaps.Count(p2 => p2.Name == p.Name) > 1
                ? $"{p.Name} [{p.Filename.Censor().CorrectSlashes()}]"
                : p.Name,
            p => editor.LoadMapFromBin(p.Filename));

        if (ImGui.MenuItem("Save", Settings.Instance.GetOrCreateHotkey("saveMap")).WithTooltip(editor.Map?.Filepath?.Censor() ?? "[null]")) {
            editor.Save();
        }
        if (ImGui.MenuItem("Save as")) {
            editor.Save(true);
        }

        if (ImGui.MenuItem("Exit"))
            RysyEngine.Instance.Exit();
    }
}
