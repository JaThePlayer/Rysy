namespace Rysy.History;

public record RoomResizeAction(RoomRef Room, int Width, int Height) : IHistoryAction {
    private char[,] _origBg, _origFg;
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

        _origBg = room.Bg.Tiles;
        _origFg = room.Fg.Tiles;
        _origHeight = room.Height;
        _origWidth = room.Width;

        room.Bg.Resize(w, h);
        room.Fg.Resize(w, h);

        room.Attributes.Width = w;
        room.Attributes.Height = h;

        return true;
    }

    public void Undo(Map map) {
        var room = Room.Resolve(map);
        
        room.Bg.Tiles = _origBg;
        room.Fg.Tiles = _origFg;
        room.Attributes.Width = _origWidth;
        room.Attributes.Height = _origHeight;
    }
}
