using Hexa.NET.ImGui;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Layers;
using Rysy.Scenes;
using Rysy.Selections;
using Rysy.Tools;

namespace Rysy.Gui.Windows;

public static class RoomList {
    private static bool _firstGui = true;

    internal static string Search = "";

    static RoomList() {
        RysyState.OnViewportChanged += OnViewportChanged;
    }

    private static void OnViewportChanged(Viewport viewport) {
        _firstGui = true;
    }

    public static void Render(EditorScene editor, Input input) {
        if (editor is not { Map: { } map }) {
            return;
        }

        if (_firstGui) {
            var menubarHeight = ImGuiManager.MenubarHeight;
            ImGui.SetNextWindowPos(new NumVector2(0f, menubarHeight));
            ImGui.SetNextWindowSize(new NumVector2(150f, RysyState.GraphicsDevice.Viewport.Height - menubarHeight));

            _firstGui = false;
        }

        ImGuiManager.PushWindowStyle();
        ImGui.Begin("Rooms", ImGuiManager.WindowFlagsResizable);
        ImGuiManager.PopWindowStyle();

        var size = ImGui.GetWindowSize();

        if (ImGui.BeginListBox("##RoomListBox", new(size.X - 10, size.Y - ImGui.GetTextLineHeightWithSpacing() * 5))) {
            var rooms = map.Rooms.SearchFilter(r => r.Searchable, Search);
            foreach (var room in rooms) {
                var name = room.Name;
                if (ImGui.Selectable(name, editor.CurrentRoom == room || room.Selected)) {
                    if (input.Keyboard.Shift()) {
                        if (editor.ToolHandler.SetTool<SelectionTool>() is { } tool) {
                            tool.Layer = EditorLayers.Room;

                            if (input.Mouse.LeftDoubleClicked()) {
                                // select all rooms visible in the list
                                foreach (var r2 in rooms) {
                                    tool.AddSelection(new(r2.GetSelectionHandler()));
                                }
                            } else {
                                // add a selection for the clicked room
                                tool.AddSelection(new(room.GetSelectionHandler()));
                            }
                        }
                    } else if (input.Keyboard.Ctrl()) {
                        if (editor.ToolHandler.SetTool<SelectionTool>() is { } tool) {
                            tool.Layer = EditorLayers.Room;

                            if (input.Mouse.LeftDoubleClicked()) {
                                // deselect all rooms visible in the list
                                foreach (var r2 in rooms) {
                                    tool.Deselect(r2.GetSelectionHandler());
                                }
                            } else {
                                // remove the selection for the clicked room
                                tool.Deselect(room.GetSelectionHandler());
                            }
                        }
                    } else {
                        editor.CurrentRoom = room;
                        editor.CenterCameraOnRoom(room);
                    }
                }

                if (ImGui.BeginPopupContextItem(name, ImGuiPopupFlags.MouseButtonRight)) {
                    if (ImGui.MenuItem("Edit")) {
                        editor.AddWindow(new RoomEditWindow(room, newRoom: false));
                    }
                    if (ImGui.MenuItem("Remove")) {
                        var toRemove = room;
                        RysyState.OnEndOfThisFrame += () =>
                            editor.HistoryHandler.ApplyNewAction(new RoomDeleteAction(toRemove));
                    }

                    if (ImGui.MenuItem("To Clipboard as JSON")) {
                        ImGui.SetClipboardText(room.Pack().ToJson());
                    }
                    ImGui.EndPopup();
                }

            }

            ImGui.EndListBox();
        }
        
        if (ImGui.Button("New")) {
            editor.AddNewRoom();
        }

        if (ImGui.InputText("##RoomListSearch", ref Search, 512)) {

        }
        
        ImGui.End();
    }
}
