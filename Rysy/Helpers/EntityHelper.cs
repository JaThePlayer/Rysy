namespace Rysy.Helpers;

internal static class EntityHelper
{
    public static Rectangle GetEntityRectangle(Entity e)
    {
        var bw = e.Width;
        var bh = e.Height;
        Rectangle bRect = new((int)e.Pos.X, (int)e.Pos.Y, bw == 0 ? 8 : bw, bh == 0 ? 8 : bh);
        return bRect;
    }
}