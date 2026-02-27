using Hexa.NET.ImGui;
using Rysy.Components;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.Gui;
using Rysy.Layers;
using Rysy.Signals;

namespace Rysy.Tools;

public class TileTool : Tool, ISignalListener<MapSwapped> {
    public Color DefaultColor = ColorHelper.HsvToColor(0f, 1f, 1f);

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

    public new TileEditorLayer? Layer {
        get => base.Layer as TileEditorLayer;
        set => base.Layer = value ?? throw new ArgumentNullException(nameof(value));
    }
    
    public override IReadOnlyList<TileEditorLayer> ValidLayers => ToolHandler.ComponentRegistry.GetAll<TileEditorLayer>();

    public override List<ToolMode> ValidModes => _tileModes;

    public override string Name => "tile";
    
    public override string PersistenceGroup => "TileTool";

    public override void Init() {
        base.Init();

        foreach (var m in _tileModes) {
            if (m is TileMode mode)
                mode.Init();
        }
    }

    public override IEnumerable<object>? GetMaterials(IEditorLayer layer) {
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

    public override string SerializeMaterial(IEditorLayer layer, object? material) {
        return material?.ToString() ?? "";
    }

    public override object DeserializeMaterial(IEditorLayer layer, string serializableMaterial) {
        return serializableMaterial[0];
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

    private Autotiler? GetAutotiler(IEditorLayer? layer) {
        return (layer as TileEditorLayer)?.GetAutotiler(EditorState.Map);
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

    public Tilegrid GetGrid(Room room, IEditorLayer? layer = null) {
        layer ??= Layer;

        if (layer is TileEditorLayer tileEditorLayer)
            return tileEditorLayer.GetGrid(room);

        throw new ArgumentException("Provided layer is not a tile layer", nameof(layer));
    }

    public Point GetMouseTilePos(Camera camera, Room room, bool round = false, Point? fakeMousePos = null) {
        var pos = room.WorldToRoomPos(camera, (fakeMousePos ?? Input.Mouse.Pos).ToVector2());
        if (round) {
            return pos.GridPosRound(8);
        }

        return pos.GridPosFloor(8);
    }

    protected void HandleMiddleClick(Room currentRoom, int tx, int ty) {
        if (!Input.Mouse.Middle.Clicked())
            return;
        Input.Mouse.ConsumeMiddle();

        var layers = ValidLayers.OrderBy(x => x.Depth);

        foreach (var layer in layers) {
            var tile = layer.GetGrid(currentRoom).SafeTileAt(tx, ty);
            if (tile != '0') {
                Layer = layer;
                Tile = tile;
                ToolHandler.PushRecentMaterial(Tile);
                return;
            }
        }
            
        // All tiles on all layers are air, so just switch to air in this layer.
        Tile = '0';
        ToolHandler.PushRecentMaterial(Tile);
    }

    public override float MaterialListElementHeight() 
        => Settings.Instance.ShowPlacementIcons ? PreviewSize + ImGui.GetStyle().FramePadding.Y : base.MaterialListElementHeight();

    internal override XnaWidgetDef? GetMaterialPreview(IEditorLayer layer, object material) {
        var autotiler = GetAutotiler(layer);
        if (autotiler is { } && material is char c) {
            var tileset = autotiler.GetTilesetData(c) ?? autotiler.MissingTileset;
            
            return tileset.GetPreviewWidget(PreviewSize);
        }

        return base.GetMaterialPreview(layer, material);
    }

    protected override XnaWidgetDef CreateTooltipPreview(XnaWidgetDef materialPreview, object material) {
        return materialPreview;
    }

    public void OnSignal(MapSwapped signal) {
        ClearMaterialListCache();
    }
}
