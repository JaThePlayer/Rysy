using Rysy.Graphics;
using Rysy.History;

namespace Rysy.Tools;

public class BrushTool : Tool
{
// TODO: Move elsewhere
    const string FG = "FG";
    const string BG = "BG";
    const string BOTH = "Both";

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

    public override void Render(Camera camera, Room currentRoom)
    {
        var mouse = currentRoom.WorldToRoomPos(camera, Input.Mouse.Pos.ToVector2()).Snap(8).ToPoint();

        ISprite.OutlinedRect(new Rectangle(mouse, new Vector2(8f, 8f).ToPoint()), Color.Transparent, Color.Green).Render();
    }

    public override void RenderOverlay()
    {
        PicoFont.Print(Tile, new(4, 4), Color.White, 4);
        PicoFont.Print(Layer, new Vector2(4, 36), Color.White, 4);
    }

    private Tilegrid GetGrid(Room room, string? layer = null) => (layer ?? Layer) switch
    {
        FG => room.FG,
        BG => room.BG,
        _ => throw new NotImplementedException(Layer)
    };

    public override void Update(Camera camera, Room room)
    {
        var (tx, ty) = room.WorldToRoomPos(camera, Input.Mouse.Pos.ToVector2()).GridPos(8);


        if (Input.Mouse.Left.Held())
        {
            if (Layer != BOTH) {
                var grid = GetGrid(room);
                History.ApplyNewAction(new TileChangeAction(Input.Keyboard.Shift() ? '0' : Tile, tx, ty, grid));
            } else {
                History.ApplyNewAction(new TileChangeAction('0', tx, ty, room.FG));
                History.ApplyNewAction(new TileChangeAction('0', tx, ty, room.BG));
            }
            
        }

        HandleMiddleClick(room, tx, ty);
    }

    private void HandleMiddleClick(Room currentRoom, int tx, int ty)
    {
        if (Input.Mouse.Middle.Clicked())
        {
            Input.Mouse.ConsumeMiddle();
            var fg = currentRoom.FG.SafeTileAt(tx, ty);
            var bg = currentRoom.BG.SafeTileAt(tx, ty);

            (Layer, Tile) = (fg, bg) switch
            {
                ('0', '0') => (BOTH, bg), // if both tiles are air, switch to the "Both" layer.
                ('0', not '0') => (BG, bg), // fg is air, but bg isn't. Switch to BG.
                (not '0', _) => (FG, fg), // fg tile exists, swap to that.
            };
        }
    }
}
