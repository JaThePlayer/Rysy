namespace Rysy.History;

public record class RoomDeleteAction(Map map, Room room) : IHistoryAction {
    public bool Apply() {
        room.ClearRenderCache();

        return map.Rooms.Remove(room);
    }

    public void Undo() {
        map.Rooms.Add(room);
    }
}
