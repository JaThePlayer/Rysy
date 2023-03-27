namespace Rysy.Graphics.TextureTypes;

internal sealed class VanillaTexture : UndisposableVirtTexture {
    public int W, H;

    public override int Width => W;
    public override int Height => H;

    public override Rectangle GetSubtextureRect(int x, int y, int w, int h, out Vector2 _drawOffset, Rectangle? _clipRect = null) {
        /*
        var clipRectPos = clipRect?.Location ?? ClipRect.Location;

        var newY = clipRectPos.Y + y + (int) DrawOffset.Y;
        var newX = clipRectPos.X + x + (int) DrawOffset.X;

        // since vanilla atlases trim whitespace, it's important to reduce the width/height of subtextures in such a way that
        // we don't start rendering parts of other textures.
        // If this code wasn't here, vanilla jumpthrus would render part of a different sprite below them, for example

        var realClipRect = ClipRect;
        if (newY + h > realClipRect.Bottom)
            h = realClipRect.Bottom - newY;
        if (newX + w > realClipRect.Right)
            w = realClipRect.Right - newX;

        return new(newX, newY, w, h);*/

        
        _drawOffset = default;

        var clip = ClipRect;

        x += (int) DrawOffset.X;
        y += (int) DrawOffset.Y;

        var newX = clip.X + x;
        var newY = clip.Y + y;

        if (newX < clip.X) {
            var dif = newX - clip.X;
            newX -= dif;
            _drawOffset.X -= dif;
            w += dif;
        }

        if (newY < clip.Y) {
            var dif = newY - clip.Y;
            newY -= dif;
            _drawOffset.Y -= dif;
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

        /*
    -- Make sure the width/height doesn't go outside the original quad
    if hideOverflow ~= false then
        width = math.min(width, quadWidth - x)
        height = math.min(height, quadHeight - y)

        if x < 0 then
            width += x
            x = 0
        end

        if y < 0 then
            height += y
            y = 0
        end
    end

    return love.graphics.newQuad(quadX + x, quadY + y, width, height, imageWidth, imageHeight), offsetX, offsetY
         */

        /*
        _drawOffset = default;

        Rectangle clipRect = ClipRect;
        Vector2 drawOffset = DrawOffset;
        int x2 = clipRect.X;
        int y2 = clipRect.Y;
        int width2 = clipRect.Width;
        int height2 = clipRect.Height;
        int val = x2 + width2;
        int val2 = y2 + height2;
        int num = (int) drawOffset.X;
        int num2 = (int) drawOffset.Y;
        int num3 = x2 - num + x;
        int num4 = y2 - num2 + y;
        int num5 = Math.Max(x2, Math.Min(num3, val));
        int num6 = Math.Max(y2, Math.Min(num4, val2));
        int width3 = Math.Max(0, Math.Min(num3 + w, val) - num5);
        int height3 = Math.Max(0, Math.Min(num4 + h, val2) - num6);
        return new Rectangle(num5, num6, width3, height3);*/
    }
}
