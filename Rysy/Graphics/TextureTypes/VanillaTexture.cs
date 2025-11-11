namespace Rysy.Graphics.TextureTypes;

internal sealed class VanillaTexture : UndisposableVirtTexture {
    public int W, H;

    public override int Width => W;
    public override int Height => H;

    public override Rectangle GetSubtextureRect(int x, int y, int w, int h, out Vector2 drawOffset, Rectangle? clipRect = null) {
        drawOffset = default;

        var clip = ((VirtTexture)this).ClipRect;

        x += (int) DrawOffset.X;
        y += (int) DrawOffset.Y;

        var newX = clip.X + x;
        var newY = clip.Y + y;

        if (newX < clip.X) {
            var dif = newX - clip.X;
            newX -= dif;
            drawOffset.X -= dif;
            w += dif;
        }

        if (newY < clip.Y) {
            var dif = newY - clip.Y;
            newY -= dif;
            drawOffset.Y -= dif;
            h += dif;
        }

        if (newX + w > clip.Right) {
            var dif = newX + w - clip.Right;
            w -= dif;
        }

        if (newY + h > clip.Bottom) {
            var dif = newY + h - clip.Bottom;
            h -= dif;
        }

        return new Rectangle(newX, newY, w, h);
    }
}
