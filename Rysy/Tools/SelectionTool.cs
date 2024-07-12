using ImGuiNET;
using Rysy;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Gui.Windows;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Layers;
using Rysy.Scenes;
using Rysy.Selections;
using System.Diagnostics.CodeAnalysis;

namespace Rysy.Tools;

public class SelectionTool : Tool, ISelectionHotkeyTool {
    public const string CreatePrefabKeybindName = "selection.createPrefab";
    
    public override string Name => "selection";
    
    public override string PersistenceGroup => "placement";

    private enum States {
        Idle,
        MoveOrResizeGesture,
        RotationGesture,
    }

    private States State = States.Idle;

    private SelectRectangleGesture SelectionGestureHandler;

    private Point? MoveGestureStart, MoveGestureLastMousePos;
    private Vector2 MoveGestureFinalDelta;
    private NineSliceLocation MoveGestureGrabbedLocation;

    private List<Selection>? CurrentSelections;
    private List<Selection> SelectionsToHighlight = new();

    private SelectionLayer CustomLayer;

    private int ClickInPlaceIdx;

    private Point? RotationGestureStart;
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

        this.AddSelectionHotkeys(handler);

        handler.AddHotkeyFromSettings("delete", "delete", DeleteSelections);

        handler.AddHotkeyFromSettings("copy", "ctrl+c", CopySelections);
        handler.AddHotkeyFromSettings("paste", "ctrl+v", PasteSelections);
        handler.AddHotkeyFromSettings("cut", "ctrl+x", CutSelections);
        
        handler.AddHotkeyFromSettings(CreatePrefabKeybindName, "alt+c", CreatePrefab);

