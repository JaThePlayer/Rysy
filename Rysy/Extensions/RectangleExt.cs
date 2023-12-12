using Rysy.Helpers;
using System.Collections;

namespace Rysy.Extensions;

public static class RectangleExt {
    public static Rectangle FromPoints(Vector2 a, Vector2 b) => FromPoints(a.ToPoint(), b.ToPoint());

    //https://stackoverflow.com/questions/45259380/convert-2-vector2-points-to-a-rectangle-in-xna-monogame
    public static Rectangle FromPoints(Point a, Point b) {
        //we need to figure out the top left and bottom right coordinates
        //we need to account for the fact that a and b could be any two opposite points of a rectangle, not always coming into this method as topleft and bottomright already.
        int smallestX = Math.Min(a.X, b.X);
        int smallestY = Math.Min(a.Y, b.Y);
        int largestX = Math.Max(a.X, b.X);
        int largestY = Math.Max(a.Y, b.Y);

        int width = largestX - smallestX;
        int height = largestY - smallestY;

        return new Rectangle(smallestX, smallestY, width, height);
    }

    public static Rectangle FromPoints(IEnumerable<Vector2> points) {
        int smallestX = int.MaxValue, smallestY = int.MaxValue;
        int largestX = int.MinValue, largestY = int.MinValue;
        foreach (var p in points) {
            var x = (int) p.X;
            var y = (int) p.Y;
            smallestX = int.Min(x, smallestX);
            smallestY = int.Min(y, smallestY);
            largestX = int.Max(x, largestX);
            largestY = int.Max(y, largestY);
        }

        int width = largestX - smallestX;
        int height = largestY - smallestY;

        return new Rectangle(smallestX, smallestY, width, height);
    }
    
    public static Rectangle FromPoints(IEnumerable<Point> points) {
        int smallestX = int.MaxValue, smallestY = int.MaxValue;
        int largestX = int.MinValue, largestY = int.MinValue;
        foreach (var p in points) {
            var x = (int) p.X;
            var y = (int) p.Y;
            smallestX = int.Min(x, smallestX);
            smallestY = int.Min(y, smallestY);
            largestX = int.Max(x, largestX);
            largestY = int.Max(y, largestY);
        }

        int width = largestX - smallestX;
        int height = largestY - smallestY;

        return new Rectangle(smallestX, smallestY, width, height);
    }

    public static Rectangle Merge(Rectangle a, Rectangle b) {
        int smallestX = int.Min(a.Left, b.Left);
        int smallestY = int.Min(a.Top, b.Top);
        int largestX = int.Max(a.Right, b.Right);
        int largestY = int.Max(a.Bottom, b.Bottom);

        int width = largestX - smallestX;
        int height = largestY - smallestY;

        return new Rectangle(smallestX, smallestY, width, height);
    }

    public static Rectangle Merge(IEnumerable<Rectangle> rectangles) {
        bool any = false;
        int smallestX = int.MaxValue;
        int smallestY = int.MaxValue;
        int largestX = int.MinValue;
        int largestY = int.MinValue;

        foreach (var r in rectangles) {
            any = true;

            smallestX = int.Min(smallestX, r.X);
            smallestY = int.Min(smallestY, r.Y);
            largestX = int.Max(largestX, r.Right);
            largestY = int.Max(largestY, r.Bottom);
        }

        if (!any) {
            return new Rectangle(0, 0, 0, 0);
        }

        int width = largestX - smallestX;
        int height = largestY - smallestY;

        return new Rectangle(smallestX, smallestY, width, height);
    }

    public static Rectangle MultSize(this Rectangle r, int mult) {
        return new(r.X, r.Y, r.Width * mult, r.Height * mult);
    }

    public static Rectangle Mult(this Rectangle r, int mult) {
        return new(r.X * mult, r.Y * mult, r.Width * mult, r.Height * mult);
    }

    public static Rectangle Div(this Rectangle r, int mult) {
        return new(r.X / mult, r.Y / mult, r.Width / mult, r.Height / mult);
    }

    public static Rectangle AddSize(this Rectangle r, int w, int h) => new(r.X, r.Y, r.Width + w, r.Height + h);
    public static Rectangle AddSize(this Rectangle r, Point offset) => new(r.X, r.Y, r.Width + offset.X, r.Height + +offset.Y);

