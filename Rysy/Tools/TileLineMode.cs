using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.History;

namespace Rysy.Tools;

public sealed class TileLineMode(TileTool tool) : TileShapeMode(tool) {
    public override string Name => "line";
    
    protected override IEnumerable<Point> GetChangedTileLocations(Point start, Point current)
        => TileUtils.GetLineGridIntersection(start, current);

    protected override IHistoryAction CreateAction(char id, Point start, Point current, Tilegrid tilegrid)
        => new TileLineChangeAction(id, start, current, tilegrid);
}