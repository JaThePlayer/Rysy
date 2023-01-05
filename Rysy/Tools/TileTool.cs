using Rysy.Graphics;

namespace Rysy.Tools;

public abstract class TileTool : Tool
{
    public Color DefaultColor = ColorHelper.HSVToColor(0f, 1f, 1f);

    public const string LAYER_FG = "FG";
    public const string LAYER_BG = "BG";
    public const string LAYER_BOTH = "Both";

    public override string Layer
    {
        get => Persistence.Instance.Get("TileTool.Layer", "FG");
        set => Persistence.Instance.Set("TileTool.Layer", value);
    }

    public char Tile
    {
        get => Persistence.Instance.Get("TileTool.Tile", 'g');
        set => Persistence.Instance.Set("TileTool.Tile", value);
    }

    public override void Update(Camera camera, Room room)
    {
        var (tx, ty) = GetMouseTilePos(camera, room);
        HandleMiddleClick(room, tx, ty);
    }

    public override void RenderOverlay()
    {
        PicoFont.Print(Tile, new(4, 4), Color.White, 4);
        PicoFont.Print(Layer, new Vector2(4, 36), Color.White, 4);
    }

    protected Tilegrid GetGrid(Room room, string? layer = null) => (layer ?? Layer) switch
    {
        LAYER_FG or LAYER_BOTH => room.FG,
        LAYER_BG => room.BG,
        _ => throw new NotImplementedException(Layer)
    };

    protected Tilegrid? GetSecondGrid(Room room) => Layer switch
    {
        LAYER_BOTH => room.BG,
        _ => null,
    };

    protected static Point GetMouseTilePos(Camera camera, Room room, bool round = false) {
        var pos = room.WorldToRoomPos(camera, Input.Mouse.Pos.ToVector2());
        if (round) {
            return pos.GridPosRound(8);
        }

        return pos.GridPosFloor(8);
    }

    protected void HandleMiddleClick(Room currentRoom, int tx, int ty)
    {
        if (Input.Mouse.Middle.Clicked())
        {
            Input.Mouse.ConsumeMiddle();
            var fg = currentRoom.FG.SafeTileAt(tx, ty);
            var bg = currentRoom.BG.SafeTileAt(tx, ty);

            (Layer, Tile) = (fg, bg) switch
            {
                ('0', '0') => (LAYER_BOTH, bg), // if both tiles are air, switch to the "Both" layer.
                ('0', not '0') => (LAYER_BG, bg), // fg is air, but bg isn't. Switch to BG.
                (not '0', _) => (LAYER_FG, fg), // fg tile exists, swap to that.
            };
        }
    }
}
