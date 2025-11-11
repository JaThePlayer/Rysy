using Hexa.NET.ImGui;
using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Gui.Windows;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Layers;
using Rysy.Scenes;
using Rysy.Selections;

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

    private States _state = States.Idle;

    private SelectRectangleGesture _selectionGestureHandler;

    private Point? _moveGestureStart, _moveGestureLastMousePos;
    private Vector2 _moveGestureFinalDelta;
    private NineSliceLocation _moveGestureGrabbedLocation;

    private List<Selection>? _currentSelections;
    private List<Selection> _selectionsToHighlight = new();

    private SelectionLayer _customLayer;

    private static int ClickInPlaceIdx;
    
    private Point? _rotationGestureStart;
    private float? _rotationGestureLastAngle;

    public SelectionTool() {
    }

    public override void Init() {
        base.Init();

        _selectionGestureHandler = new(Input);
    }

    public override void InitHotkeys(HotkeyHandler handler) {
        base.InitHotkeys(handler);

        handler.AddHotkeyFromSettings("selection.moveLeft", "left", CreateMoveHandler(new(-1, 0), precise: false), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.moveRight", "right", CreateMoveHandler(new(1, 0), precise: false), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.moveUp", "up", CreateMoveHandler(new(0, -1), precise: false), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.moveDown", "down", CreateMoveHandler(new(0, 1), precise: false), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.moveLeftPixel", "ctrl+left", CreateMoveHandler(new(-1, 0), precise: true), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.moveRightPixel", "ctrl+right", CreateMoveHandler(new(1, 0), precise: true), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.moveUpPixel", "ctrl+up", CreateMoveHandler(new(0, -1), precise: true), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.moveDownPixel", "ctrl+down", CreateMoveHandler(new(0, 1), precise: true), HotkeyModes.OnHoldSmoothInterval);

        handler.AddHotkeyFromSettings("selection.upsizeLeft", "a", CreateUpsizeHandler(new(1, 0), new(-1, 0)), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.upsizeRight", "d", CreateUpsizeHandler(new(1, 0), new()), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.upsizeTop", "w", CreateUpsizeHandler(new(0, 1), new(0, -1)), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.upsizeBottom", "s", CreateUpsizeHandler(new(0, 1), new()), HotkeyModes.OnHoldSmoothInterval);

        handler.AddHotkeyFromSettings("selection.downsizeLeft", "shift+d", CreateUpsizeHandler(new(-1, 0), new(1, 0)), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.downsizeRight", "shift+a", CreateUpsizeHandler(new(-1, 0), new()), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.downsizeTop", "shift+s", CreateUpsizeHandler(new(0, -1), new(0, 1)), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.downsizeBottom", "shift+w", CreateUpsizeHandler(new(0, -1), new()), HotkeyModes.OnHoldSmoothInterval);
        
        handler.AddHotkeyFromSettings("selection.selectAll", "ctrl+a", SelectAll);
        handler.AddHotkeyFromSettings("selection.selectAllSimilar", "ctrl+shift+a", SelectAllSimilar);
        
        this.AddSelectionHotkeys(handler);

        handler.AddHotkeyFromSettings("delete", "delete", DeleteSelections);

        handler.AddHotkeyFromSettings("copy", "ctrl+c", CopySelections);
        handler.AddHotkeyFromSettings("paste", "ctrl+v", PasteSelections);
        handler.AddHotkeyFromSettings("cut", "ctrl+x", CutSelections);
        
        handler.AddHotkeyFromSettings(CreatePrefabKeybindName, "alt+c", CreatePrefab);

        History.OnApply += ClearColliderCachesInSelections;
    }

    private void SelectAll() {
        if (EditorState.CurrentRoom is { } room) {
            Deselect();
            _currentSelections = room.GetSelectionsInRect(null, 
                EditorLayers.ToolLayerToEnum(Layer, _customLayer));
        }
    }
    
    private void SelectAllSimilar() {
        if (_currentSelections is not { Count: > 0})
            return;
        
        if (EditorState.CurrentRoom is { } room) {
            var newSelections = room.GetSelectionsForSimilar(_currentSelections[0].Handler);
            Deselect();
            _currentSelections = newSelections;
        }
    }

    private void CreatePrefab() {
        if (_currentSelections is not { } selections)
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
        if (_currentSelections is not { } selections) {
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
            _currentSelections = selections;
        }

        Input.Keyboard.ConsumeKeyClick(Microsoft.Xna.Framework.Input.Keys.LeftControl);
        Input.Keyboard.ConsumeKeyClick(Microsoft.Xna.Framework.Input.Keys.RightControl);
    }

    private void CopySelections() {
        CopypasteHelper.CopySelectionsToClipboard(_currentSelections);
    }

    private Action CreateUpsizeHandler(Point resize, Vector2 move) => () => {
        if (_currentSelections is { } selections) {
            ResizeSelectionsBy(resize.Mult(GridSize), move * GridSize, selections);
        }
    };

    private Action CreateMoveHandler(Vector2 offset, bool precise) => () => {
        if (_currentSelections is { } selections) {
            MoveSelectionsBy(precise ? offset : offset * GridSize, selections, NineSliceLocation.Middle);
        }
    };

    void ISelectionHotkeyTool.AddNode(Vector2? at) {
        if (_currentSelections is not { } selections) {
            return;
        }

        List<IHistoryAction> actions = [];
        List<Selection> newSelections = [];
        List<Selection> unselected = [];

        foreach (var s in selections) {
            if (unselected.All(x => x.Handler != s.Handler) && s.Handler.TryAddNode(at) is { } res) {
                actions.Add(res.Item1);
                newSelections.Add(new() { Handler = res.Item2 });
                unselected.Add(s);
                unselected.AddRange(selections.Where(x => 
                    s.Handler switch{ NodeSelectionHandler n => n.Entity, var xx => xx.Parent } == 
                    x.Handler switch{ NodeSelectionHandler n => n.Entity, var xx => xx.Parent }));
            }
        }

        if (actions.Count > 0) {
            ClearColliderCachesInSelections();
            History.ApplyNewAction(actions.MergeActions());

            _currentSelections = [..selections.Except(unselected).Concat(newSelections)];
        }
    }

    void ISelectionHotkeyTool.Flip(bool vertical) {
        if (_currentSelections is not { } selections) {
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
        if (_currentSelections is not { } selections) {
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
        if (_currentSelections is { } selections) {
            var action = selections.Select(s => s.Handler.DeleteSelf()).MergeActions();
            Deselect();
            ClearColliderCachesInSelections();

            History.ApplyNewAction(action);
        }
    }

    private void ClearColliderCachesInSelections() => _currentSelections?.ForEach(s => s.Handler.ClearCollideCache());

    public void Deselect() {
        if (_currentSelections is not { Count: > 0 })
            return;

        foreach (var selection in _currentSelections) {
            selection.Handler.OnDeselected();
        }

        // clear the list so that the list captured into the history action lambda no longer contains references to the selections, allowing them to get GC'd
        _currentSelections?.Clear();
        _currentSelections = null;

        OnSelectionsChanged();
    }

    public void Deselect(ISelectionHandler handler) {
        if (_currentSelections is null)
            return;

        var selection = _currentSelections.Find(s => s.Handler == handler);
        if (selection is { }) {
            _currentSelections.Remove(selection);
            handler.OnDeselected();

            OnSelectionsChanged();
        }
    }

    public override List<EditorLayer> ValidLayers { get; } = [
        EditorLayers.Entities, EditorLayers.Triggers,
        EditorLayers.FgDecals, EditorLayers.BgDecals,
        EditorLayers.Fg, EditorLayers.Bg,
        EditorLayers.Room,
        EditorLayers.All, EditorLayers.CustomLayer
    ];

    public override string GetMaterialDisplayName(EditorLayer layer, object material) {
        throw new NotImplementedException();
    }

    public override IEnumerable<object> GetMaterials(EditorLayer layer) => [];

    public override string? SerializeMaterial(EditorLayer layer, object? material) {
        return null;
    }

    public override object? DeserializeMaterial(EditorLayer layer, string serializableMaterial) {
        return null;
    }

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

        Gfx.EndBatch();
        Gfx.BeginBatch(EditorState.Camera!);

        var room = EditorState.CurrentRoom;
        if (Layer == EditorLayers.Room)
            room ??= EditorState.Map?.Rooms.FirstOrDefault();
        
        DoRender(EditorState.Camera, room);
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

    private Selection? GetSelectionToBeSelectedOnClick(List<Selection> selections) {
        if (selections.Count <= 0) {
            ClickInPlaceIdx = 0;
            return null;
        }

        var exceptCurrent = selections.Except(_currentSelections ?? []).ToList();
        if (exceptCurrent.Count > 0)
            selections = exceptCurrent;

        var exceptTiles = selections.Where(s => s.Handler is not TileSelectionHandler).ToList();
        if (exceptTiles.Count > 0)
            selections = exceptTiles;
        
        return selections[ClickInPlaceIdx % selections.Count];
    }

    private void DoRender(Camera camera, Room? room) {
        if (_currentSelections is not { Count: > 0 }) {
            // If we're in room selection mode, always select the current room if there are no other selections
            // We do this here instead of Update, as Update is not called when hovering over imgui elements
            if (Layer == EditorLayers.Room && EditorState.CurrentRoom is { } currentRoom) {
                AddSelection(new(currentRoom.GetSelectionHandler()));
            }
        }

        if (_selectionGestureHandler.CurrentRectangle is { } rect) {
            DrawSelectionRect(rect);
        }

        var mousePos = GetMouseRoomPos(camera, room);
        var imguiWantsMouse = ImGuiManager.WantCaptureMouse || ImGui.IsAnyItemHovered();

        var selectionsUnderCursor = 
            room?.GetSelectionsInRect(_selectionGestureHandler.CurrentRectangle ?? new(mousePos.X, mousePos.Y, 1, 1), 
                EditorLayers.ToolLayerToEnum(Layer, _customLayer)) ?? [];

        selectionsUnderCursor = GetSortedSelections(selectionsUnderCursor.Where(s => s.Handler is not TileSelectionHandler));

        Selection? selectionToBeSelectedOnClick = !_selectionGestureHandler.Started && _state == States.Idle
                ? GetSelectionToBeSelectedOnClick(selectionsUnderCursor)
                : null;
        
        if (_currentSelections is { } selections) {
            foreach (var selection in selections) {
                if (!imguiWantsMouse && (_state != States.Idle || selection.Check(mousePos.X, mousePos.Y))) {
                    if (selectionToBeSelectedOnClick is {})
                        selection.Render(Color.Red);
                    else
                        _selectionsToHighlight.Add(selection);

                    if (_state == States.Idle) {
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
        
        if (_state == States.Idle && !imguiWantsMouse) {
            HandleHoveredSelections(room, selectionsUnderCursor, _currentSelections, Input, middleClick: true);

            if (selectionToBeSelectedOnClick is {} s) {
                var isToBeSelectedAlreadySelected = _currentSelections?.Contains(s) ?? false;
                var color = isToBeSelectedAlreadySelected ? Color.Gold : Color.Pink;
                s.Render(color);
                if (s.Handler is EntitySelectionHandler { Entity: Trigger trigger }) {
                    trigger.GetTextSprite(color, Color.Black).Render();
                }
            } else {
                foreach (var s2 in selectionsUnderCursor) {
                    s2.Render(Color.Pink);
                }
            }
        }

        if (_selectionsToHighlight is { Count: > 0 }) {
            foreach (var selection in _selectionsToHighlight) {
                selection.Render(Color.Gold);
            }
            
            _selectionsToHighlight.Clear();
        }

        if (_rotationGestureStart is { } rotStart) {
            var ctx = SpriteRenderCtx.Default();
            ISprite.Circle(rotStart.ToVector2(), 8, Color.Gray, 32, thickness: 0.5f).Render(ctx);
            ISprite.Circle(rotStart.ToVector2(), 1, Color.Gold, 8).Render(ctx);
            (ISprite.Line(rotStart.ToVector2(), mousePos.ToVector2(), Color.Gold) with {
                Thickness = 0.25f,
            }).Render(ctx);
        }
    }

    internal static void HandleHoveredSelections(Room? room, List<Selection>? selectionsUnderCursor,
        IEnumerable<Selection>? selected = null, Input? input = null, bool middleClick = false) {
        input ??= Input.Global;
        
        var canRightClick = input.Mouse.RightClickedInPlace();

        if (!canRightClick && !middleClick)
            return;

        if (selectionsUnderCursor is not { Count: > 0 }) {
            return;
        }

        var firstSelection = selectionsUnderCursor[0];
        if (firstSelection.Handler is TileSelectionHandler) {
            canRightClick = false;
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

        if (_state == States.MoveOrResizeGesture) {
            FinalizeMove(EditorState.Camera, EditorState.CurrentRoom);
        }
    }

    public override void Update(Camera camera, Room? room) {
        if (Layer == EditorLayers.Room)
            room ??= EditorState.Map?.Rooms.FirstOrDefault();
        
        if (_currentSelections is { } selections) {
            var mouseRoomPos = GetMouseRoomPos(camera, room);
            var mouseRect = new Rectangle(mouseRoomPos.X, mouseRoomPos.Y, 1, 1);
            if (Input.Mouse.Left.Clicked()) {
                foreach (var selection in selections) {
                    if (selection.Check(mouseRect)) {
                        _moveGestureStart = mouseRoomPos;
                        _state = States.MoveOrResizeGesture;
                        _moveGestureGrabbedLocation = selection.Handler.Rect.GetLocationInRect(mouseRoomPos, GetSideGrabLeniency(camera)) ?? NineSliceLocation.Middle;
                        _moveGestureGrabbedLocation = AdjustGrabLocBasedOnResizable(_moveGestureGrabbedLocation, selection.Handler);
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

        switch (_state) {
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
        _selectionGestureHandler.CancelGesture();
        Deselect();
    }

    public override bool AllowSwappingRooms => Layer != EditorLayers.Room;

    private IHistoryAction? GetPreciseRotationAction(float realAngle) {
        if (_currentSelections is null)
            return null;

        var actions = new List<IHistoryAction>();
        foreach (var s in _currentSelections) {
            if (s.Handler is ISelectionPreciseRotationHandler rotationHandler) {
                if (rotationHandler.TryPreciseRotate(realAngle, _rotationGestureStart!.Value.ToVector2()) is { } act) {
                    actions.Add(act);
                }
                s.Handler.ClearCollideCache();
            }
        }

        return actions.MergeActions();
    }

    private void EndRotationGesture(Camera camera, Room room, float angle) {
        if (_currentSelections is null) {
            return;
        }
        
        History.UndoSimulations();
        
        if (Input.Mouse.LeftDoubleClicked() && angle == 0f) {
            Point mousePos = GetMouseRoomPos(camera, room);
            SelectWithin(room, new Rectangle(mousePos.X, mousePos.Y, 1, 1));
        } else {
            if (angle != 0f)
                History.ApplyNewAction(GetPreciseRotationAction(angle));
            ClearColliderCachesInSelections();
        }
        
        _rotationGestureStart = null;
    }

    private void UpdateRotationGesture(Camera camera, Room? room) {
        if (room is null)
            return;
        
        if (_currentSelections is null) {
            _state = States.Idle;
            EndRotationGesture(camera, room, 0f);
            return;
        }

        if (_rotationGestureStart is null) {
            CreateMousePosRect(camera, room, out var start);
            _rotationGestureStart = start;
            _rotationGestureLastAngle = null;
        }

        CreateMousePosRect(camera, room, out var currentPos);
        var angle = VectorExt.Angle(_rotationGestureStart.Value.ToVector2(), currentPos.ToVector2());
        var realAngle = angle;
        if (_rotationGestureLastAngle is null) {
            _rotationGestureLastAngle = angle;
            angle = 0f;
        } else {
            (angle, _rotationGestureLastAngle) = (angle - _rotationGestureLastAngle.Value, angle);
        }

        if (!Input.Keyboard.Shift() || _currentSelections is null) {
            EndRotationGesture(camera, room, angle);
            _state = States.MoveOrResizeGesture;
            return;
        }

        switch (Input.Mouse.Left) {
            case MouseInputState.Held: {
                History.UndoSimulations();
                History.ApplyNewSimulation(GetPreciseRotationAction(realAngle));

                break;
            }
            case MouseInputState.Released: {
                EndRotationGesture(camera, room, realAngle);
                _state = States.Idle;

                break;
            }
        }
    }

    private void FinalizeMove(Camera camera, Room? room) {
        if (_currentSelections is { } selections && _moveGestureStart is { } start) {
            Point mousePos = GetMouseRoomPos(camera, room);
            Vector2 delta = _moveGestureFinalDelta;

            if (delta.LengthSquared() <= 0) {
                // If you just clicked in place, then act as if you wanted to simply change your selection, instead of moving
                SelectWithin(room, new Rectangle(mousePos.X, mousePos.Y, 1, 1));
            } else {
                MoveSelectionsBy(delta, selections, _moveGestureGrabbedLocation);
            }

            _moveGestureStart = mousePos;
            _moveGestureFinalDelta = default;
        }
    }
    
    private void EndMoveGesture(bool simulate, Camera? camera, Room? room) {
        if (_state != States.MoveOrResizeGesture)
            return;

        History.UndoSimulations();

        if (simulate && room is {} && camera is {}) {
            FinalizeMove(camera, room);
        }
        
        _moveGestureStart = null;
        _moveGestureLastMousePos = null;
        _moveGestureFinalDelta = Vector2.Zero;
        _state = States.Idle;
        _moveGestureGrabbedLocation = NineSliceLocation.Middle;
    }

    private void UpdateMoveGesture(Camera camera, Room? room) {
        var left = Input.Mouse.Left;

        if (Input.Keyboard.Shift()) {
            EndMoveGesture(false, camera, room);
            _state = States.RotationGesture;
            return;
        }

        switch (left) {
            case MouseInputState.Released: {
                EndMoveGesture(true, camera, room);
                break;
            }
            case MouseInputState.Held: {
                if (_currentSelections is { } selections && _moveGestureStart is { } start) {
                    var mousePos = GetMouseRoomPos(camera, room);
                    _moveGestureLastMousePos ??= mousePos;

                    Vector2 delta = CalculateMovementDelta(_moveGestureLastMousePos!.Value, mousePos);

                    if (delta.LengthSquared() != 0) {
                        _moveGestureLastMousePos += delta.ToPoint();
                        _moveGestureFinalDelta += delta;
                        //Console.WriteLine(MoveGestureFinalDelta);
                        SimulateMoveSelectionsBy(_moveGestureFinalDelta, selections, _moveGestureGrabbedLocation);
                    }

                    var cursorType = _moveGestureGrabbedLocation.ToMouseCursor();
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
        if (!Input.Keyboard.Ctrl() || !Layer.SupportsPreciseMoveMode) {
            var gridSize = GridSize;
            pos = pos.GridPosRound(gridSize).ToVector2() * gridSize;
        }

        return pos;
    }

    private void UpdateDragGesture(Camera camera, Room? room) {
        if (_selectionGestureHandler.Update((p) => GetMouseRoomPos(camera, room, p)) is { } rect) {
            SelectWithin(room, rect);
        }
    }
    
    private List<Selection> GetSortedSelections(IEnumerable<Selection> selections)
        => selections
            //.OrderBy(s => s.Handler.Parent is IDepth d ? d.Depth : int.MinValue)
            .OrderBy(s => s.Handler.Rect.Area())
            .ToList();

    private void SelectWithin(Room? room, Rectangle rect) {
        var selections = room?.GetSelectionsInRect(rect, EditorLayers.ToolLayerToEnum(Layer, _customLayer)) ?? [];
        selections = GetSortedSelections(selections);
        
        List<Selection>? finalSelections = null;

        if (rect.Width <= 1 && rect.Height <= 1 && selections.Count > 0) {
            // you just clicked in place, select only 1 selection
            if (selections.Count > 1) {
                int idx = ClickInPlaceIdx % selections.Count;
                var toSelect = GetSelectionToBeSelectedOnClick(selections)!.Value;
                ClickInPlaceIdx = (idx + 1) % selections.Count;

                finalSelections = [ toSelect ];
            } else if (_currentSelections?.Count > 0 && Input.Mouse.LeftDoubleClicked()) {
                // if you double clicked in place, select all similar entities/decals
                var handler = selections[0].Handler;
                finalSelections = Input.Keyboard.Shift() 
                    ? room!.GetSelectionsForSimilar(handler)!
                    : room!.GetSelectionsForSameType(handler)!;
                ClickInPlaceIdx = 0;
            }

        }

        if (finalSelections == null) {
            finalSelections = selections;
            ClickInPlaceIdx = 0;
        }

        // Deselect all current selections, we will call OnSelected on all remaining ones at the end anyway
        if (_currentSelections is { })
            foreach (var selection in _currentSelections) {
                selection.Handler.OnDeselected();
            }

        if (Input.Keyboard.Shift() && _currentSelections is { }) {
            // Add new selections

            foreach (var h in _currentSelections.SelectWhereNotNull(s => s.Handler as TileSelectionHandler))
                h.MergeWith(rect, exclude: false);

            _currentSelections = _currentSelections
                .Concat(finalSelections)
                .DistinctBy(x => x.Handler.Parent)
                .ToList();
        } else if (Input.Keyboard.Ctrl() && _currentSelections is { }) {
            // Remove existing selections
            var newSelections = _currentSelections.Except(finalSelections, new HandlerParentEqualityComparer());

            // tile selections are unique - they need to remain in the selection list, and we need to call MergeWith
            foreach (var tileSelection in _currentSelections.Where(s => s.Handler is TileSelectionHandler)) {
                if (tileSelection is { Handler: TileSelectionHandler tileHandler }) {
                    tileHandler.MergeWith(rect, exclude: true);

                    newSelections = newSelections.Append(tileSelection);
                }
            }

            _currentSelections = newSelections.DistinctBy(x => x.Handler.Parent).ToList();
        } else {
            // Set selections
            Deselect();
            _currentSelections = finalSelections;
        }

        // Tell the handlers that they're selected
        foreach (var selection in _currentSelections) {
            selection.Handler.OnSelected();
        }

        OnSelectionsChanged();
    }

    private void OnSelectionsChanged() {
        if (_currentSelections is [{ Handler: RoomSelectionHandler roomSelection }]) {
            // you only selected 1 room, let's swap to that room as well
            EditorState.CurrentRoom = roomSelection.Room;
        }
    }

    public void AddSelection(Selection selection) {
        _currentSelections = _currentSelections?
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
            var c = (int) _customLayer;
            ImGui.CheckboxFlags(EditorLayers.Entities.LocalizedName, ref c, (int) SelectionLayer.Entities);
            ImGui.CheckboxFlags(EditorLayers.Triggers.LocalizedName, ref c, (int) SelectionLayer.Triggers);
            ImGui.CheckboxFlags(EditorLayers.FgDecals.LocalizedName, ref c, (int) SelectionLayer.FgDecals);
            ImGui.CheckboxFlags(EditorLayers.BgDecals.LocalizedName, ref c, (int) SelectionLayer.BgDecals);
            ImGui.CheckboxFlags(EditorLayers.Bg.LocalizedName, ref c, (int) SelectionLayer.BgTiles);
            ImGui.CheckboxFlags(EditorLayers.Fg.LocalizedName, ref c, (int) SelectionLayer.FgTiles);

            _customLayer = (SelectionLayer) c;

            ImGui.Separator();
        }

        ImGui.Text("Selections");
        if (_currentSelections is [_, ..] && ImGuiManager.TranslatedButton("rysy.createPrefab")) {
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

        if (_currentSelections is { })
            foreach (var selection in _currentSelections) {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                switch (selection.Handler) {
                    case EntitySelectionHandler entity:
                        ImGui.Text(entity.Entity.Name);
                        break;
                    case NodeSelectionHandler node:
                        ImGui.Text(Interpolator.TempU8($"{node.Entity.Name}[{node.NodeIdx}]"));
                        break;
                    case RoomSelectionHandler room:
                        ImGui.Text(room.Room.Name);
                        break;
                    case TileSelectionHandler tiles:
                        ImGui.Text(tiles.Layer.FastToString());
                        break;
                }
                HighlightIfHovered(_selectionsToHighlight, selection);
                ImGui.TableNextColumn();

                ImGuiManager.PushNullStyle();
                if (RysyEngine.Scene is EditorScene && ImGui.Selectable(Interpolator.TempU8($"Deselect##{selection.GetHashCode()}"))) {
                    DeselectOnEndOfFrame(selection);
                }
                HighlightIfHovered(_selectionsToHighlight, selection);
                ImGui.TableNextColumn();

                if (RysyEngine.Scene is EditorScene && ImGui.Selectable(Interpolator.TempU8($"Edit##{selection.GetHashCode()}"))) {
                    RightClickOnEndOfFrame(selection);
                }
                HighlightIfHovered(_selectionsToHighlight, selection);

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
