namespace Rysy.History;

public record MapMetaEditAction(MapMetadata New) : IHistoryAction {
    private MapMetadata Orig;

    public bool Apply(Map map) {
        Orig = map.Meta;

        if (Orig == New)
            return false;

        map.Meta = New;

        return true;
    }

    public void Undo(Map map) {
        map.Meta = Orig;
    }
}
