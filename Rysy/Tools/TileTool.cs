using ImGuiNET;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.Scenes;
using System.Runtime.CompilerServices;
using Rysy.Gui;
using Rysy.Layers;

namespace Rysy.Tools;

public abstract class TileTool : Tool {
    public Color DefaultColor = ColorHelper.HSVToColor(0f, 1f, 1f);

    private static List<EditorLayer> _ValidLayers { get; } = new() {
        EditorLayers.Fg, EditorLayers.Bg, EditorLayers.BothTilegrids
    };

    public override List<EditorLayer> ValidLayers => _ValidLayers;

    public override string PersistenceGroup => "TileTool";

    public override void Init() {
        base.Init();

        EditorState.OnMapChanged += ClearMaterialListCache;
    }

    public override IEnumerable<object>? GetMaterials(EditorLayer layer) {
        var autotiler = GetAutotiler(layer);
        if (autotiler is not { })
            return null;

        autotiler.TilesetDataCacheToken.OnNextInvalidate += ClearMaterialListCache;
        return autotiler.Tilesets.Keys.Where(k => k is not ('z' or 'y')).Append('0').Select(k => (object) k);
    }

    private static Dictionary<EditorLayer, ConditionalWeakTable<string, string>> MaterialToDisplayCache = new();

    public override string GetMaterialDisplayName(EditorLayer layer, object material) {
        if (!MaterialToDisplayCache.TryGetValue(layer, out var cache)) {
            MaterialToDisplayCache[layer] = cache = new();
        }

        if (material is char c) {
            if (c is '0') {
                return "Air";
            }
            var cAsString = c.ToString();
            if (!cache.TryGetValue(cAsString, out var name)) {
                name = GetAutotiler(layer)?.GetTilesetDisplayName(c) ?? cAsString;

                cache.Add(cAsString, name);
            }

            return name;
        }

        return material.ToString()!;
    }

    public override string? GetMaterialTooltip(EditorLayer layer, object material) {
        if (material is not char c)
            return null;

        return $"""
            Id: {c}
            Source: {GetAutotiler(layer)?.GetTilesetData(c)?.Filename}
            """;
    }

    public char Tile {
        get => Material is char c ? c : '0';
        set => Material = value;
    }

    protected TileLayer EditorLayerToTileLayer(EditorLayer? layer) {
        layer ??= Layer;
        
        if (layer is TileEditorLayer { TileLayer: { } tileLayer }) {
            return tileLayer;
        }

        return TileLayer.FG;
    }

    public Autotiler? GetAutotiler(EditorLayer layer) {
        if (RysyEngine.Scene is EditorScene { Map: { } map }) {
            if (layer is TileEditorLayer { TileLayer: { } tileLayer }) {
                return tileLayer switch {
                    TileLayer.FG => map.FGAutotiler,
                    TileLayer.BG => map.BGAutotiler,
                    _ => null,
                };
            }
        }
        return null;
    }

    public void RenderTiles(Vector2 loc, int w, int h) {
        foreach (var item in GetAutotiler(Layer)?.GetSprites(loc, Tile, w, h, Color.White) ?? Array.Empty<ISprite>()) {
            item.WithMultipliedAlpha(0.3f).Render();
        }
    }

    public override void Update(Camera camera, Room room) {
        var (tx, ty) = GetMouseTilePos(camera, room);
        HandleMiddleClick(room, tx, ty);
    }

    public override void RenderOverlay() {
    }

    protected Tilegrid GetGrid(Room room, EditorLayer? layer = null) {
        layer ??= Layer;

        if (layer is TileEditorLayer tileEditorLayer)
            return tileEditorLayer.GetGrid(room);

        if (layer == EditorLayers.BothTilegrids)
            return room.FG;

        throw new ArgumentException("Provided layer is not a tile layer", nameof(layer));
    }

    protected Tilegrid? GetSecondGrid(Room room) 
        => Layer == EditorLayers.BothTilegrids ? room.BG : null;

    protected Point GetMouseTilePos(Camera camera, Room room, bool round = false) {
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
                ('0', '0') => (EditorLayers.BothTilegrids, bg), // if both tiles are air, switch to the "Both" layer.
                ('0', not '0') => (EditorLayers.Bg, bg), // fg is air, but bg isn't. Switch to BG.
                (not '0', _) => (EditorLayers.Fg, fg), // fg tile exists, swap to that.
            };
        }
    }

    const int PreviewSize = 32;

    public override float MaterialListElementHeight() 
        => Settings.Instance.ShowPlacementIcons ? PreviewSize + ImGui.GetStyle().FramePadding.Y : base.MaterialListElementHeight();

    protected override XnaWidgetDef? GetMaterialPreview(object material) {
        var autotiler = GetAutotiler(Layer);
        if (autotiler is { } && material is char c) {
            var tileset = autotiler.GetTilesetData(c);
            return new($"tile_{c}_{autotiler.GetTilesetDisplayName(c)}", PreviewSize, PreviewSize, () => {
                if (tileset is null)
                    return;
                
                foreach (var item in tileset.GetPreview(PreviewSize)) {
                    item.Render();
                }
            });
        }

        return base.GetMaterialPreview(material);
    }

    protected override XnaWidgetDef CreateTooltipPreview(XnaWidgetDef materialPreview, object material) {
        return materialPreview;
    }
}
