namespace Rysy;

public static class RectangleExt {
    public static Rectangle FromPoints(Vector2 a, Vector2 b) => FromPoints(a.ToPoint(), b.ToPoint());

    //https://stackoverflow.com/questions/45259380/convert-2-vector2-points-to-a-rectangle-in-xna-monogame
    public static Rectangle FromPoints(Point a, Point b) {
        //we need to figure out the top left and bottom right coordinates
        //we need to account for the fact that a and b could be any two opposite points of a rectangle, not always coming into this method as topleft and bottomright already.
        int smallestX = Math.Min(a.X, b.X); //Smallest X
        int smallestY = Math.Min(a.Y, b.Y); //Smallest Y
        int largestX = Math.Max(a.X, b.X);  //Largest X
        int largestY = Math.Max(a.Y, b.Y);  //Largest Y

        //calc the width and height
        int width = largestX - smallestX;
        int height = largestY - smallestY;

        //assuming Y is small at the top of screen
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
    public static Rectangle MovedTo(this Rectangle r, Vector2 pos) => new((int) pos.X, (int) pos.Y, r.Width, r.Height);
}
