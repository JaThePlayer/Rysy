using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.History;

namespace Rysy.Tools;


public class TileRectangleMode(TileTool tool, bool hollow) : TileShapeMode(tool) {
    public override string Name => hollow ? "rectHollow" : "rectangle";
    
    protected override IEnumerable<Point> GetChangedTileLocations(Point start, Point current) {
        var rect = RectangleExt.FromPoints(start, current);

        return hollow ? rect.EnumerateGridEdgeLocations() : rect.EnumerateGridLocations();
    }

    protected override IHistoryAction CreateAction(char id, Point start, Point current, Tilegrid tilegrid) {
        var rect = RectangleExt.FromPoints(start, current);

        return new TileRectChangeAction(id, rect, tilegrid, hollow);
    }
}
