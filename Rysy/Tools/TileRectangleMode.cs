﻿using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.History;

namespace Rysy.Tools;

public class TileRectangleMode : TileMode {
    public override string Name => "rectangle";

    private MouseDragGesture<RectangleData> _dragGesture;
    
    public override void Render(Camera camera, Room room) {
        var mousePos = Tool.GetMouseTilePos(camera, room);
        var startPos = Tool.GetMouseTilePos(camera, room, fakeMousePos: _dragGesture.StartingPos);
        var rect = SelectionRect(startPos, mousePos).Mult(8);

        Tool.RenderTiles(rect.Location.ToVector2(), rect.Width / 8, rect.Height / 8);

        
        if (_dragGesture.StartingPos is { } start) {
            Tools.Tool.DrawSelectionRect(rect);
        } else {
            ISprite.OutlinedRect(rect, Color.Transparent, Tool.DefaultColor).Render();
        }
    }

    public override void Update(Camera camera, Room room) {
        _dragGesture.Update(out var continueStroke, out var endStroke, out var lastMousePos, out var data);

        if (endStroke) {
            var endPos = Tool.GetMouseTilePos(camera, room);
            var startPos = Tool.GetMouseTilePos(camera, room, fakeMousePos: _dragGesture.StartingPos!);
            var rect = SelectionRect(startPos, endPos);

            Tool.History.ApplyNewAction(new TileRectChangeAction(Tool.TileOrAlt(_dragGesture.Shift), rect,
                Tool.GetGrid(room), Tool.GetSecondGrid(room)));
        }
    }

    public override void CancelInteraction() {
        _dragGesture.CancelStroke();
    }

    public override void Init() {
        _dragGesture = new(Tool.Input);
    }
    
    private static Rectangle SelectionRect(Point start, Point mousePos) {
        return RectangleExt.FromPoints(start, mousePos).AddSize(1, 1);
    }

    public TileRectangleMode(TileTool tool) : base(tool)
    {
    }

    internal sealed class RectangleData {
        
    }
}