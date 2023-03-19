using ImGuiNET;
using Rysy.Graphics;
using Rysy.Helpers;
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

        /*
        DECAL LIST CODE for later

        cached:
        private static List<(string VirtPath, VirtTexture)> decalList = GFX.Atlas.GetTextures().Where(p => p.virtPath.StartsWith("decals/", StringComparison.InvariantCulture)).SearchFilter(p => p.virtPath, Search).ToList();
        
        decal list code:
        var skip = (ImGui.GetScrollY()) / ImGui.GetTextLineHeightWithSpacing() - 1;
        var rendered = 0;
        foreach (var item in decalList) {
        // todo: calculate that 60!!!
            if (rendered < 60 && skip <= 0) {
                rendered++;
                ImGui.Selectable(item.VirtPath);
            }
            else
                ImGui.NewLine();

            skip--;
        }*/

        ImGui.EndListBox();

        if (ImGui.Button("New")) {
            editor.AddNewRoom();
        }

        if (ImGui.InputText("##RoomListSearch", ref Search, 512)) {

        }
    }
}
