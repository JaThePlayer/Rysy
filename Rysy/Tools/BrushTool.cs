using Rysy.Graphics;
using Rysy.History;

namespace Rysy.Tools;

public class BrushTool : TileTool
{
    public override void Render(Camera camera, Room currentRoom)
    {
        var mouse = currentRoom.WorldToRoomPos(camera, Input.Mouse.Pos.ToVector2()).Snap(8).ToPoint();

        ISprite.OutlinedRect(new Rectangle(mouse, new Vector2(8f, 8f).ToPoint()), Color.Transparent, DefaultColor).Render();
    }

    public override void Update(Camera camera, Room room)
    {
        var (tx, ty) = GetMouseTilePos(camera, room);

        if (Input.Mouse.Left.Held())
        {
            History.ApplyNewAction(new TileChangeAction(Input.Keyboard.Shift() ? '0' : Tile, tx, ty, GetGrid(room), GetSecondGrid(room)));
        }

        base.Update(camera, room);
    }
}
