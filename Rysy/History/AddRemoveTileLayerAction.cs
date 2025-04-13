using Rysy.Helpers;

namespace Rysy.History;

file static class AddRemoveTileLayerHelpers {
    public static void AddLayer(Map map, TileLayer layer, Dictionary<Room, Room.TilegridInfo>? toRestore) {
        if (toRestore is null or { Count: 0 })
            map.NewTileLayers.Add(layer);
        else {
            foreach (var (room, info) in toRestore) {
                room.Tilegrids[layer] = info;
            }
        }
    }
    
    public static Dictionary<Room, Room.TilegridInfo> RemoveLayer(Map map, TileLayer layer) {
        map.NewTileLayers.Remove(layer);

        var removedGrids = new Dictionary<Room, Room.TilegridInfo>();
        foreach (var room in map.Rooms) {
            if (room.Tilegrids.TryGetValue(layer, out var toRemove)) {
                removedGrids.Add(room, toRemove);
                room.Tilegrids.Remove(layer);
            }
        }

        return removedGrids;
    }
}

internal sealed class AddTileLayerAction(TileLayer newLayer) : IHistoryAction {
    public bool Apply(Map map) {
        AddRemoveTileLayerHelpers.AddLayer(map, newLayer, toRestore: null);

        return true;
    }

    public void Undo(Map map) {
        AddRemoveTileLayerHelpers.RemoveLayer(map, newLayer);
    }
}

internal sealed class RemoveTileLayerAction(TileLayer toRemove) : IHistoryAction {
    private Dictionary<Room, Room.TilegridInfo> _removedGrids;
    
    public bool Apply(Map map) {
        _removedGrids = AddRemoveTileLayerHelpers.RemoveLayer(map, toRemove);
        return true;
    }

    public void Undo(Map map) {
        AddRemoveTileLayerHelpers.AddLayer(map, toRemove, toRestore: _removedGrids);
    }
}