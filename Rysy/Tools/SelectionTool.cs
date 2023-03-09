using ImGuiNET;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Scenes;
using System.Collections.Generic;

namespace Rysy.Tools;

internal class SelectionTool : Tool {
    private enum States {
        Idle,
        MoveGesture,
    }

    private States State = States.Idle;

    private SelectRectangleGesture SelectionGestureHandler = new();

    private Point? MoveGestureStart, MoveGestureLastMousePos;
    private Vector2 MoveGestureFinalDelta;

    private List<Selection>? CurrentSelections;

    private SelectionLayer CustomLayer;

    public SelectionTool() {
    }

    public override void InitHotkeys(HotkeyHandler handler) {
        base.InitHotkeys(handler);

        handler.AddHotkeyFromSettings("selection.moveLeft", "left", CreateMoveHandler(new(-8, 0)), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.moveRight", "right", CreateMoveHandler(new(8, 0)), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.moveUp", "up", CreateMoveHandler(new(0, -8)), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.moveDown", "down", CreateMoveHandler(new(0, 8)), HotkeyModes.OnHoldSmoothInterval);

        handler.AddHotkeyFromSettings("selection.upsizeLeft", "a", CreateUpsizeHandler(new(-8, 0)), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.upsizeRight", "d", CreateUpsizeHandler(new(8, 0)), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.upsizeUp", "w", CreateUpsizeHandler(new(0, -8)), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.upsizeDown", "s", CreateUpsizeHandler(new(0, 8)), HotkeyModes.OnHoldSmoothInterval);

        handler.AddHotkeyFromSettings("selection.delete", "delete", DeleteSelection);
    }

    private Action CreateUpsizeHandler(Point offset) => () => {
        if (CurrentSelections is { } selections) {
            ResizeSelectionsBy(offset, selections);
        }
    };

    private Action CreateMoveHandler(Vector2 offset) => () => {
        if (CurrentSelections is { } selections) {
            MoveSelectionsBy(offset, selections);
        }
    };

    private void MoveSelectionsBy(Vector2 offset, List<Selection> selections) {
        var action = selections.Select(s => s.Handler.MoveBy(offset)).MergeActions().WithHook(
            onApply: () => selections.ForEach(s => s.Collider.MoveBy(offset)),
            onUndo: () => selections.ForEach(s => s.Collider.MoveBy(-offset))
        );

        History.ApplyNewAction(action);
    }

    private void ResizeSelectionsBy(Point offset, List<Selection> selections) {
        var action = selections.Select(s => s.Handler.TryResize(offset)).MergeActions().WithHook(
            onApply: () => selections.ForEach(s => s.Collider.ResizeBy(offset)),
            onUndo: () => selections.ForEach(s => s.Collider.ResizeBy(offset.Negate()))
        );

        History.ApplyNewAction(action);
    }

    private void SimulateMoveSelectionsBy(Vector2 offset, List<Selection> selections) {
        foreach (var s in selections) {
            s.Handler.MoveBy(offset).Apply();
            s.Collider.MoveBy(offset);
        }
    }

    private void DeleteSelection() {
        if (CurrentSelections is { } selections) {
            var action = selections.Select(s => s.Handler.DeleteSelf()).MergeActions().WithHook(
                onApply: () => {
                    if (CurrentSelections == selections) {
                        Deselect();
                    }
                }
            );

            History.ApplyNewAction(action);
        }
    }

    private void Deselect() {
        // clear the list so that the list captured into the history action lambda no longer contains references to the selections, allowing them to get GC'd
        CurrentSelections?.Clear();
        CurrentSelections = null;
    }

    public override string Name => "Selection";

    public override string PersistenceGroup => "Selection";

    private static readonly List<string> _ValidLayers = new() {
        LayerNames.ENTITIES, LayerNames.TRIGGERS,
        LayerNames.FG_DECALS, LayerNames.BG_DECALS,
        LayerNames.FG, LayerNames.BG,
        LayerNames.ALL, LayerNames.CUSTOM_LAYER
    };

    public override List<string> ValidLayers => _ValidLayers;

    public override string GetMaterialDisplayName(string layer, object material) {
        throw new NotImplementedException();
    }

    public override IEnumerable<object>? GetMaterials(string layer) => Array.Empty<object>();

    public override string? GetMaterialTooltip(string layer, object material) {
        throw new NotImplementedException();
    }

    public override void Render(Camera camera, Room room) {
        if (SelectionGestureHandler.CurrentRectangle is { } rect) {
            DrawSelectionRect(rect);
        }
        if (CurrentSelections is { } selections)
            foreach (var selection in selections) {
                selection.Render(Color.Red);
            }
    }

    public override void RenderOverlay() {
    }

    public override void Update(Camera camera, Room room) {
        if (Input.Mouse.Left.Clicked() && CurrentSelections is { } selections) {
            var mouseRoomPos = GetMouseRoomPos(camera, room);
            var mouseRect = new Rectangle(mouseRoomPos, new(1, 1));

            foreach (var selection in selections) {
                if (selection.Check(mouseRect)) {
                    State = States.MoveGesture;
                    MoveGestureStart = mouseRoomPos;
                    break;
                }
            }
        }

        switch (State) {
            case States.Idle:
                UpdateDragGesture(camera, room);
                break;
            case States.MoveGesture:
                UpdateMoveGesture(camera, room);
                break;
            default:
                break;
        }
    }

    public override void CancelInteraction() {
        base.CancelInteraction();

        EndMoveGesture(true);
        SelectionGestureHandler.CancelGesture();
        Deselect();
    }

    private void EndMoveGesture(bool simulate) {
        if (State != States.MoveGesture)
            return;

        if (simulate && CurrentSelections is { } selections)
            SimulateMoveSelectionsBy(-MoveGestureFinalDelta, selections);

        MoveGestureStart = null;
        MoveGestureLastMousePos = null;
        MoveGestureFinalDelta = Vector2.Zero;
        State = States.Idle;
    }

    private void UpdateMoveGesture(Camera camera, Room room) {
        var left = Input.Mouse.Left;

        switch (left) {
            case MouseInputState.Released: {
                if (CurrentSelections is { } selections && MoveGestureStart is { } start) {
                    Point mousePos = GetMouseRoomPos(camera, room);
                    Vector2 delta = MoveGestureFinalDelta;

                    if (delta.LengthSquared() <= 1) {
                        // If you just clicked in place, then act as if you wanted to simply change your selection, instead of moving
                        SelectWithin(room, new Rectangle(mousePos, new(1, 1)));
                    } else {
                        SimulateMoveSelectionsBy(-delta, selections);
                        MoveSelectionsBy(delta, selections);
                    }
                }

                EndMoveGesture(false);
                break;
            }
            case MouseInputState.Held: {
                if (CurrentSelections is { } selections && MoveGestureStart is { } start) {
                    var mousePos = GetMouseRoomPos(camera, room);
                    MoveGestureLastMousePos ??= mousePos;

                    Vector2 delta = CalculateMovementDelta(MoveGestureLastMousePos!.Value, mousePos);

                    if (delta.LengthSquared() != 0) {
                        SimulateMoveSelectionsBy(delta, selections);
                        MoveGestureLastMousePos += delta.ToPoint();
                        MoveGestureFinalDelta += delta;
                    }
                }
                break;
            }
            case MouseInputState.Clicked:
                break;
            default:
                break;
        }
    }

    private static Vector2 CalculateMovementDelta(Point start, Point mousePos) {
        var delta = (mousePos - start).ToVector2();
        delta = SnapToGridIfNeeded(delta);

        return delta;
    }

    private static Vector2 SnapToGridIfNeeded(Vector2 pos) {
        if (!Input.Keyboard.Ctrl()) {
            pos = pos.GridPosRound(8).ToVector2() * 8f;
        }

        return pos;
    }

    private static Point GetMouseRoomPos(Camera camera, Room room, Point? pos = default) {
        return room.WorldToRoomPos(camera, pos ?? Input.Mouse.Pos);
    }

    private void UpdateDragGesture(Camera camera, Room room) {
        if (SelectionGestureHandler.Update((p) => room.WorldToRoomPos(camera, p)) is { } rect) {
            SelectWithin(room, rect);
        }
    }

    private int ClickInPlaceIdx;

    private void SelectWithin(Room room, Rectangle rect) {
        var selections = room.GetSelectionsInRect(rect, LayerNames.ToolLayerToEnum(Layer, CustomLayer));
        IEnumerable<Selection> finalSelections;

        if (rect.Size.X <= 1 && rect.Size.Y <= 1 && selections.Count > 0) {
            // you just clicked in place, select only 1 selection
            int idx = ClickInPlaceIdx % selections.Count;
            ClickInPlaceIdx = (idx + 1) % selections.Count;

            finalSelections = selections.OrderBy(s => s.Handler.Parent is IDepth d ? d.Depth : 0).Take(idx..(idx + 1));
        } else {
            finalSelections = selections;
            ClickInPlaceIdx = 0;
        }

        if (Input.Keyboard.Shift() && CurrentSelections is { }) {
            CurrentSelections = CurrentSelections
                .Concat(finalSelections.Where(s => s.Handler is not Tilegrid.RectSelectionHandler))
                .DistinctBy(x => x.Handler.Parent)
                .ToList();
        } else {
            Deselect();
            CurrentSelections = finalSelections.ToList();
        }
    }

    public override void RenderGui(EditorScene editor, bool firstGui) {
        BeginMaterialListGUI(firstGui);

        if (Layer == LayerNames.CUSTOM_LAYER) {
            var c = (int) CustomLayer;
            ImGui.CheckboxFlags(LayerNames.ENTITIES, ref c, (int) SelectionLayer.Entities);
            ImGui.CheckboxFlags(LayerNames.TRIGGERS, ref c, (int) SelectionLayer.Triggers);
            ImGui.CheckboxFlags(LayerNames.FG_DECALS, ref c, (int) SelectionLayer.FGDecals);
            ImGui.CheckboxFlags(LayerNames.BG_DECALS, ref c, (int) SelectionLayer.BGDecals);
            ImGui.CheckboxFlags(LayerNames.BG, ref c, (int) SelectionLayer.BGTiles);
            ImGui.CheckboxFlags(LayerNames.FG, ref c, (int) SelectionLayer.FGTiles);

            CustomLayer = (SelectionLayer) c;
        }

        EndMaterialListGUI(searchBar: false);
    }
}