    public static Rectangle MovedBy(this Rectangle r, Vector2 offset) => new(r.X + (int) offset.X, r.Y + (int) offset.Y, r.Width, r.Height);
    public static Rectangle MovedBy(this Rectangle r, int x, int y) => new(r.X + x, r.Y + y, r.Width, r.Height);
    public static Rectangle MovedTo(this Rectangle r, Vector2 pos) => new((int) pos.X, (int) pos.Y, r.Width, r.Height);

    public static Point Size(this Rectangle r) => new(r.Width, r.Height);

    public static NineSliceLocation? GetLocationInRect(this Rectangle r, Point pos, int leniency = 1) {
        if (!r.Contains(pos))
            return null;

        if (new Rectangle(r.X, r.Y, leniency, leniency).Contains(pos))
            return NineSliceLocation.TopLeft;
        if (new Rectangle(r.Right - leniency, r.Y, leniency, leniency).Contains(pos))
            return NineSliceLocation.TopRight;

        if (new Rectangle(r.X, r.Bottom - leniency, leniency, leniency).Contains(pos))
            return NineSliceLocation.BottomLeft;
        if (new Rectangle(r.Right - leniency, r.Bottom - leniency, leniency, leniency).Contains(pos))
            return NineSliceLocation.BottomRight;

        if (new Rectangle(r.X, r.Y, r.Width, leniency).Contains(pos))
            return NineSliceLocation.TopMiddle;
        if (new Rectangle(r.X, r.Bottom - leniency, r.Width, leniency).Contains(pos))
            return NineSliceLocation.BottomMiddle;

        if (new Rectangle(r.X, r.Y, leniency, r.Height).Contains(pos))
            return NineSliceLocation.Left;
        if (new Rectangle(r.Right - leniency, r.Y, leniency, r.Height).Contains(pos))
            return NineSliceLocation.Right;

        return null;
    }

    /// <summary>
    /// Returns an enumerator which returns all grid locations that are within the given rectangle.
    /// </summary>
    public static GridLocationsEnumerator EnumerateGridLocations(this Rectangle r) => new(r);
    
    /// <summary>
    /// Returns an enumerator which returns all grid locations that are on the edges of the given rectangle.
    /// </summary>
    public static GridEdgeLocationsEnumerator EnumerateGridEdgeLocations(this Rectangle r) => new(r);

    public struct GridLocationsEnumerator : IEnumerator<Point>, IEnumerable<Point> {
        private Rectangle _rectangle;
        private int _x, _y;

        public GridLocationsEnumerator(Rectangle r) {
            _rectangle = r;
            Reset();
        }
        
        public bool MoveNext() {
            var r = _rectangle;
            if (_x >= r.Right) {
                _x = r.X;
                _y++;
                return _y <= r.Bottom;
            }

            _x++;
            return true;
        }

        public void Reset() {
            _x = _rectangle.X - 1;
            _y = _rectangle.Y;
        }

        object IEnumerator.Current => Current;

        public Point Current => new(_x, _y);

        public void Dispose() {
        }

        public GridLocationsEnumerator GetEnumerator() 
            => this;
        
        IEnumerator<Point> IEnumerable<Point>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    
    public struct GridEdgeLocationsEnumerator : IEnumerator<Point>, IEnumerable<Point> {
        private Rectangle _rectangle;
        private int _x, _y;

        public GridEdgeLocationsEnumerator(Rectangle r) {
            _rectangle = r;
            Reset();
        }

        public GridEdgeLocationsEnumerator GetEnumerator() 
            => this;
        
        IEnumerator<Point> IEnumerable<Point>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        public bool MoveNext() {
            var r = _rectangle;
            
            // move down if needed
            if (_x >= r.Right) {
                _x = r.X;
                _y++;
                return _y <= r.Bottom;
            }
            
            if (_y == r.Y || _y == r.Bottom) {
                // top and bottom rows are full
            } else {
                // inner rows only have left and right points
                if (_x == r.X) {
                    _x = r.Right;
                    return true;
                }
            }

            _x++;
            return true;
        }

        public void Reset() {
            _x = _rectangle.X - 1;
            _y = _rectangle.Y;
        }

        object IEnumerator.Current => Current;

        public Point Current => new(_x, _y);

        public void Dispose() {
        }
    }
}
