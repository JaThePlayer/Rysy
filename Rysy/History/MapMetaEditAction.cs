namespace Rysy.History;

public record class MapMetaEditAction(Map Map, MapMetadata New) : IHistoryAction {
    private MapMetadata Orig;

    public bool Apply() {
        Orig = Map.Meta;

        if (Orig == New)
            return false;

        Map.Meta = New;

        return true;
    }

    public void Undo() {
        Map.Meta = Orig;
    }
}
