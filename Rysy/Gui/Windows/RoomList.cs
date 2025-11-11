using Hexa.NET.ImGui;
using Rysy.Entities;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Layers;
using Rysy.Scenes;
using Rysy.Stylegrounds;
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

        var size = ImGui.GetContentRegionAvail();

        ImGui.BeginChild("##RoomListBox", new(size.X, size.Y - ImGui.GetTextLineHeightWithSpacing() * 3), ImGuiChildFlags.None);
        
        var rooms = map.Rooms.SearchFilter(r => r.Searchable, Search);
        foreach (var room in rooms) {
            var name = room.Name;

            // Draw the room color marker:
            var drawList = ImGui.GetWindowDrawList();
            var p = ImGui.GetCursorScreenPos();
            drawList.AddRectFilled(p, new(p.X + ImGui.GetStyle().FramePadding.X, p.Y + ImGui.GetTextLineHeightWithSpacing()), room.Color.PackedValue, ImGui.GetStyle().FrameRounding, ImDrawFlags.RoundCornersAll);
            ImGui.Dummy(default);
            ImGui.SameLine(ImGui.GetStyle().FramePadding.X * 2f);
            // Center the selectable vertically
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().ItemSpacing.Y / 2f);
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
            
            if (ImGui.IsItemHovered() && ImGui.BeginTooltip()) {
                const int w = 320;
                const int h = 180;
                
                ImGuiManager.XnaWidget("room-preview-tooltip", w, h, () => {
                    var cam = new Camera();
                    // find first spawn point and center the camera on it (while taking into account room boundaries)
                    Vector2 spawn;
                    if (room.Entities[typeof(Player)].FirstOrDefault() is { } firstSpawn) {
                        spawn = firstSpawn.Pos.Add(room.X, room.Y).Add(-w / 2f, -h / 2f);
                        spawn = new(
                            spawn.X.Clamp(room.Bounds.Left, room.Bounds.Right - w), 
                            spawn.Y.Clamp(room.Bounds.Top, room.Bounds.Bottom - h));
                    } else {
                        spawn = room.Pos;
                    }

                    cam.Move(spawn);
                    var prev = Gfx.EndBatch();
                    
                    var renderStylegrounds = Settings.Instance?.StylegroundPreview ?? false;
                    var fgInFront = Settings.Instance?.RenderFgStylegroundsInFront ?? false;
                    
                    if (renderStylegrounds)
                        StylegroundRenderer.Render(room, room.Map.Style, cam, StylegroundRenderer.Layers.Bg, filter: StylegroundRenderer.NotMasked);
                    
                    room.Render(cam, Room.RenderConfig.Preview, Colorgrade.None);
                    
                    if (renderStylegrounds && fgInFront)
                        StylegroundRenderer.Render(room, room.Map.Style, cam, StylegroundRenderer.Layers.Fg, filter: StylegroundRenderer.NotMasked);
                    
                    Gfx.BeginBatch(prev);
                });
                ImGui.EndTooltip();
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

        ImGui.EndChild();
        
        if (ImGui.Button("New")) {
            editor.AddNewRoom();
        }

        ImGuiManager.RenderSearchBarInDropdown(ref Search, "RoomListSearch");
        
        ImGui.End();
    }
}
