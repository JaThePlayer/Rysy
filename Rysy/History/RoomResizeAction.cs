namespace Rysy.History;

public record class RoomResizeAction(Room Room, int Width, int Height) : IHistoryAction {
    private char[,] _origBG, _origFG;
    private int _origWidth, _origHeight;

    public bool Apply() {
        if (Room.Width == Width && Room.Height == Height) {
            return false;
        }

        _origBG = Room.BG.Tiles;
        _origFG = Room.FG.Tiles;
        _origHeight = Room.Height;
        _origWidth = Room.Width;

        Room.BG.Resize(Width, Height);
        Room.FG.Resize(Width, Height);

        Room.Attributes.Width = Width;
        Room.Attributes.Height = Height;

        return true;
    }

    public void Undo() {
        Room.BG.Tiles = _origBG;
        Room.FG.Tiles = _origFG;
        Room.Attributes.Width = _origWidth;
        Room.Attributes.Height = _origHeight;
    }
}
