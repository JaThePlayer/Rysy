using Rysy.Extensions;
using System.Collections;

namespace Rysy.Helpers; 

/// <summary>
/// Contains helper functions that don't really fit anywhere else yet
/// </summary>
public static class Utils {
    // https://gamedev.stackexchange.com/questions/20103/finding-which-tiles-are-intersected-by-a-line-without-looping-through-all-of-th
    /// <summary>
    /// Gets all intersections between the provided line and a 1x1 grid.
    /// </summary>
    public static IEnumerable<Point> GetLineGridIntersection(Point a, Point b) {
        var ret = new List<Point>(1);
        
        if (a == b) {
            ret.Add(a);
            return ret;
        }
        
        var (x0, y0) = a;
        var (x1, y1) = b;
        
        int dx = int.Abs(x1 - x0);
        int dy = int.Abs(y1 - y0);
        int x = x0;
        int y = y0;
        int n = 1 + dx + dy;
        int xInc = (x1 > x0) ? 1 : -1;
        int yInc = (y1 > y0) ? 1 : -1;
        int error = dx - dy;
        dx *= 2;
        dy *= 2;

        for (; n > 0; --n) {
            ret.Add(new(x, y));

            switch (error)
            {
                case > 0:
                    x += xInc;
                    error -= dy;
                    break;
                case < 0:
                    y += yInc;
                    error += dx;
                    break;
                default:
                    // Ensure that perfectly diagonal lines don't take up more tiles than necessary.
                    // http://playtechs.blogspot.com/2007/03/raytracing-on-grid.html?showComment=1281448902099#c3785285092830049685
                    x += xInc;
                    y += yInc;
                    error -= dy;
                    error += dx;
                    --n;
                    break;
            }
        }

        return ret;
    }
    
    /// <summary>
    /// Gets all intersections between the provided circle and a 1x1 grid
    /// </summary>
    public static IEnumerable<Point> GetCircleGridIntersection(Point start, int radius) {
        var startVec2 = start.ToVector2();
        var radiusSquared = radius * radius;
        
        for (int x = start.X - radius; x <= start.X + radius; x++) {
            for (int y = start.Y - radius; y <= start.Y + radius; y++) {
                var pos = new Vector2(x, y);

                if (Vector2.DistanceSquared(startVec2, pos) <= radiusSquared)
                    yield return pos.ToPoint();
            }
        }
    }
    
    /// <summary>
    /// Gets all intersections between the provided circle and a 1x1 grid.
    /// The 'end' point might not necessarily be included in the circle
    /// </summary>
    public static IEnumerable<Point> GetCircleGridIntersection(Point start, Point end) {
        var radius = (int)Vector2.Distance(start.ToVector2(), end.ToVector2());

        return GetCircleGridIntersection(start, radius);
    }
    
    // https://en.wikipedia.org/wiki/Flood_fill#Span_filling
    /// <summary>
    /// Performs a generalized flood fill, starting from <paramref name="sx"/>, <paramref name="sy"/>.
    /// </summary>
    /// <param name="sx">Starting x point of the fill</param>
    /// <param name="sy">Starting y point of the fill</param>
    /// <param name="inside">Returns true for unfilled points that should be inside the filled area. If <paramref name="set"/> got called on this tile, this should return false!</param>
    /// <param name="set">Fills a pixel/node. Any point that has Set called on it must then no longer be Inside.</param>
    /// <param name="cap">The limit on how many tiles can be flood filled at once</param>
    /// <returns>Whether the entire area got flooded. Returns false if the <paramref name="cap"/> got reached.</returns>
    public static bool FloodFill(int sx, int sy, Func<int, int, bool> inside, Action<int, int> set, int? cap = null) {
        if (!inside(sx, sy))
            return true;

        var count = 0;
        var s = new Queue<(int, int, int, int)>();
        
        s.Enqueue((sx, sx, sy, 1));
        s.Enqueue((sx, sx, sy - 1, -1));

        while (s.Count != 0) {
            var (x1, x2, y, dy) = s.Dequeue();
            var x = x1;

            if (inside(x, y)) {
                while (inside(x - 1, y)) {
                    set(x - 1, y);
                    count++;
                    if (cap < count) {
                        return false;
                    }
                    x -= 1;
                }
                
                if (x < x1) {
                    s.Enqueue((x, x1 - 1, y - dy, -dy));
                }
            }

            while (x1 <= x2) {
                while (inside(x1, y)) {
                    set(x1, y);
                    count++;
                    if (cap < count) {
                        return false;
                    }
                    x1 += 1;
                }

                if (x1 > x) {
                    s.Enqueue((x, x1 - 1, y + dy, dy));
                }

                if (x1 - 1 > x2) {
                    s.Enqueue((x2 + 1, x1 - 1, y - dy, -dy));
                }

                x1 += 1;

                while (x1 < x2 && !inside(x1, y)) {
                    x1 += 1;
                }
                x = x1;
            }
        }

        return true;
    }
}