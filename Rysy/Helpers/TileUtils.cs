namespace Rysy.Helpers; 

/// <summary>
/// Contains helper functions for tiles and shapes in grids
/// </summary>
public static class TileUtils {
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
    /// Gets all intersections between the provided filled circle and a 1x1 grid
    /// </summary>
    public static IEnumerable<Point> GetCircleGridIntersection(Point start, int radius) {
        var startVec2 = start.ToVector2();
        // offsetting the radius yields a nicer circle
        var radiusOffset = radius + 0.5f;
        
        var radiusSquared = radiusOffset * radiusOffset;
        
        for (int x = 0; x <= radius; x++) {
            for (int y = 0; y <= radius; y++) {
                var pos = new NumVector2(x, y);

                if (pos.LengthSquared() > radiusSquared)
                    continue;
                
                switch (x, y) {
                    case (0, 0):
                        yield return start;
                        break;
                    case (_, 0):
                        yield return new(start.X + x, start.Y);
                        yield return new(start.X - x, start.Y);
                        break;
                    case (0, _):
                        yield return new(start.X, start.Y + y);
                        yield return new(start.X, start.Y - y);
                        break;
                    default:
                        yield return new(start.X + x, start.Y + y);
                        yield return new(start.X - x, start.Y + y);
                        yield return new(start.X - x, start.Y - y);
                        yield return new(start.X + x, start.Y - y);
                        break;
                }
            }
        }
    }
    
    /// <summary>
    /// Gets all intersections between the provided hollow circle and a 1x1 grid
    /// </summary>
    public static IEnumerable<Point> GetHollowCircleGridIntersection(Point center, int radius) {
        // offsetting the radius yields a nicer circle
        var radiusF = radius + 0.5f;
        var radiusFSq = radiusF * radiusF;

        var points = new List<Point>();
        if (radius < 1) {
            points.Add(center);
            return points;
        }
        
        // https://www.redblobgames.com/grids/circle-drawing/
        for (int r = 0; r <= float.Floor(radiusF * float.Sqrt(0.5f)); r++) {
            int d = (int)float.Floor(float.Sqrt(radiusFSq - r*r));
            points.Add(new Point(center.X - d, center.Y + r));
            points.Add(new Point(center.X + d, center.Y + r));
            if (r != 0) {
                points.Add(new Point(center.X - d, center.Y - r));
                points.Add(new Point(center.X + d, center.Y - r));
            }

            if (r != d) {
                points.Add(new Point(center.X + r, center.Y - d));
                points.Add(new Point(center.X + r, center.Y + d));

                if (r != 0) {
                    points.Add(new Point(center.X - r, center.Y - d));
                    points.Add(new Point(center.X - r, center.Y + d));
                }
            }
        }

        return points;
    }
    
    /// <summary>
    /// Gets all intersections between the provided circle and a 1x1 grid.
    /// The 'end' point might not necessarily be included in the circle
    /// </summary>
    public static IEnumerable<Point> GetCircleGridIntersection(Point start, Point end) {
        var radius = (int)Vector2.Distance(start.ToVector2(), end.ToVector2());

        return GetCircleGridIntersection(start, radius);
    }
    
    /// <summary>
    /// Gets all intersections between the provided circle and a 1x1 grid.
    /// The 'end' point might not necessarily be included in the circle
    /// </summary>
    public static IEnumerable<Point> GetHollowCircleGridIntersection(Point start, Point end) {
        var radius = (int)Vector2.Distance(start.ToVector2(), end.ToVector2());

        return GetHollowCircleGridIntersection(start, radius);
    }
    
    
    /// <summary>
    /// Gets all intersections between the provided filled ellipse and a 1x1 grid
    /// </summary>
    public static IEnumerable<Point> GetEllipseGridIntersection(Point center, int rx, int ry) {
        var startVec2 = center.ToVector2();

        var h = startVec2.X;
        var k = startVec2.Y;
        var rxsq = Math.Pow(rx + 0.5f, 2);
        var rysq = Math.Pow(ry + 0.5f, 2);
        
        for (int x = center.X - rx; x <= center.X + rx; x++) {
            for (int y = center.Y - ry; y <= center.Y + ry; y++) {
                // https://math.stackexchange.com/a/76463
                var dist =
                    Math.Pow(x - h, 2) / rxsq +
                    Math.Pow(y - k, 2) / rysq;

                if (dist <= 1)
                    yield return new(x, y);
            }
        }
    }
    
    public static IEnumerable<Point> GetEllipseGridIntersection(Point start, Point end) {
        return GetEllipseGridIntersection(start, int.Abs(start.X - end.X), int.Abs(start.Y - end.Y));
    }
    
    /// <summary>
    /// Gets all intersections between the provided filled ellipse and a 1x1 grid
    /// </summary>
    public static IEnumerable<Point> GetHollowEllipseGridIntersection(Point center, int rx, int ry) {
        // https://www.geeksforgeeks.org/midpoint-ellipse-drawing-algorithm/
        List<Point> points = new();

        double x = 0;
        double y = ry;
 
        double d1 = (ry * ry) - (rx * rx * ry) + (0.25f * rx * rx);
        double dx = 2 * ry * ry * x;
        double dy = 2 * rx * rx * y;

        var rySqr2 = 2 * ry * ry;
        var rxSqr2 = 2 * rx * rx;
     
        while (dx < dy)
        {
            Plot4EllipsePoints(points, center, (int)x, (int)y);
            x++;
            dx += rySqr2;
            
            if (d1 < 0) 
            {
                d1 += dx + (ry * ry);
            }
            else
            {
                y--;
                dy -= rxSqr2;
                d1 += dx - dy + (ry * ry);
            }
        }

        double d2 = ((ry * ry) * ((x + 0.5f) * (x + 0.5f))) + ((rx * rx) * ((y - 1) * (y - 1))) - (rx * rx * ry * ry);

        while (y >= 0)
        {
            Plot4EllipsePoints(points, center, (int)x, (int)y);
            y--;
            dy -= rxSqr2;
            if (d2 > 0)
            {
                d2 += (rx * rx) - dy;
            }
            else
            {
                x++;
                dx += rySqr2;
                d2 += dx - dy + (rx * rx);
            }
        }
    
        return points;
        void Plot4EllipsePoints(List<Point> points, Point center, int x, int y) {
            // Switch to make sure not to create duplicate points
            switch (x, y) {
                case (0, 0):
                    points.Add(center);
                    break;
                case (_, 0):
                    points.Add(new(center.X + x, center.Y));
                    points.Add(new(center.X - x, center.Y));
                    break;
                case (0, _):
                    points.Add(new(center.X, center.Y + y));
                    points.Add(new(center.X, center.Y - y));
                    break;
                default:
                    points.Add(new(center.X + x, center.Y + y));
                    points.Add(new(center.X - x, center.Y + y));
                    points.Add(new(center.X - x, center.Y - y));
                    points.Add(new(center.X + x, center.Y - y));
                    break;
            }
        }
    }
    
    public static IEnumerable<Point> GetHollowEllipseGridIntersection(Point start, Point end) {
        return GetHollowEllipseGridIntersection(start, int.Abs(start.X - end.X), int.Abs(start.Y - end.Y));
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