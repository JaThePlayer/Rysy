namespace Rysy.History;
public record class AddDecalAction(Decal Decal, Room Room) : IHistoryAction {
    private List<Decal> GetList() => Decal.FG ? Room.FgDecals : Room.BgDecals;

    public bool Apply() {
        GetList().Add(Decal);
        Decal.ClearRoomRenderCache();
        return true;
    }

    public void Undo() {
        GetList().Remove(Decal);
        Decal.ClearRoomRenderCache();
    }
}
