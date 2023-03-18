namespace Rysy.History;

public record class AddRoomAction(Map Map, Room Room) : IHistoryAction {
    public bool Apply() {
        Map.Rooms.Add(Room);

        return true;
    }

    public void Undo() {
        Map.Rooms.Remove(Room);
    }
}
