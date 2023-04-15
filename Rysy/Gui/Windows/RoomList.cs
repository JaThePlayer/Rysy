using ImGuiNET;
using Rysy.Extensions;
using Rysy.Graphics;
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

        ImGui.BeginListBox("##RoomListBox", new(size.X - 10, size.Y - ImGui.GetTextLineHeightWithSpacing() * 5));

        foreach (var room in map.Rooms.SearchFilter(r => r.Name, Search)) {
            var name = room.Name;
            if (ImGui.Selectable(name, editor.CurrentRoom == room)) {
                editor.CurrentRoom = room;
                editor.CenterCameraOnRoom(room);
            }

            if (ImGui.BeginPopupContextItem(name, ImGuiPopupFlags.MouseButtonRight)) {
                if (ImGui.MenuItem("Edit")) {
                    editor.AddWindow(new RoomEditWindow(room, newRoom: false));
                }
                if (ImGui.MenuItem("Remove")) {
                    editor.HistoryHandler.ApplyNewAction(new RoomDeleteAction(editor.Map, room));
                }

                if (ImGui.MenuItem("To Clipboard as JSON")) {
                    ImGui.SetClipboardText(room.Pack().ToJson());
                }
                ImGui.EndPopup();
            }

        }

        ImGui.EndListBox();

        if (ImGui.Button("New")) {
            editor.AddNewRoom();
        }

        if (ImGui.InputText("##RoomListSearch", ref Search, 512)) {

        }
    }
}
