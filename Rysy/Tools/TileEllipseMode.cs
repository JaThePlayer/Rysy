using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.History;

namespace Rysy.Tools; 

public sealed class TileEllipseMode(TileTool tool, bool hollow) : TileShapeMode(tool) {
    public override string Name => hollow ? "ellipseHollow" : "ellipse";
    protected override IEnumerable<Point> GetChangedTileLocations(Point start, Point current)
        => hollow 
            ? TileUtils.GetHollowEllipseGridIntersection(start, current, Thickness)
            : TileUtils.GetEllipseGridIntersection(start, current);

    protected override IHistoryAction CreateAction(char id, Point start, Point current, Tilegrid tilegrid)
        => new TileEllipseChangeAction(id, start, current, tilegrid, hollow, Thickness);
    
    protected internal override int Thickness => hollow ? Persistence.Instance.Get<int>("tileToolThickness", 1) : 1;
}