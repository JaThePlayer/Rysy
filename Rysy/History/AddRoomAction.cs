namespace Rysy.History;

public record class AddRoomAction(Room Room) : IHistoryAction {
    public Action<Room>? OnFirstApply { get; set; }
    
    public bool Apply(Map map) {
        map.Rooms.Add(Room);
        if (OnFirstApply is { }) {
            OnFirstApply(Room);
            OnFirstApply = null;
        }

        return true;
    }

    public void Undo(Map map) {
        map.Rooms.Remove(Room);
        if (EditorState.CurrentRoom == Room) {
            EditorState.CurrentRoom = null;
        }
    }
}
