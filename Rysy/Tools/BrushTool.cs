using Rysy.Graphics;
using Rysy.History;

namespace Rysy.Tools;

public class BrushTool : TileTool {
    public override string Name => "Brush";

    public override void Render(Camera camera, Room currentRoom) {
        var mouse = currentRoom.WorldToRoomPos(camera, Input.Mouse.Pos.ToVector2()).Snap(8).ToPoint();

        RenderTiles(mouse.ToVector2(), 1, 1);
        ISprite.OutlinedRect(new Rectangle(mouse, new Point(8, 8)), Color.Transparent, DefaultColor).Render();
    }

    public override void Update(Camera camera, Room room) {
        var (tx, ty) = GetMouseTilePos(camera, room);

        if (Input.Mouse.Left.Held()) {
            History.ApplyNewAction(new TileChangeAction(Input.Keyboard.Shift() ? '0' : Tile, tx, ty, GetGrid(room), GetSecondGrid(room)));
        }

        base.Update(camera, room);
    }
}
