using Hexa.NET.ImGui;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.Scenes;
using System.Runtime.CompilerServices;
using Rysy.Gui;
using Rysy.Layers;

namespace Rysy.Tools;

public class TileTool : Tool {
    public Color DefaultColor = ColorHelper.HSVToColor(0f, 1f, 1f);

    private readonly List<ToolMode> _tileModes;

    public TileTool() {
        _tileModes = [
            new TileBrushMode(this),
            new TileRectangleMode(this, hollow: false),
            new TileRectangleMode(this, hollow: true),
            new TileBucketMode(this),
            new TileLineMode(this),
            new TileCircleMode(this, hollow: false),
            new TileCircleMode(this, hollow: true),
            new TileEllipseMode(this, hollow: false),
            new TileEllipseMode(this, hollow: true)
        ];
    }
    
    private static List<EditorLayer> _ValidLayers { get; } =[
        EditorLayers.Fg, EditorLayers.Bg, EditorLayers.BothTilegrids
    ];

    public override List<EditorLayer> ValidLayers => _ValidLayers;

    public override List<ToolMode> ValidModes => _tileModes;

    public override string Name => "tile";
    
    public override string PersistenceGroup => "TileTool";

    public override void Init() {
        base.Init();

        EditorState.OnMapChanged += ClearMaterialListCache;

        foreach (var m in _tileModes) {
            if (m is TileMode mode)
                mode.Init();
        }
    }

    public override IEnumerable<object>? GetMaterials(EditorLayer layer) {
        var autotiler = GetAutotiler(layer);
        if (autotiler is not { })
            return null;

        autotiler.TilesetDataCacheToken.OnInvalidate += ClearMaterialListCache;
        return autotiler.Tilesets
            .Where(x => !x.Value.IsTemplate)
            .Select(x => x.Key)
            .Append('0')
            .Select(k => (object) k);
    }

    public override string GetMaterialDisplayName(EditorLayer layer, object material) {
        if (material is not char c)
            return material.ToString()!;
        
        if (c is '0') {
            return "Air";
        }

        return GetAutotiler(layer)?.GetTilesetDisplayName(c) ?? c.ToString();
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

    /// <summary>
    /// Returns <see cref="Tile"/>, or a different tile if needed. (For example, air if holding shift)
    /// </summary>
    /// <returns></returns>
    public char TileOrAlt(bool? shiftHeld = null) => (shiftHeld ?? Input.Keyboard.Shift()) ? '0' : Tile;

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

    public void RenderTileRectangle(Vector2 loc, int w, int h, bool hollow) {
        var autotiler = GetAutotiler(Layer);
        IEnumerable<ISprite>? sprites;

        if (hollow) {
            sprites = autotiler?.GetHollowRectSprites(loc, Tile, w, h, Color.White);
        } else {
            sprites = autotiler?.GetFilledRectSprites(loc, Tile, w, h, Color.White);
        }

        sprites ??= Array.Empty<ISprite>();

        var ctx = SpriteRenderCtx.Default(true);
        foreach (var item in sprites) {
            item.WithMultipliedAlpha(0.3f).Render(ctx);
        }
    }

    public override void Update(Camera camera, Room? room) {
        if (room is null)
            return;
        
        var (tx, ty) = GetMouseTilePos(camera, room);
        HandleMiddleClick(room, tx, ty);
        
        if (Mode is TileMode tileMode) {
            tileMode.Update(camera, room);
        }
    }

    public override void Render(Camera camera, Room room) {
        if (Mode is TileMode tileMode) {
            tileMode.Render(camera, room);
        }
    }

    public override void CancelInteraction() {
        base.CancelInteraction();
        
        if (Mode is TileMode tileMode) {
            tileMode.CancelInteraction();
        }
    }

    public override void RenderOverlay() {
    }

    public Tilegrid GetGrid(Room room, EditorLayer? layer = null) {
        layer ??= Layer;

        if (layer is TileEditorLayer tileEditorLayer)
            return tileEditorLayer.GetGrid(room);

        if (layer == EditorLayers.BothTilegrids)
            return room.FG;

        throw new ArgumentException("Provided layer is not a tile layer", nameof(layer));
    }

    public Tilegrid? GetSecondGrid(Room room) 
        => Layer == EditorLayers.BothTilegrids ? room.BG : null;

    public Point GetMouseTilePos(Camera camera, Room room, bool round = false, Point? fakeMousePos = null) {
        var pos = room.WorldToRoomPos(camera, (fakeMousePos ?? Input.Mouse.Pos).ToVector2());
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
            if (tileset is null) {
                return autotiler.MissingTileset.GetPreviewWidget(PreviewSize);
            }
            
            return tileset?.GetPreviewWidget(PreviewSize);
        }

        return base.GetMaterialPreview(material);
    }

    protected override XnaWidgetDef CreateTooltipPreview(XnaWidgetDef materialPreview, object material) {
        return materialPreview;
    }
}
