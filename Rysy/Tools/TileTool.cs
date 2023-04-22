using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.Scenes;
using System.Runtime.CompilerServices;

namespace Rysy.Tools;

public abstract class TileTool : Tool {
    public Color DefaultColor = ColorHelper.HSVToColor(0f, 1f, 1f);

    private static List<string> _ValidLayers = new() { LayerNames.FG, LayerNames.BG, LayerNames.BOTH_TILEGRIDS };

    public override List<string> ValidLayers => _ValidLayers;

    public override string PersistenceGroup => "TileTool";

    public override void Init() {
        base.Init();

        EditorState.OnMapChanged += ClearMaterialListCache;
    }

    public override IEnumerable<object>? GetMaterials(string layer) {
        var autotiler = GetAutotiler(layer);
        if (autotiler is not { })
            return null;

        autotiler.TilesetDataCacheToken.OnNextInvalidate += ClearMaterialListCache;
        return autotiler.Tilesets.Keys.Where(k => k is not 'z' or 'y').Select(k => (object) k);
    }

    private static Dictionary<string, ConditionalWeakTable<string, string>> MaterialToDisplayCache = new();

    public override string GetMaterialDisplayName(string layer, object material) {
        if (!MaterialToDisplayCache.TryGetValue(layer, out var cache)) {
            MaterialToDisplayCache[layer] = cache = new();
        }

        if (material is char c) {
            var cAsString = c.ToString();
            if (!cache.TryGetValue(cAsString, out var name)) {
                if (GetAutotiler(layer)?.Tilesets.TryGetValue(c, out var tileset) ?? false) {
                    name = tileset.Filename.Split('/').Last().TrimStart("bg").Humanize();
                } else {
                    name = cAsString;
                }

                cache.Add(cAsString, name);
            }

            return name;
        }

        return material.ToString()!;
    }

    public override string? GetMaterialTooltip(string layer, object material) {
        return material?.ToString();
    }

    public char Tile {
        get => Material is char c ? c : '0';
        set => Material = value;
    }

    public Autotiler? GetAutotiler(string layer) {
        if (RysyEngine.Scene is EditorScene { Map: { } map }) {
            return layer switch {
                LayerNames.FG => map.FGAutotiler,
                LayerNames.BG => map.BGAutotiler,
                _ => null,
            };
        }
        return null;
    }

    public void RenderTiles(Vector2 loc, int w, int h) {
        foreach (var item in GetAutotiler(Layer)?.GetSprites(loc, Tile, w, h) ?? Array.Empty<ISprite>()) {
            item.WithMultipliedAlpha(0.3f).Render();
        }
    }

    public override void Update(Camera camera, Room room) {
        var (tx, ty) = GetMouseTilePos(camera, room);
        HandleMiddleClick(room, tx, ty);
    }

    public override void RenderOverlay() {
        PicoFont.Print(Tile, new(4, 4), Color.White, 4);
        PicoFont.Print(Layer, new Vector2(4, 36), Color.White, 4);
    }

    protected Tilegrid GetGrid(Room room, string? layer = null) => (layer ?? Layer) switch {
        LayerNames.FG or LayerNames.BOTH_TILEGRIDS => room.FG,
        LayerNames.BG => room.BG,
        _ => throw new NotImplementedException(Layer)
    };

    protected Tilegrid? GetSecondGrid(Room room) => Layer switch {
        LayerNames.BOTH_TILEGRIDS => room.BG,
        _ => null,
    };

    protected static Point GetMouseTilePos(Camera camera, Room room, bool round = false) {
        var pos = room.WorldToRoomPos(camera, Input.Mouse.Pos.ToVector2());
        if (round) {
            return pos.GridPosRound(8);
        }

        return pos.GridPosFloor(8);
    }

    protected void HandleMiddleClick(Room currentRoom, int tx, int ty) {
        if (Input.Mouse.Middle.Clicked()) {
            Input.Mouse.ConsumeMiddle();
            var fg = currentRoom.FG.SafeTileAt(tx, ty);
            var bg = currentRoom.BG.SafeTileAt(tx, ty);

            (Layer, Tile) = (fg, bg) switch {
                ('0', '0') => (LayerNames.BOTH_TILEGRIDS, bg), // if both tiles are air, switch to the "Both" layer.
                ('0', not '0') => (LayerNames.BG, bg), // fg is air, but bg isn't. Switch to BG.
                (not '0', _) => (LayerNames.FG, fg), // fg tile exists, swap to that.
            };
        }
    }
}
