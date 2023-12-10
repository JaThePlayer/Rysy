using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.History;

namespace Rysy.Tools; 

public sealed class TileCircleMode(TileTool tool, bool hollow) : TileShapeMode(tool) {
    public override string Name => hollow ? "circleHollow" : "circle";

    protected override IEnumerable<Point> GetChangedTileLocations(Point start, Point current)
        => hollow ? TileUtils.GetHollowCircleGridIntersection(start, current) : TileUtils.GetCircleGridIntersection(start, current);

    protected override IHistoryAction CreateAction(char id, Point start, Point current, Tilegrid tilegrid)
        => new TileCircleChangeAction(id, start, current, tilegrid, hollow);
}