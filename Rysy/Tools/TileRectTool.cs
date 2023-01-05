﻿using Rysy.Graphics;
using Rysy.History;

namespace Rysy.Tools;

internal class TileRectTool : TileTool {
    private Point? startPos = null;

    public TileRectTool() {
        RysyEngine.OnLoseFocus += RysyEngine_OnLoseFocus;
    }

    private void RysyEngine_OnLoseFocus() {
        // After alt-tabbing and such, we should cancel the selection or we'll end up with accidental placements
        CancelDrag();
    }

    public void CancelDrag() {
        startPos = null;
    }

    private static Rectangle SelectionRect(Point start, Point mousePos) {
        return RectangleExt.FromPoints(start, mousePos).AddSize(1, 1);
    }

    public override void Render(Camera camera, Room room) {
        var mousePos = GetMouseTilePos(camera, room);
        var rect = SelectionRect(startPos ?? mousePos, mousePos).Mult(8);
        if (startPos is { } start) {
            var c = ColorHelper.HSVToColor(rect.Size.ToVector2().Length().Div(2f).Cap(70f), 1f, 1f);
            ISprite.OutlinedRect(rect, c * 0.3f, c).Render();
        } else {
            ISprite.OutlinedRect(rect, Color.Transparent, DefaultColor).Render();
        }

    }

    public override void Update(Camera camera, Room room) {
        switch (Input.Mouse.Left) {
            case MouseInputState.Released:
                if (startPos is { } start) {
                    var endPos = GetMouseTilePos(camera, room);
                    var rect = SelectionRect(start, endPos);

                    History.ApplyNewAction(new TileRectChangeAction(Input.Keyboard.Shift() ? '0' : Tile, rect, GetGrid(room), GetSecondGrid(room)));

                    startPos = null;
                }
                break;
            case MouseInputState.Held:
                break;
            case MouseInputState.Clicked:
                startPos = GetMouseTilePos(camera, room);
                break;
            default:
                break;
        }
        base.Update(camera, room);
    }
}