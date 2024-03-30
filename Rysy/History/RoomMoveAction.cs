namespace Rysy.History;
public record RoomMoveAction(RoomRef Room, int TilesX, int TilesY) : IHistoryAction {
    public bool Apply(Map map) {
        var room = Room.Resolve(map);
        room.X += TilesX * 8;
        room.Y += TilesY * 8;

        return true;
    }

    public void Undo(Map map) {
        var room = Room.Resolve(map);
        room.X -= TilesX * 8;
        room.Y -= TilesY * 8;
    }
}
