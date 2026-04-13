using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.Layers;
using Rysy.Signals;

namespace Rysy.Components;

public interface IRoomSpriteProvider {
    public IReadOnlyList<ISprite> GetSprites(Room room);
}

internal sealed class EntityListSpriteProvider(EntityLayer layer, Func<Persistence, bool> isVisible)
    : IRoomSpriteProvider, ISignalListener<RoomLayerChanged> {
    class Cache : IAttachable {
        public List<ISprite>? Sprites { get; set; }
    }

    private readonly AttachedStorage<Room, Cache> _storage = new();
    
    public IReadOnlyList<ISprite> GetSprites(Room room) {
        var cache = _storage.GetOrCreateAttached(room, _ => new Cache());
        
        var p = Persistence.Instance;
        if (!isVisible(p)) {
            cache.Sprites?.Clear();
            return [];
        }

        var list = layer.GetContents(room);
        
        cache.Sprites ??= list.Select(e => {
            var spr = e.GetSpritesWithNodes();
            if (!e.EditorGroups.Enabled)
                spr = spr.Select(s => s.WithMultipliedAlpha(Settings.Instance.HiddenLayerAlpha));
            return spr;
        }).SelectMany(x => x).ToList();

        return cache.Sprites;
    }

    public void OnSignal(RoomLayerChanged signal) {
        if (signal.Layer == layer && _storage.TryGetAttached(signal.Room, out var cache)) {
            cache.Sprites = null;
        }
    }
}

internal sealed class TileGridSpriteProvider(TileEditorLayer layer, Func<Persistence, bool> isVisible) 
    : IRoomSpriteProvider {

    public IReadOnlyList<ISprite> GetSprites(Room room) {
        var p = Persistence.Instance;
        if (!isVisible(p)) {
            return [];
        }
        
        var grid = layer.GetGrid(room);
        grid.RenderCacheToken?.Reset();

        return [ grid.GetSprites() ];
    }
}
