using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.History;

namespace Rysy.Tools;

public class TileRectangleMode : TileMode {
    public override string Name => Hollow ? "rectangleHollow" : "rectangle";

    public readonly bool Hollow;
    
    private MouseDragGesture<RectangleData> _dragGesture;
    
    public override void Render(Camera camera, Room room) {
        var mousePos = Tool.GetMouseTilePos(camera, room);
        var startPos = _dragGesture.Data?.StartPos ?? mousePos;
        var rect = SelectionRect(startPos, mousePos).Mult(8);

        const int maxTilesRendered = 1_000;
        if (rect.Size().ToVector2().LengthSquared() < maxTilesRendered * maxTilesRendered)
            Tool.RenderTileRectangle(rect.Location.ToVector2(), rect.Width / 8, rect.Height / 8, Hollow);

        
        if (_dragGesture.StartingPos is { } start) {
            Tool.DrawSelectionRect(rect);
        } else {
            ISprite.OutlinedRect(rect, Color.Transparent, Tool.DefaultColor).Render();
        }
    }

    public override void Update(Camera camera, Room room) {
        _dragGesture.Update(out var continueStroke, out var endStroke, out var lastMousePos, out var data);

        if (continueStroke || endStroke) {
            data!.StartPos ??= Tool.GetMouseTilePos(camera, room, fakeMousePos: _dragGesture.StartingPos);
        }
        
        if (endStroke) {
            var endPos = Tool.GetMouseTilePos(camera, room);
            var startPos = data!.StartPos!.Value;
            var rect = SelectionRect(startPos, endPos);
            var tile = Tool.TileOrAlt(_dragGesture.Shift);
            
            Tool.History.ApplyNewAction(new MergedAction(
                new TileRectChangeAction(tile, rect, Tool.GetGrid(room), Hollow),
                Tool.GetSecondGrid(room) is {} second ? new TileRectChangeAction(tile, rect, second, Hollow) : null
            ));
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

    public TileRectangleMode(TileTool tool, bool hollow) : base(tool) {
        Hollow = hollow;
    }

    internal sealed class RectangleData {
        public Point? StartPos;
    }
}