        History.OnApply += ClearColliderCachesInSelections;
    }

    private void CreatePrefab() {
        if (CurrentSelections is not { } selections)
            return;

        if (CopypasteHelper.CopySelections(selections) is not { } copied)
            return;

        if (!PrefabHelper.SelectionsLegal(copied))
            return;

        var name = "";
        RysyEngine.Scene.AddWindow(new ScriptedWindow("Create Prefab", (w) => {
            bool invalid = string.IsNullOrWhiteSpace(name) || PrefabHelper.CurrentPrefabs.ContainsKey(name);

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
            Layer = EditorLayers.Room;
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
            MoveSelectionsBy(offset, selections, NineSliceLocation.Middle);
        }
    };

    void ISelectionHotkeyTool.AddNode(Vector2? at) {
        if (CurrentSelections is not { } selections) {
            return;
        }

        List<IHistoryAction> actions = [];
        List<Selection> newSelections = [];
        List<Selection> unselected = [];

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

            CurrentSelections = [..selections.Except(unselected).Concat(newSelections)];
        }
    }

    void ISelectionHotkeyTool.Flip(bool vertical) {
        if (CurrentSelections is not { } selections) {
            return;
        }

        FinalizeStates();
        
        var action = selections
            .Select(s => s.Handler is ISelectionFlipHandler flip ? vertical ? flip.TryFlipVertical() : flip.TryFlipHorizontal()  : null)
            .MergeActions();
        
        if (action.Any())
            ClearColliderCachesInSelections();

        History.ApplyNewAction(action);
    }

    void ISelectionHotkeyTool.Rotate(RotationDirection dir) {
        if (CurrentSelections is not { } selections) {
            return;
        }

        FinalizeStates();
        var action = selections.Select(s => s.Handler is ISelectionFlipHandler flip ? flip.TryRotate(dir) : null).MergeActions();
        if (action.Any())
            ClearColliderCachesInSelections();

        History.ApplyNewAction(action);
    }

    private void MoveSelectionsBy(Vector2 offset, List<Selection> selections, NineSliceLocation grabbed) {
        History.UndoSimulations();
        var action = GetMoveSelectionsByAction(offset, selections, grabbed);

        History.ApplyNewAction(action);
        ClearColliderCachesInSelections();

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

    private IHistoryAction SimulateMoveSelectionsBy(Vector2 offset, List<Selection> selections, NineSliceLocation grabbed) {
        History.UndoSimulations();
        var action = GetMoveSelectionsByAction(offset, selections, grabbed);
        //action.Apply();
        History.ApplyNewSimulation(action);
        ClearColliderCachesInSelections();
        return action;
    }

    private IHistoryAction GetMoveSelectionsByAction(Vector2 offset, List<Selection> selections, NineSliceLocation grabbed)
        => selections.Select(s => GetMoveAction(s.Handler, offset, grabbed)).MergeActions();

    private IHistoryAction? GetMoveAction(ISelectionHandler handler, Vector2 offset, NineSliceLocation grabbed) {
        return handler.GetMoveOrResizeAction(offset, grabbed);
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
        if (CurrentSelections is not { Count: > 0 })
            return;

        foreach (var selection in CurrentSelections) {
            selection.Handler.OnDeselected();
        }

        // clear the list so that the list captured into the history action lambda no longer contains references to the selections, allowing them to get GC'd
        CurrentSelections?.Clear();
        CurrentSelections = null;

        OnSelectionsChanged();
    }

    public void Deselect(ISelectionHandler handler) {
        if (CurrentSelections is null)
            return;

        var selection = CurrentSelections.Find(s => s.Handler == handler);
        if (selection is { }) {
            CurrentSelections.Remove(selection);
            selection.Handler.OnDeselected();

            OnSelectionsChanged();
        }
    }

    private static readonly List<EditorLayer> _ValidLayers = new() {
        EditorLayers.Entities, EditorLayers.Triggers,
        EditorLayers.FgDecals, EditorLayers.BgDecals,
        EditorLayers.Fg, EditorLayers.Bg,
        EditorLayers.Room,
        EditorLayers.All, EditorLayers.CustomLayer
    };

    public override List<EditorLayer> ValidLayers => _ValidLayers;

    public override string GetMaterialDisplayName(EditorLayer layer, object material) {
        throw new NotImplementedException();
    }

    public override IEnumerable<object>? GetMaterials(EditorLayer layer) => Array.Empty<object>();

    public override string? GetMaterialTooltip(EditorLayer layer, object material) {
        throw new NotImplementedException();
    }

    public override void Render(Camera camera, Room room) {
        if (Layer == EditorLayers.Room)
            return;

        DoRender(camera, room);

        SelectionContextWindowRegistry.Render(this, room);
    }

    public override void RenderOverlay() {
        if (Layer != EditorLayers.Room)
            return;

        GFX.EndBatch();
        GFX.BeginBatch(EditorState.Camera!);

        DoRender(EditorState.Camera, EditorState.CurrentRoom);
    }

    private int GetSideGrabLeniency(Camera camera) => (int) (1f / camera.Scale * 6f).AtLeast(1);

    /// <summary>
    /// Adjusts the grabbed location so that we don't try to resize a selection in a unsupported direction.
    /// </summary>
    private static NineSliceLocation AdjustGrabLocBasedOnResizable(NineSliceLocation grabbed, ISelectionHandler handler) {
        if (grabbed == NineSliceLocation.Middle)
            return grabbed;

        var resizableX = handler.ResizableX;
        var resizableY = handler.ResizableY;

        if (grabbed is NineSliceLocation.TopLeft) {
            if (!resizableX)
                grabbed = NineSliceLocation.TopMiddle;
            else if (!resizableY)
                grabbed = NineSliceLocation.Left;
        }
        if (grabbed is NineSliceLocation.TopRight) {
            if (!resizableX)
                grabbed = NineSliceLocation.TopMiddle;
            else if (!resizableY)
                grabbed = NineSliceLocation.Right;
        }

        if (grabbed is NineSliceLocation.BottomLeft) {
            if (!resizableX)
                grabbed = NineSliceLocation.BottomMiddle;
            else if (!resizableY)
                grabbed = NineSliceLocation.Left;
        }
        if (grabbed is NineSliceLocation.BottomRight) {
            if (!resizableX)
                grabbed = NineSliceLocation.BottomMiddle;
            else if (!resizableY)
                grabbed = NineSliceLocation.Right;
        }

        if (grabbed is NineSliceLocation.BottomLeft or NineSliceLocation.BottomRight && !resizableX) {
            grabbed = NineSliceLocation.BottomMiddle;
        }

        if (grabbed is NineSliceLocation.Left or NineSliceLocation.Right && !resizableX) {
            return NineSliceLocation.Middle;
        }

        if (grabbed is NineSliceLocation.TopMiddle or NineSliceLocation.BottomMiddle && !resizableY) {
            return NineSliceLocation.Middle;
        }

        return grabbed;
    }

    private void DoRender(Camera camera, Room? room) {
        if (CurrentSelections is not { Count: > 0 }) {
            // If we're in room selection mode, always select the current room if there are no other selections
            // We do this here instead of Update, as Update is not called when hovering over imgui elements
            if (Layer == EditorLayers.Room && EditorState.CurrentRoom is { } currentRoom) {
                AddSelection(new(currentRoom.GetSelectionHandler()));
            }
        }

        if (SelectionGestureHandler.CurrentRectangle is { } rect) {
            DrawSelectionRect(rect);
        }

        var mousePos = GetMouseRoomPos(camera, room!);
        var imguiWantsMouse = ImGuiManager.WantCaptureMouse || ImGui.IsAnyItemHovered();

        if (CurrentSelections is { } selections) {
            foreach (var selection in selections) {
                if (!imguiWantsMouse && (State != States.Idle || selection.Check(mousePos.X, mousePos.Y))) {
                    SelectionsToHighlight.Add(selection);

                    if (State == States.Idle) {
                        var r = selection.Handler.Rect;
                        var cursorType =
                            AdjustGrabLocBasedOnResizable(r.GetLocationInRect(mousePos, GetSideGrabLeniency(camera)) ?? NineSliceLocation.Middle, selection.Handler)
                            .ToMouseCursor();
                        ImGui.SetMouseCursor(cursorType);
                    }
                } else {
                    selection.Render(Color.Red);
                }
            }
        }

        if (State == States.Idle && !imguiWantsMouse) {
            HandleHoveredSelections(room, SelectionGestureHandler.CurrentRectangle ?? new(mousePos.X, mousePos.Y, 1, 1),
                EditorLayers.ToolLayerToEnum(Layer, CustomLayer), CurrentSelections, Input, middleClick: true
            );
        }

        if (SelectionsToHighlight is { Count: > 0 }) {
            foreach (var selection in SelectionsToHighlight) {
                selection.Render(Color.Gold);
            }

            SelectionsToHighlight.Clear();
        }

        if (RotationGestureStart is { } rotStart) {
            var ctx = SpriteRenderCtx.Default();
            ISprite.Circle(rotStart.ToVector2(), 8, Color.Gray, 32, thickness: 0.5f).Render(ctx);
            ISprite.Circle(rotStart.ToVector2(), 1, Color.Gold, 8).Render(ctx);
            (ISprite.Line(rotStart.ToVector2(), mousePos.ToVector2(), Color.Gold) with {
                Thickness = 0.25f,
            }).Render(ctx);
        }
    }

    internal static void HandleHoveredSelections(Room? room, Rectangle rectangle, SelectionLayer layer,
        IEnumerable<Selection>? selected = null, Input? input = null, bool render = true, bool middleClick = false) {
        input ??= Input.Global;
        
        var canRightClick = input.Mouse.RightClickedInPlace();

        if (!canRightClick && !render && !middleClick)
            return;

        var selectionsUnderCursor = room?.GetSelectionsInRect(rectangle, layer);
        if (selectionsUnderCursor is not { Count: > 0 }) {
            return;
        }

        var firstSelection = selectionsUnderCursor[0];
        if (firstSelection.Handler is TileSelectionHandler) {
            canRightClick = false;
        }

        if (render) {
            foreach (var selection in selectionsUnderCursor) {
                if (selection.Handler is TileSelectionHandler)
                    continue;
                
                selection.Render(Color.Pink);
            }
        }

        if (canRightClick && selected is { }) {
            // if we're hovering over an active selection, right clicking will select that instead,
            // let's not right click a unselected item in this case
            foreach (var curr in selected) {
                if (selectionsUnderCursor.Any(x => x.Handler == curr.Handler)) {
                    canRightClick = false;
                    break;
                }
            }
        }

        // allow right clicking a un-selected item
        if (canRightClick) {
            firstSelection.Handler.OnRightClicked(new Selection[] { firstSelection });
        }

        if (input.Mouse.Middle.Clicked() && RysyEngine.Scene is EditorScene editor && editor.ToolHandler.GetTool<PlacementTool>() is {} placementTool) {
            if (placementTool.ValidLayers.Select(x => EditorLayers.ToolLayerToEnum(x)).Any(x => x == firstSelection.Handler.Layer)) {
                // TODO: Create a proper helper for this!
                if (EditorLayers.LayerFromSelectionLayer(firstSelection.Handler.Layer) is { } editorLayer) {
                    input.Mouse.ConsumeMiddle();
                    editor.ToolHandler.SetTool<PlacementTool>();
                    placementTool.Layer = editorLayer;
                    placementTool.OnMiddleClick();
                }

            }
        }
    }

    private Rectangle CreateMousePosRect(Camera camera, Room room, out Point mouseRoomPos) {
        mouseRoomPos = GetMouseRoomPos(camera, room);
        return new Rectangle(mouseRoomPos.X, mouseRoomPos.Y, 1, 1);
    }

    private void FinalizeStates() {
        History.UndoSimulations();

        if (State == States.MoveOrResizeGesture) {
            FinalizeMove(EditorState.Camera, EditorState.CurrentRoom!);
        }
    }

    public override void Update(Camera camera, Room room) {
        if (CurrentSelections is { } selections) {
            var mouseRoomPos = GetMouseRoomPos(camera, room);
            var mouseRect = new Rectangle(mouseRoomPos.X, mouseRoomPos.Y, 1, 1);
            if (Input.Mouse.Left.Clicked()) {
                foreach (var selection in selections) {
                    if (selection.Check(mouseRect)) {
                        MoveGestureStart = mouseRoomPos;
                        State = States.MoveOrResizeGesture;
                        MoveGestureGrabbedLocation = selection.Handler.Rect.GetLocationInRect(mouseRoomPos, GetSideGrabLeniency(camera)) ?? NineSliceLocation.Middle;
                        MoveGestureGrabbedLocation = AdjustGrabLocBasedOnResizable(MoveGestureGrabbedLocation, selection.Handler);
                        break;
                    }
                }
            }

            if (Input.Mouse.RightClickedInPlace()) {
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
            case States.MoveOrResizeGesture:
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

        EndMoveGesture(false, null, null);
        SelectionGestureHandler.CancelGesture();
        Deselect();
    }

    public override bool AllowSwappingRooms => Layer != EditorLayers.Room;

    private IHistoryAction? GetPreciseRotationAction(float realAngle) {
        if (CurrentSelections is null)
            return null;

        var actions = new List<IHistoryAction>();
        foreach (var s in CurrentSelections) {
            if (s.Handler is ISelectionPreciseRotationHandler rotationHandler) {
                if (rotationHandler.TryPreciseRotate(realAngle, RotationGestureStart!.Value.ToVector2()) is { } act) {
                    actions.Add(act);
                }
                s.Handler.ClearCollideCache();
            }
        }

        return actions.MergeActions();
    }

    private void EndRotationGesture(float angle) {
        if (CurrentSelections is null) {
            return;
        }

        History.UndoSimulations();
        if (angle != 0f)
            History.ApplyNewAction(GetPreciseRotationAction(angle));
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
        }

        CreateMousePosRect(camera, room, out var currentPos);
        var angle = VectorExt.Angle(RotationGestureStart.Value.ToVector2(), currentPos.ToVector2());
        var realAngle = angle;
        if (RotationGestureLastAngle is null) {
            RotationGestureLastAngle = angle;
            angle = 0f;
        } else {
            (angle, RotationGestureLastAngle) = (angle - RotationGestureLastAngle.Value, angle);
        }

        if (!Input.Keyboard.Shift() || CurrentSelections is null) {
            EndRotationGesture(angle);
            State = States.MoveOrResizeGesture;
            return;
        }

        switch (Input.Mouse.Left) {
            case MouseInputState.Held: {
                History.UndoSimulations();
                History.ApplyNewSimulation(GetPreciseRotationAction(realAngle));

                break;
            }
            case MouseInputState.Released: {
                EndRotationGesture(realAngle);
                State = States.Idle;

                break;
            }
        }
    }

    private void FinalizeMove(Camera camera, Room room) {
        if (CurrentSelections is { } selections && MoveGestureStart is { } start) {
            Point mousePos = GetMouseRoomPos(camera, room);
            Vector2 delta = MoveGestureFinalDelta;

            if (delta.LengthSquared() <= 0) {
                // If you just clicked in place, then act as if you wanted to simply change your selection, instead of moving
                SelectWithin(room, new Rectangle(mousePos.X, mousePos.Y, 1, 1));
            } else {
                MoveSelectionsBy(delta, selections, MoveGestureGrabbedLocation);
            }

            MoveGestureStart = mousePos;
            MoveGestureFinalDelta = default;
        }
    }
    
    private void EndMoveGesture(bool simulate, Camera? camera, Room? room) {
        if (State != States.MoveOrResizeGesture)
            return;

        History.UndoSimulations();

        if (simulate && room is {} && camera is {}) {
            FinalizeMove(camera, room);
        }
        
        MoveGestureStart = null;
        MoveGestureLastMousePos = null;
        MoveGestureFinalDelta = Vector2.Zero;
        State = States.Idle;
        MoveGestureGrabbedLocation = NineSliceLocation.Middle;
    }

    private void UpdateMoveGesture(Camera camera, Room room) {
        var left = Input.Mouse.Left;

        if (Input.Keyboard.Shift()) {
            EndMoveGesture(false, camera, room);
            State = States.RotationGesture;
            return;
        }

        switch (left) {
            case MouseInputState.Released: {
                EndMoveGesture(true, camera, room);
                break;
            }
            case MouseInputState.Held: {
                if (CurrentSelections is { } selections && MoveGestureStart is { } start) {
                    var mousePos = GetMouseRoomPos(camera, room);
                    MoveGestureLastMousePos ??= mousePos;

                    Vector2 delta = CalculateMovementDelta(MoveGestureLastMousePos!.Value, mousePos);

                    if (delta.LengthSquared() != 0) {
                        MoveGestureLastMousePos += delta.ToPoint();
                        MoveGestureFinalDelta += delta;
                        //Console.WriteLine(MoveGestureFinalDelta);
                        SimulateMoveSelectionsBy(MoveGestureFinalDelta, selections, MoveGestureGrabbedLocation);
                    }

                    var cursorType = MoveGestureGrabbedLocation.ToMouseCursor();
                    ImGui.SetMouseCursor(cursorType);
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

    private void UpdateDragGesture(Camera camera, Room room) {
        if (SelectionGestureHandler.Update((p) => GetMouseRoomPos(camera, room, p)) is { } rect) {
            SelectWithin(room, rect);
        }
    }

    private void SelectWithin(Room room, Rectangle rect) {
        var selections = room.GetSelectionsInRect(rect, EditorLayers.ToolLayerToEnum(Layer, CustomLayer));
        List<Selection>? finalSelections = null;

        if (rect.Width <= 1 && rect.Height <= 1 && selections.Count > 0) {
            // you just clicked in place, select only 1 selection
            if (selections.Count > 1) {
                int idx = ClickInPlaceIdx % selections.Count;
                ClickInPlaceIdx = (idx + 1) % selections.Count;

                finalSelections = selections.OrderBy(s => s.Handler.Parent is IDepth d ? d.Depth : int.MinValue).Take(idx..(idx + 1)).ToList();
            } else if (CurrentSelections?.Count > 0 && Input.Mouse.LeftDoubleClicked()) {
                // if you double clicked in place, select all similar entities/decals
                finalSelections = room.GetSelectionsForSimilar(selections[0].Handler.Parent)!;
                ClickInPlaceIdx = 0;
            }

        }

        if (finalSelections == null) {
            finalSelections = selections;
            ClickInPlaceIdx = 0;
        }

        // Deselect all current selections, we will call OnSelected on all remaining ones at the end anyway
        if (CurrentSelections is { })
            foreach (var selection in CurrentSelections) {
                selection.Handler.OnDeselected();
            }

        if (Input.Keyboard.Shift() && CurrentSelections is { }) {
            // Add new selections

            foreach (var h in CurrentSelections.SelectWhereNotNull(s => s.Handler as TileSelectionHandler))
                h.MergeWith(rect, exclude: false);

            CurrentSelections = CurrentSelections
                .Concat(finalSelections)
                .DistinctBy(x => x.Handler.Parent)
                .ToList();
        } else if (Input.Keyboard.Ctrl() && CurrentSelections is { }) {
            // Remove existing selections
            var newSelections = CurrentSelections.Except(finalSelections, new HandlerParentEqualityComparer());

            // tile selections are unique - they need to remain in the selection list, and we need to call MergeWith
            foreach (var tileSelection in CurrentSelections.Where(s => s.Handler is TileSelectionHandler)) {
                if (tileSelection is { Handler: TileSelectionHandler tileHandler }) {
                    tileHandler.MergeWith(rect, exclude: true);

                    newSelections = newSelections.Append(tileSelection);
                }
            }

            CurrentSelections = newSelections.DistinctBy(x => x.Handler.Parent).ToList();
        } else {
            // Set selections
            Deselect();
            CurrentSelections = finalSelections;
        }

        // Tell the handlers that they're selected
        foreach (var selection in CurrentSelections) {
            selection.Handler.OnSelected();
        }

        OnSelectionsChanged();
    }

    private void OnSelectionsChanged() {
        if (CurrentSelections is [{ Handler: RoomSelectionHandler roomSelection }]) {
            // you only selected 1 room, let's swap to that room as well
            EditorState.CurrentRoom = roomSelection.Room;
        }
    }

    public void AddSelection(Selection selection) {
        CurrentSelections = CurrentSelections?
        .Append(selection)
        .DistinctBy(x => x.Handler.Parent)
        .ToList() ?? [ selection ];

        selection.Handler.OnSelected();
        OnSelectionsChanged();
    }

    private struct HandlerParentEqualityComparer : IEqualityComparer<Selection> {
        public bool Equals(Selection x, Selection y) {
            return x.Handler.Parent == y.Handler.Parent;
        }

        public int GetHashCode(Selection obj) {
            return obj.Handler.Parent.GetHashCode();
        }
    }

    public override void RenderMaterialList(Vector2 size, out bool showSearchBar) {
        showSearchBar = false;

        if (Layer == EditorLayers.CustomLayer) {
            var c = (int) CustomLayer;
            ImGui.CheckboxFlags(EditorLayers.Entities.LocalizedName, ref c, (int) SelectionLayer.Entities);
            ImGui.CheckboxFlags(EditorLayers.Triggers.LocalizedName, ref c, (int) SelectionLayer.Triggers);
            ImGui.CheckboxFlags(EditorLayers.FgDecals.LocalizedName, ref c, (int) SelectionLayer.FGDecals);
            ImGui.CheckboxFlags(EditorLayers.BgDecals.LocalizedName, ref c, (int) SelectionLayer.BGDecals);
            ImGui.CheckboxFlags(EditorLayers.Bg.LocalizedName, ref c, (int) SelectionLayer.BGTiles);
            ImGui.CheckboxFlags(EditorLayers.Fg.LocalizedName, ref c, (int) SelectionLayer.FGTiles);

            CustomLayer = (SelectionLayer) c;

            ImGui.Separator();
        }

        ImGui.Text("Selections");
        if (CurrentSelections is [_, ..] && ImGuiManager.TranslatedButton("rysy.createPrefab")) {
            CreatePrefab();
        }

        if (!ImGui.BeginTable("Selections", 3, ImGuiManager.TableFlags)) {
            return;
        }

        var textBaseWidth = ImGui.CalcTextSize("m").X;

        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, textBaseWidth * 30f);
        ImGui.TableSetupColumn("Deselect", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Edit", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        if (CurrentSelections is { })
            foreach (var selection in CurrentSelections) {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                switch (selection.Handler) {
                    case EntitySelectionHandler entity:
                        ImGui.Text(entity.Entity.Name);
                        break;
                    case NodeSelectionHandler node:
                        ImGui.Text(Interpolator.Temp($"{node.Entity.Name}[{node.NodeIdx}]"));
                        break;
                    case RoomSelectionHandler room:
                        ImGui.Text(room.Room.Name);
                        break;
                    case TileSelectionHandler tiles:
                        ImGui.Text(tiles.Layer.FastToString());
                        break;
                }
                HighlightIfHovered(SelectionsToHighlight, selection);
                ImGui.TableNextColumn();

                ImGuiManager.PushNullStyle();
                if (RysyEngine.Scene is EditorScene && ImGui.Selectable(Interpolator.Temp($"Deselect##{selection.GetHashCode()}"))) {
                    DeselectOnEndOfFrame(selection);
                }
                HighlightIfHovered(SelectionsToHighlight, selection);
                ImGui.TableNextColumn();

                if (RysyEngine.Scene is EditorScene && ImGui.Selectable(Interpolator.Temp($"Edit##{selection.GetHashCode()}"))) {
                    RightClickOnEndOfFrame(selection);
                }
                HighlightIfHovered(SelectionsToHighlight, selection);

                ImGuiManager.PopNullStyle();

                static void HighlightIfHovered(List<Selection> into, Selection selection) {
                    if (ImGui.IsItemHovered()) {
                        into.Add(selection);
                    }
                }
            }

        ImGui.EndTable();
    }
    
    void DeselectOnEndOfFrame(Selection selection) {
        RysyState.OnEndOfThisFrame += () => Deselect(selection.Handler);
    }
    
    void RightClickOnEndOfFrame(Selection selection) {
        RysyState.OnEndOfThisFrame += () => selection.Handler.OnRightClicked([selection]);
    }
}
