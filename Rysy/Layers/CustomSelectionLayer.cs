using Hexa.NET.ImGui;
using Rysy.Selections;

namespace Rysy.Layers;

public sealed class CustomSelectionLayer : EditorLayer, ISelectionEditorLayer {
    public override string Name => "Custom";

    private SelectionLayer _currentLayer = SelectionLayer.None;
    
    public override SelectionLayer SelectionLayer => _currentLayer;

    public override IReadOnlyList<object> GetContents(Room room) => [];

    public override IEnumerable<Placement> GetMaterials() {
        return [];
    }

    public List<Selection> GetSelectionsInRect(Map map, Room? room, Rectangle? rect) {
        if (room is null)
            return [];

        return room.GetSelectionsInRect(rect, SelectionLayer);
    }

    public void RenderCustomMaterialListStart() {
        var c = (int) SelectionLayer;
        ImGui.CheckboxFlags(EditorLayers.Entities.LocalizedName, ref c, (int) SelectionLayer.Entities);
        ImGui.CheckboxFlags(EditorLayers.Triggers.LocalizedName, ref c, (int) SelectionLayer.Triggers);
        ImGui.CheckboxFlags(EditorLayers.FgDecals.LocalizedName, ref c, (int) SelectionLayer.FgDecals);
        ImGui.CheckboxFlags(EditorLayers.BgDecals.LocalizedName, ref c, (int) SelectionLayer.BgDecals);
        ImGui.CheckboxFlags(EditorLayers.Bg.LocalizedName, ref c, (int) SelectionLayer.BgTiles);
        ImGui.CheckboxFlags(EditorLayers.Fg.LocalizedName, ref c, (int) SelectionLayer.FgTiles);

        _currentLayer = (SelectionLayer) c;

        ImGui.Separator();
    }
}