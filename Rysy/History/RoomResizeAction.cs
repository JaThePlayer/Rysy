using Rysy.Helpers;

namespace Rysy.History;

public record RoomResizeAction(RoomRef Room, int Width, int Height) : IHistoryAction {
    private int _origWidth, _origHeight;
    private Dictionary<TileLayer, char[,]> _origTiles;

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

        _origHeight = room.Height;
        _origWidth = room.Width;
        _origTiles = [];

        foreach (var (layer, gridInfo) in room.Tilegrids) {
            var orig = gridInfo.Tilegrid.Tiles;
            _origTiles[layer] = orig;
            gridInfo.Tilegrid.Resize(w, h);
        }

        room.Attributes.Width = w;
        room.Attributes.Height = h;

        return true;
    }

    public void Undo(Map map) {
        var room = Room.Resolve(map);
        
        foreach (var (layer, gridInfo) in room.Tilegrids) {
            gridInfo.Tilegrid.Tiles = _origTiles[layer];
        }
        
        room.Attributes.Width = _origWidth;
        room.Attributes.Height = _origHeight;
    }
}
