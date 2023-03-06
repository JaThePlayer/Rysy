namespace Rysy.History;

public sealed record class RemoveEntityAction(Entity Entity, Room Room) : IHistoryAction {
    public bool Apply() {
        return GetList().Remove(Entity);
    }

    private TypeTrackedList<Entity> GetList() {
        return Entity is Trigger ? Room.Triggers : Room.Entities;
    }

    public void Undo() {
        GetList().Add(Entity);
    }
}

public sealed record class RemoveDecalAction(Decal Decal, Room Room) : IHistoryAction {
    public bool Apply() {
        var ret = GetList().Remove(Decal);
        Decal.ClearRoomRenderCache();

        return ret;
    }

    private List<Decal> GetList() {
        return Decal.FG ? Room.FgDecals : Room.BgDecals;
    }

    public void Undo() {
        GetList().Add(Decal);

        Decal.ClearRoomRenderCache();
    }
}