using ImGuiNET;
using Rysy;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Gui.Windows;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Scenes;
using Rysy.Selections;
using System.Diagnostics.CodeAnalysis;

namespace Rysy.Tools;

public class SelectionTool : Tool {
    public override string Name => "selection";

    private enum States {
        Idle,
        MoveGesture,
        RotationGesture,
    }

    private States State = States.Idle;

    private SelectRectangleGesture SelectionGestureHandler;

    private Point? MoveGestureStart, MoveGestureLastMousePos;
    private Vector2 MoveGestureFinalDelta;

    private List<Selection>? CurrentSelections;
    private List<Selection> SelectionsToHighlight = new();

    private SelectionLayer CustomLayer;

    private int ClickInPlaceIdx;

    private Point? RotationGestureStart;
    private Rectangle? RotationGestureRect;
    private float? RotationGestureLastAngle;



    public SelectionTool() {
    }

    public override void Init() {
        base.Init();

        SelectionGestureHandler = new(Input);
    }

    public override void InitHotkeys(HotkeyHandler handler) {
        base.InitHotkeys(handler);

        handler.AddHotkeyFromSettings("selection.moveLeft", "left", CreateMoveHandler(new(-8, 0)), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.moveRight", "right", CreateMoveHandler(new(8, 0)), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.moveUp", "up", CreateMoveHandler(new(0, -8)), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.moveDown", "down", CreateMoveHandler(new(0, 8)), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.moveLeftPixel", "ctrl+left", CreateMoveHandler(new(-1, 0)), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.moveRightPixel", "ctrl+right", CreateMoveHandler(new(1, 0)), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.moveUpPixel", "ctrl+up", CreateMoveHandler(new(0, -1)), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.moveDownPixel", "ctrl+down", CreateMoveHandler(new(0, 1)), HotkeyModes.OnHoldSmoothInterval);

        handler.AddHotkeyFromSettings("selection.upsizeLeft", "a", CreateUpsizeHandler(new(8, 0), new(-8, 0)), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.upsizeRight", "d", CreateUpsizeHandler(new(8, 0), new()), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.upsizeTop", "w", CreateUpsizeHandler(new(0, 8), new(0, -8)), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.upsizeBottom", "s", CreateUpsizeHandler(new(0, 8), new()), HotkeyModes.OnHoldSmoothInterval);

        handler.AddHotkeyFromSettings("selection.downsizeLeft", "shift+d", CreateUpsizeHandler(new(-8, 0), new(8, 0)), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.downsizeRight", "shift+a", CreateUpsizeHandler(new(-8, 0), new()), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.downsizeTop", "shift+s", CreateUpsizeHandler(new(0, -8), new(0, 8)), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.downsizeBottom", "shift+w", CreateUpsizeHandler(new(0, -8), new()), HotkeyModes.OnHoldSmoothInterval);

        handler.AddHotkeyFromSettings("selection.flipHorizontal", "h", HorizontalFlipSelections);
        handler.AddHotkeyFromSettings("selection.flipVertical", "v", VerticalFlipSelections);
        handler.AddHotkeyFromSettings("selection.rotateRight", "r", () => RotateSelections(RotationDirection.Right));
        handler.AddHotkeyFromSettings("selection.rotateLeft", "l", () => RotateSelections(RotationDirection.Left));

        handler.AddHotkeyFromSettings("delete", "delete", DeleteSelections);

        handler.AddHotkeyFromSettings("selection.addNode", "shift+n", () => AddNodeKeybind(at: null));
        handler.AddHotkeyFromSettings("selection.addNodeAtMouse", "n", () => AddNodeKeybind(at: GetMouseRoomPos(EditorState.Camera!, EditorState.CurrentRoom!).ToVector2().Snap(8)));

        handler.AddHotkeyFromSettings("copy", "ctrl+c", CopySelections);
        handler.AddHotkeyFromSettings("paste", "ctrl+v", PasteSelections);
        handler.AddHotkeyFromSettings("cut", "ctrl+x", CutSelections);

        handler.AddHotkeyFromSettings("selection.createPrefab", "alt+c", CreatePrefab);

        History.OnApply += ClearColliderCachesInSelections;
    }

    private void CreatePrefab() {
        if (CurrentSelections is not { } selections)
            return;

        if (CopypasteHelper.CopySelections(CurrentSelections) is not { } copied)
            return;

        if (!PrefabHelper.SelectionsLegal(copied))
            return;

        var name = "";
        RysyEngine.Scene.AddWindow(new ScriptedWindow("Create Prefab", (w) => {
            bool invalid = name == "" || PrefabHelper.CurrentPrefabs.ContainsKey(name);

            ImGuiManager.PushInvalidStyleIf(invalid);
            if (ImGui.InputText("Name", ref name, 64, ImGuiInputTextFlags.EnterReturnsTrue)) {
                PrefabHelper.RegisterPrefab(name, copied);
                w.RemoveSelf();
            }
            ImGuiManager.PopInvalidStyle();

            ImGui.BeginDisabled(invalid);
            if (ImGui.Button("Create")) {
                PrefabHelper.RegisterPrefab(name, copied);
                w.RemoveSelf();
            }
            ImGui.EndDisabled();
        }, size: new(300, ImGui.GetTextLineHeightWithSpacing() * 3f + ImGui.GetFrameHeightWithSpacing())));
    }

    private void CutSelections() {
        if (CurrentSelections is not { } selections) {
            return;
        }

        CopySelections();
        DeleteSelections();
    }

    private void PasteSelections() {
        if (EditorState.CurrentRoom is null)
            return;

        var selections = CopypasteHelper.PasteSelectionsFromClipboard(History, EditorState.Map, EditorState.CurrentRoom, GetMouseRoomPos(EditorState.Camera, EditorState.CurrentRoom).ToVector2(), out bool pastedRooms);
        if (pastedRooms) {
            Layer = LayerNames.ROOM;
        }

        if (selections != null) {
            Deselect();
            CurrentSelections = selections;
        }

        Input.Keyboard.ConsumeKeyClick(Microsoft.Xna.Framework.Input.Keys.LeftControl);
        Input.Keyboard.ConsumeKeyClick(Microsoft.Xna.Framework.Input.Keys.RightControl);
    }

    private void CopySelections() {
        CopypasteHelper.CopySelectionsToClipboard(CurrentSelections);
    }

    private Action CreateUpsizeHandler(Point resize, Vector2 move) => () => {
        if (CurrentSelections is { } selections) {
            ResizeSelectionsBy(resize, move, selections);
        }
    };

    private Action CreateMoveHandler(Vector2 offset) => () => {
        if (CurrentSelections is { } selections) {
            MoveSelectionsBy(offset, selections);
        }
    };

    private void AddNodeKeybind(Vector2? at) {
        if (CurrentSelections is not { } selections) {
            return;
        }

        List<IHistoryAction> actions = new();
        List<Selection> newSelections = new();
        List<Selection> unselected = new();

        foreach (var s in selections) {
            if (s.Handler.TryAddNode(at) is { } res) {
                actions.Add(res.Item1);
                newSelections.Add(new() { Handler = res.Item2 });
                unselected.Add(s);
            }
        }

        if (actions.Count > 0) {
            ClearColliderCachesInSelections();
            History.ApplyNewAction(actions.MergeActions());

            CurrentSelections = new(selections.Except(unselected).Concat(newSelections));
        }
    }

    private void HorizontalFlipSelections() {
        if (CurrentSelections is not { } selections) {
            return;
        }

        var action = selections.Select(s => s.Handler is ISelectionFlipHandler flip ? flip.TryFlipHorizontal() : null).MergeActions();
        if (action.Any())
            ClearColliderCachesInSelections();

        History.ApplyNewAction(action);
    }

    private void VerticalFlipSelections() {
        if (CurrentSelections is not { } selections) {
            return;
        }

        var action = selections.Select(s => s.Handler is ISelectionFlipHandler flip ? flip.TryFlipVertical() : null).MergeActions();
        if (action.Any())
            ClearColliderCachesInSelections();

        History.ApplyNewAction(action);
    }

    private void RotateSelections(RotationDirection dir) {
        if (CurrentSelections is not { } selections) {
            return;
        }

        var action = selections.Select(s => s.Handler is ISelectionFlipHandler flip ? flip.TryRotate(dir) : null).MergeActions();
        if (action.Any())
            ClearColliderCachesInSelections();

        History.ApplyNewAction(action);
    }

    private void MoveSelectionsBy(Vector2 offset, List<Selection> selections) {
        var action = selections.Select(s => s.Handler.MoveBy(offset)).MergeActions();

        ClearColliderCachesInSelections();

        History.ApplyNewAction(action);
    }

    private void ResizeSelectionsBy(Point resize, Vector2 move, List<Selection> selections) {
        var actions = selections.Select(s => s.Handler.TryResize(resize));
        if (move != default) {
            actions = actions.Concat(selections.Select(s => s.Handler.MoveBy(move)));
        }

        var merged = actions.MergeActions();

        if (merged.Any()) {
            ClearColliderCachesInSelections();
        }

        History.ApplyNewAction(merged);
    }

    private void SimulateMoveSelectionsBy(Vector2 offset, List<Selection> selections) {
        foreach (var s in selections) {
            s.Handler.MoveBy(offset).Apply();
            s.Handler.ClearCollideCache();
        }
    }

    public void DeleteSelections() {
        if (CurrentSelections is { } selections) {
            var action = selections.Select(s => s.Handler.DeleteSelf()).MergeActions();
            Deselect();
            ClearColliderCachesInSelections();

            History.ApplyNewAction(action);
        }
    }

    private void ClearColliderCachesInSelections() => CurrentSelections?.ForEach(s => s.Handler.ClearCollideCache());

    public void Deselect() {
        if (CurrentSelections is null)
            return;

        foreach (var selection in CurrentSelections) {
            selection.Handler.OnDeselected();
        }

        // clear the list so that the list captured into the history action lambda no longer contains references to the selections, allowing them to get GC'd
        CurrentSelections?.Clear();
        CurrentSelections = null;
    }

    public void Deselect(ISelectionHandler handler) {
        if (CurrentSelections is null)
            return;

        var selection = CurrentSelections.Find(s => s.Handler == handler);
        if (selection is { }) {
            CurrentSelections.Remove(selection);
            selection.Handler.OnDeselected();
        }
    }

    public override string PersistenceGroup => "Selection";

    private static readonly List<string> _ValidLayers = new() {
        LayerNames.ENTITIES, LayerNames.TRIGGERS,
        LayerNames.FG_DECALS, LayerNames.BG_DECALS,
        LayerNames.FG, LayerNames.BG,
        LayerNames.ROOM,
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
        if (Layer == LayerNames.ROOM)
            return;

        DoRender(camera, room);

        SelectionContextWindowRegistry.Render(this, room);
    }

    public override void RenderOverlay() {
        if (Layer != LayerNames.ROOM)
            return;

        GFX.EndBatch();
        GFX.BeginBatch(EditorState.Camera!);

        DoRender(EditorState.Camera, null);
    }

    private void DoRender(Camera camera, Room? room) {
        if (SelectionGestureHandler.CurrentRectangle is { } rect) {
            DrawSelectionRect(rect);
        }

        var mousePos = GetMouseRoomPos(camera, room!);

        if (CurrentSelections is { } selections)
            foreach (var selection in selections) {
                if (selection.Check(new(mousePos.X, mousePos.Y, 1, 1))) {
                    SelectionsToHighlight.Add(selection);
                } else {
                    selection.Render(Color.Red);
                }
            }

        if (SelectionsToHighlight is { Count: > 0}) {
            foreach (var selection in SelectionsToHighlight) {
                selection.Render(Color.Gold);
            }

            SelectionsToHighlight.Clear();
        }
    }

    private Rectangle CreateMousePosRect(Camera camera, Room room, out Point mouseRoomPos) {
        mouseRoomPos = GetMouseRoomPos(camera, room);
        return new Rectangle(mouseRoomPos.X, mouseRoomPos.Y, 1, 1);
    }

    private Selection? GetHoveredSelection(Camera camera, Room room) {
        if (CurrentSelections is null)
            return null;

        var mouseRect = CreateMousePosRect(camera, room, out _);

        foreach (var selection in CurrentSelections) {
            if (selection.Check(mouseRect)) {
                return selection;
            }
        }

        return null;
    }

    public override void Update(Camera camera, Room room) {
        if (CurrentSelections is { } selections) {
            if (Input.Mouse.Left.Clicked()) {
                var mouseRoomPos = GetMouseRoomPos(camera, room);
                var mouseRect = new Rectangle(mouseRoomPos.X, mouseRoomPos.Y, 1, 1);

                foreach (var selection in selections) {
                    if (selection.Check(mouseRect)) {
                        State = States.MoveGesture;
                        MoveGestureStart = mouseRoomPos;
                        break;
                    }
                }
            }

            if (Input.Mouse.Right.Clicked()) {
                var mouseRoomPos = GetMouseRoomPos(camera, room);
                var mouseRect = new Rectangle(mouseRoomPos.X, mouseRoomPos.Y, 1, 1);

                foreach (var selection in selections) {
                    if (selection.Check(mouseRect)) {
                        selection.Handler.OnRightClicked(selections);

                        break;
                    }
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
            case States.RotationGesture:
                UpdateRotationGesture(camera, room);
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

    public override bool AllowSwappingRooms => Layer != LayerNames.ROOM;

    private void EndRotationGesture(float angle) {
        if (CurrentSelections is null) {
            return;
        }

        var actions = CurrentSelections.Select(s => s.Handler).OfType<ISelectionPreciseRotationHandler>().Select(h => h.TryPreciseRotate(angle, isSimulation: false));

        History.ApplyNewAction(actions);
        ClearColliderCachesInSelections();
        RotationGestureStart = null;
    }

    private void UpdateRotationGesture(Camera camera, Room room) {
        if (CurrentSelections is null) {
            State = States.Idle;
            EndRotationGesture(0f);
            return;
        }

        if (RotationGestureStart is null) {
            CreateMousePosRect(camera, room, out var start);
            RotationGestureStart = start;
            RotationGestureLastAngle = null;
            RotationGestureRect = null;
        }

        CreateMousePosRect(camera, room, out var currentPos);
        var angle = VectorExt.Angle(RotationGestureStart.Value.ToVector2(), currentPos.ToVector2()) + (MathF.PI / 2f);
        //Console.WriteLine((angle, RotationGestureLastAngle));
        if (RotationGestureLastAngle is null) {
            RotationGestureLastAngle = angle;
            angle = 0f;
        } else {
            (angle, RotationGestureLastAngle) = (angle - RotationGestureLastAngle.Value, angle);
        }
        // RotationGestureLastAngle = angle;
        //angle -= RotationGestureLastAngle;
        //RotationGestureRect ??= RectangleExt.Merge(CurrentSelections.Select(s => s.Handler.Rect));
        //var rect = RotationGestureRect.Value;
        //var center = rect.Center;

        if (!Input.Keyboard.Shift() || CurrentSelections is null) {
            EndRotationGesture(angle);
            State = States.MoveGesture;
            return;
        }

        switch (Input.Mouse.Left) {
            case MouseInputState.Held: {
                foreach (var s in CurrentSelections) {
                    if (s.Handler is ISelectionPreciseRotationHandler rotationHandler) {
                        rotationHandler.TryPreciseRotate(angle, isSimulation: true)?.Apply();
                        //var offset = (center - s.Handler.Rect.Center).ToVector2().Rotate(angle);
                        //s.Handler.MoveBy(offset).Apply();
                        s.Handler.ClearCollideCache();
                    }
                }

                break;
            }
            case MouseInputState.Released: {
                EndRotationGesture(angle);
                State = States.Idle;


                //foreach (var s in CurrentSelections) {
                //    if (s is ISelectionPreciseRotationHandler rotationHandler) {
                //        rotationHandler.TryPreciseRotate(angle);
                //    }
                //}

                break;
            }
        }
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

        if (Input.Keyboard.Shift()) {
            EndMoveGesture(false);
            State = States.RotationGesture;
            return;
        }

        switch (left) {
            case MouseInputState.Released: {
                if (CurrentSelections is { } selections && MoveGestureStart is { } start) {
                    Point mousePos = GetMouseRoomPos(camera, room);
                    Vector2 delta = MoveGestureFinalDelta;

                    if (delta.LengthSquared() <= 0) {
                        // If you just clicked in place, then act as if you wanted to simply change your selection, instead of moving
                        SelectWithin(room, new Rectangle(mousePos.X, mousePos.Y, 1, 1));
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

    private Vector2 CalculateMovementDelta(Point start, Point mousePos) {
        var delta = (mousePos - start).ToVector2();
        delta = SnapToGridIfNeeded(delta);

        return delta;
    }

    private Vector2 SnapToGridIfNeeded(Vector2 pos) {
        if (!Input.Keyboard.Ctrl()) {
            pos = pos.GridPosRound(8).ToVector2() * 8f;
        }

        return pos;
    }

    private Point GetMouseRoomPos(Camera camera, Room room, Point? pos = default) {
        if (Layer == LayerNames.ROOM)
            return camera.ScreenToReal(pos ?? Input.Mouse.Pos);
        return room.WorldToRoomPos(camera, pos ?? Input.Mouse.Pos);
    }

    private void UpdateDragGesture(Camera camera, Room room) {
        if (SelectionGestureHandler.Update((p) => GetMouseRoomPos(camera, room, p)) is { } rect) {
            SelectWithin(room, rect);
        }
    }

    private void SelectWithin(Room room, Rectangle rect) {
        var selections = room.GetSelectionsInRect(rect, LayerNames.ToolLayerToEnum(Layer, CustomLayer));
        IEnumerable<Selection> finalSelections = null!;

        if (rect.Width <= 1 && rect.Height <= 1 && selections.Count > 0) {
            // you just clicked in place, select only 1 selection
            if (selections.Count > 1) {
                int idx = ClickInPlaceIdx % selections.Count;
                ClickInPlaceIdx = (idx + 1) % selections.Count;

                finalSelections = selections.OrderBy(s => s.Handler.Parent is IDepth d ? d.Depth : int.MinValue).Take(idx..(idx + 1));
            } else if (CurrentSelections?.Count > 0 && Input.Mouse.LeftDoubleClicked()) {
                // if you double clicked in place, select all simillar entities/decals
                finalSelections = room.GetSelectionsForSimillar(selections[0].Handler.Parent)!;
                ClickInPlaceIdx = 0;
            }

        }

        if (finalSelections == null) {
            finalSelections = selections;
            ClickInPlaceIdx = 0;
        }

        if (Input.Keyboard.Shift() && CurrentSelections is { }) {
            foreach (var h in CurrentSelections.SelectWhereNotNull(s => s.Handler as TileSelectionHandler))
                h.MergeWith(rect, exclude: false);

            CurrentSelections = CurrentSelections
                .Concat(finalSelections)
                .DistinctBy(x => x.Handler.Parent)
                .ToList();
        } else if (Input.Keyboard.Ctrl() && CurrentSelections is { }) {
            var newSelections = CurrentSelections.Except(finalSelections, new HandlerParentEqualityComparer());

            // tile selections are unique - they need to remain in the selection list, and we need to call MergeWith
            foreach (var tileSelection in CurrentSelections.Where(s => s.Handler is TileSelectionHandler)) {
                if (tileSelection is { Handler: TileSelectionHandler tileHandler }) {
                    tileHandler.MergeWith(rect, exclude: true);

                    newSelections = newSelections.Append(tileSelection);
                }
            }

            CurrentSelections = newSelections.DistinctBy(x => x.Handler.Parent).ToList();
            ;
        } else {
            Deselect();
            CurrentSelections = finalSelections.ToList();
        }
    }

    public void AddSelection(Selection selection) {
        CurrentSelections = CurrentSelections?
        .Append(selection)
        .DistinctBy(x => x.Handler.Parent)
        .ToList() ?? new() { selection };
    }

    private struct HandlerParentEqualityComparer : IEqualityComparer<Selection> {
        public bool Equals(Selection? x, Selection? y) {
            return x!.Handler.Parent == y!.Handler.Parent;
        }

        public int GetHashCode([DisallowNull] Selection obj) {
            return obj.Handler.Parent.GetHashCode();
        }
    }

    public override void RenderMaterialList(Vector2 size, out bool showSearchBar) {
        showSearchBar = false;

        if (Layer == LayerNames.CUSTOM_LAYER) {
            var c = (int) CustomLayer;
            ImGui.CheckboxFlags(LayerNames.ENTITIES, ref c, (int) SelectionLayer.Entities);
            ImGui.CheckboxFlags(LayerNames.TRIGGERS, ref c, (int) SelectionLayer.Triggers);
            ImGui.CheckboxFlags(LayerNames.FG_DECALS, ref c, (int) SelectionLayer.FGDecals);
            ImGui.CheckboxFlags(LayerNames.BG_DECALS, ref c, (int) SelectionLayer.BGDecals);
            ImGui.CheckboxFlags(LayerNames.BG, ref c, (int) SelectionLayer.BGTiles);
            ImGui.CheckboxFlags(LayerNames.FG, ref c, (int) SelectionLayer.FGTiles);

            CustomLayer = (SelectionLayer) c;

            ImGui.Separator();
        }

        ImGui.Text("Selections");

        if (!ImGui.BeginTable("Selections", 3, ImGuiManager.TableFlags)) {
            return;
        }

        var textBaseWidth = ImGui.CalcTextSize("m").X;

        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, textBaseWidth * 30f);
        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        if (CurrentSelections is { })
            foreach (var selection in CurrentSelections) {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                var parent = selection.Handler.Parent;

                switch (selection.Handler) {
                    case EntitySelectionHandler entity:
                        ImGui.Text(entity.Entity.Name);
                        break;
                    case NodeSelectionHandler node:
                        ImGui.Text($"{node.Entity.Name}[{node.NodeIdx}]");
                        break;
                    case RoomSelectionHandler room:
                        ImGui.Text(room.Room.Name);
                        break;
                    case TileSelectionHandler tiles:
                        ImGui.Text(tiles.Layer.FastToString());
                        break;
                    default:
                        break;
                }
                HighlightIfHovered(selection);
                ImGui.TableNextColumn();

                ImGuiManager.PushNullStyle();
                if (RysyEngine.Scene is EditorScene editor && ImGui.Selectable($"Deselect...##{selection.GetHashCode()}")) {
                    RysyEngine.OnEndOfThisFrame += () => Deselect(selection.Handler);
                }
                HighlightIfHovered(selection);
                ImGui.TableNextColumn();

                if (RysyEngine.Scene is EditorScene && ImGui.Selectable($"Edit...##{selection.GetHashCode()}")) {
                    RysyEngine.OnEndOfThisFrame += () => selection.Handler.OnRightClicked(new Selection[] { selection });
                }
                HighlightIfHovered(selection);

                ImGuiManager.PopNullStyle();

                void HighlightIfHovered(Selection selection) {
                    if (ImGui.IsItemHovered()) {
                        SelectionsToHighlight.Add(selection);
                    }
                }
            }

        ImGui.EndTable();
    }
}
