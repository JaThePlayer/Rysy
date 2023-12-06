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
        if (a == b) {
            yield return a;
            yield break;
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
            yield return new(x, y);

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
    }
}