using Rysy.Helpers;

namespace Rysy.History;

internal class ChangeTileLayerAction(TileLayer layer, Dictionary<string, object> edited) : IHistoryAction {
    private Dictionary<string, object> _prevValues;
    
    public bool Apply(Map map) {
        var updated = false;

        _prevValues = layer.Pack().Attributes;
        layer.SetOverlay(null, map);
        updated |= layer.Update(edited, map);

        return updated;
    }

    public void Undo(Map map) {
        layer.SetOverlay(null, map);
        layer.Update(_prevValues, map);
    }
}