namespace Rysy.History;

public record class AddRoomAction(Room Room) : IHistoryAction {
    public bool Apply(Map map) {
        map.Rooms.Add(Room);

        return true;
    }

    public void Undo(Map map) {
        map.Rooms.Remove(Room);
        if (EditorState.CurrentRoom == Room) {
            EditorState.CurrentRoom = null;
        }
    }
}
