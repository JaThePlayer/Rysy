using ImGuiNET;
using Rysy.History;
using Rysy.Scenes;

namespace Rysy.Gui;

public static class RoomList {
    private static bool _firstGui = true;

    public static string Search = "";

    static RoomList() {
        RysyEngine.OnViewportChanged += OnViewportChanged;
    }

    private static void OnViewportChanged(Viewport viewport) {
        _firstGui = true;
    }

    public static void Render(EditorScene editor) {
        if (editor is not { Map: { } map }) {
            return;
        }

        if (_firstGui) {
            var menubarHeight = ImGuiManager.MenubarHeight;
            ImGui.SetNextWindowPos(new NumVector2(0f, menubarHeight));
            ImGui.SetNextWindowSize(new NumVector2(150f, RysyEngine.Instance.GraphicsDevice.Viewport.Height - menubarHeight));

            _firstGui = false;
        }

        ImGuiManager.PushWindowStyle();
        if (!ImGui.Begin("Rooms", ImGuiManager.WindowFlagsResizable)) {
            return;
        }
        ImGuiManager.PopWindowStyle();

        var size = ImGui.GetWindowSize();

        ImGui.BeginListBox("##RoomListBox", new(size.X - 10, size.Y - ImGui.GetTextLineHeightWithSpacing() * 3));

        foreach (var (name, room) in map.Rooms.SearchFilter(r => r.Key, Search)) {
            if (ImGui.Selectable(name, editor.CurrentRoom == room)) {
                editor.CurrentRoom = room;
                editor.CenterCameraOnRoom(room);
            }

            if (ImGui.BeginPopupContextItem(name, ImGuiPopupFlags.MouseButtonRight)) {
                ImGui.MenuItem("Edit").WithTooltip("Unimplemented");
                if (ImGui.MenuItem("Remove")) {
                    editor.HistoryHandler.ApplyNewAction(new RoomDeleteAction(editor.Map, room));
                }
                ImGui.EndPopup();
            }

        }

        ImGui.EndListBox();

        if (ImGui.InputText("##RoomListSearch", ref Search, 512)) {

        }
    }
}
