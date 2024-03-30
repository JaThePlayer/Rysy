namespace Rysy.History;

public record RoomDeleteAction(RoomRef Room) : IHistoryAction {
    private Room? _removed;
    
    public bool Apply(Map map) {
        var room = Room.Resolve(map);
        _removed = room;
        room.ClearRenderCache();
        if (EditorState.CurrentRoom == room) {
            EditorState.CurrentRoom = null;
        }

        return map.Rooms.Remove(room);
    }

    public void Undo(Map map) {
        map.Rooms.Add(_removed!);
    }
}
