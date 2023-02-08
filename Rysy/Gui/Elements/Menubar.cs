using ImGuiNET;
using Rysy.Gui.Elements;
using Rysy.Scenes;

namespace Rysy.Gui;
public static class Menubar {
    public static void Render(EditorScene editor) {
        ImGuiManager.PushWindowStyle();
        if (!ImGui.BeginMainMenuBar())
            return;
        ImGuiManager.PopWindowStyle();
        ImGuiManager.MenubarHeight = ImGui.GetWindowHeight();

        FileMenu(editor);
        EditMenu(editor);
        ViewMenu(editor);
        DebugMenu(editor);

        ImGui.EndMainMenuBar();
    }

    private static void ViewMenu(EditorScene editor) {
        if (ImGui.BeginMenu("View")) {
            var p = Persistence.Instance;
            bool b;

            b = p.FGTilesVisible;
            if (ImGui.Checkbox("FG Tiles", ref b)) {
                p.FGTilesVisible = b;
                editor.Map.Rooms.ForEach(r => r.ClearRenderCache());
            }

            b = p.BGTilesVisible;
            if (ImGui.Checkbox("BG Tiles", ref b)) {
                p.BGTilesVisible = b;
                editor.Map.Rooms.ForEach(r => r.ClearRenderCache());
            }

            b = p.EntitiesVisible;
            if (ImGui.Checkbox("Entities", ref b)) {
                p.EntitiesVisible = b;
                editor.Map.Rooms.ForEach(r => r.ClearRenderCache());
            }

            b = p.TriggersVisible;
            if (ImGui.Checkbox("Triggers", ref b)) {
                p.TriggersVisible = b;
                editor.Map.Rooms.ForEach(r => r.ClearRenderCache());
            }

            b = p.FGDecalsVisible;
            if (ImGui.Checkbox("FG Decals", ref b)) {
                p.FGDecalsVisible = b;
                editor.Map.Rooms.ForEach(r => r.ClearRenderCache());
            }

            b = p.BGDecalsVisible;
            if (ImGui.Checkbox("BG Decals", ref b)) {
                p.BGDecalsVisible = b;
                editor.Map.Rooms.ForEach(r => r.ClearRenderCache());
            }

            ImGui.EndMenu();
        }
    }

    private static void DebugMenu(EditorScene editor) {
        if (ImGui.BeginMenu("Debug")) {
            if (ImGui.MenuItem("Style Editor")) {
                editor.AddWindow(new("Style Editor", (w) => {
                    ImGui.ShowStyleEditor();
                }));
            }

            if (ImGui.MenuItem("Map as JSON").WithTooltip("Copies the map as JSON to your clipboard")) {
                ImGui.SetClipboardText(editor.Map.Pack().ToJson());
            }

            ImGui.EndMenu();
        }
    }

    private static void EditMenu(EditorScene editor) {
        if (ImGui.BeginMenu("Edit")) {
            if (ImGui.MenuItem("Settings")) {
                SettingsWindow.Add(editor);
            }

            if (ImGui.MenuItem("Undo", Settings.Instance.GetHotkey("undo")))
                editor.Undo();

            if (ImGui.MenuItem("Redo", Settings.Instance.GetHotkey("redo")))
                editor.Redo();

            ImGui.EndMenu();
        }
    }

    private static void FileMenu(EditorScene editor) {
        if (ImGui.BeginMenu("File")) {
            if (ImGui.MenuItem("New", Settings.Instance.GetHotkey("newMap"))) {
                editor.LoadNewMap();
            }

            if (ImGui.MenuItem("Open", Settings.Instance.GetHotkey("openMap"))) {
                editor.Open();
            }

            ImGuiManager.DropdownMenu("Recent", Persistence.Instance.RecentMaps,
                p => Persistence.Instance.RecentMaps.Count(p2 => p2.Name == p.Name) > 1
                    ? $"{p.Name} [{p.Filename.TryCensor().CorrectSlashes()}]"
                    : p.Name,
                p => editor.LoadMapFromBin(p.Filename));

            if (ImGui.MenuItem("Save", Settings.Instance.GetHotkey("saveMap")).WithTooltip(editor.Map?.Filepath?.TryCensor() ?? "[null]")) {
                editor.Save();
            }
            if (ImGui.MenuItem("Save as")) {
                editor.Save(true);
            }

            if (ImGui.MenuItem("Exit"))
                RysyEngine.Instance.Exit();

            ImGui.EndMenu();
        }
    }
}
