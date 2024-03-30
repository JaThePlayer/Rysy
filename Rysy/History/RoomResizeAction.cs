namespace Rysy.History;

public record RoomResizeAction(RoomRef Room, int Width, int Height) : IHistoryAction {
    private char[,] _origBG, _origFG;
    private int _origWidth, _origHeight;

    public bool Apply(Map map) {
        var room = Room.Resolve(map);
        
        if (room.Width == Width && room.Height == Height) {
            return false;
        }
        int w = Width;
        if (w <= 0) {
            w = 8;
        }
        var h = Height;
        if (h <= 0) {
            h = 8;
        }

        _origBG = room.BG.Tiles;
        _origFG = room.FG.Tiles;
        _origHeight = room.Height;
        _origWidth = room.Width;

        room.BG.Resize(w, h);
        room.FG.Resize(w, h);

        room.Attributes.Width = w;
        room.Attributes.Height = h;

        return true;
    }

    public void Undo(Map map) {
        var room = Room.Resolve(map);
        
        room.BG.Tiles = _origBG;
        room.FG.Tiles = _origFG;
        room.Attributes.Width = _origWidth;
        room.Attributes.Height = _origHeight;
    }
}
