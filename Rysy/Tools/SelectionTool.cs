using ImGuiNET;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Scenes;

namespace Rysy.Tools;

internal class SelectionTool : Tool {
    private const string CUSTOM_LAYER = "Custom";

    private SelectRectangleGesture SelectionGestureHandler;

    private List<Selection>? CurrentSelections;

    private SelectionLayer CustomLayer;

    public SelectionTool() {
        SelectionGestureHandler = new();
    }

    public override void InitHotkeys(HotkeyHandler handler) {
        base.InitHotkeys(handler);

        handler.AddHotkeyFromSettings("selection.moveLeft", "left", CreateMoveHandler(new(-8, 0)), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.moveRight", "right", CreateMoveHandler(new(8, 0)), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.moveUp", "up", CreateMoveHandler(new(0, -8)), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.moveDown", "down", CreateMoveHandler(new(0, 8)), HotkeyModes.OnHoldSmoothInterval);
        handler.AddHotkeyFromSettings("selection.delete", "delete", DeleteSelection);
    }

    private Action CreateMoveHandler(Vector2 offset) => () => {
        if (CurrentSelections is { } selections) {
            var action = selections.Select(s => s.Handler.MoveBy(offset)).MergeActions().WithHook(
                onApply: () => selections.ForEach(s => s.Collider.MoveBy(offset)),
                onUndo: () => selections.ForEach(s => s.Collider.MoveBy(-offset))
            );

            History.ApplyNewAction(action);
        }
    };

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
        LayerNames.ALL, CUSTOM_LAYER
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
        if (SelectionGestureHandler.Update((p) => room.WorldToRoomPos(camera, p)) is { } rect) {
            var selections = room.GetSelectionsInRect(rect, ToolLayerToEnum(Layer));

            if (Input.Keyboard.Shift() && CurrentSelections is { }) {
                CurrentSelections = CurrentSelections.Concat(selections.Where(s => s.Handler is not Tilegrid.RectSelectionHandler).DistinctBy(x => x.Handler)).ToList();
            } else {
                Deselect();
                CurrentSelections = selections;
            }
        }
    }

    public override void CancelInteraction() {
        base.CancelInteraction();

        Deselect();
        SelectionGestureHandler.CancelGesture();
    }

    public override void RenderGui(EditorScene editor, bool firstGui) {
        BeginMaterialListGUI(firstGui);

        if (Layer == CUSTOM_LAYER) {
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

    private SelectionLayer ToolLayerToEnum(string layer) => layer switch {
        LayerNames.FG => SelectionLayer.FGTiles,
        LayerNames.BG => SelectionLayer.BGTiles,
        LayerNames.FG_DECALS => SelectionLayer.FGDecals,
        LayerNames.BG_DECALS => SelectionLayer.BGDecals,
        LayerNames.ENTITIES => SelectionLayer.Entities,
        LayerNames.TRIGGERS => SelectionLayer.Triggers,
        LayerNames.ALL => SelectionLayer.All,
        CUSTOM_LAYER => CustomLayer,
        _ => SelectionLayer.None,
    };
}
