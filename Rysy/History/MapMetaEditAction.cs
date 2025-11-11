namespace Rysy.History;

public record MapMetaEditAction(MapMetadata New) : IHistoryAction {
    private MapMetadata _orig;

    public bool Apply(Map map) {
        _orig = map.Meta;

        if (_orig == New)
            return false;

        map.Meta = New;

        return true;
    }

    public void Undo(Map map) {
        map.Meta = _orig;
    }
}
