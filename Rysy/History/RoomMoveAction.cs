namespace Rysy.History;
public record class RoomMoveAction(Room room, int TilesX, int TilesY) : IHistoryAction {
    public bool Apply() {
        room.X += TilesX * 8;
        room.Y += TilesY * 8;

        return true;
    }

    public void Undo() {
        room.X -= TilesX * 8;
        room.Y -= TilesY * 8;
    }
}
