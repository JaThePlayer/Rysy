using Microsoft.Xna.Framework.Graphics;
using Rysy.Graphics.TextureTypes;
using System.Runtime.CompilerServices;

namespace Rysy.Graphics;

public record struct Sprite : ISprite
{
    public int? Depth { get; set; }

    public Vector2 Pos;

    public VirtTexture Texture;
    public Rectangle? ClipRect;

    public Color Color { get; set; } = Color.White;
    public float Alpha
    {
        get => Color.A / 255f;
        set
        {
            //Color = new Color(Color, value);
            Color = Color * value;
        }
    }

    public bool IsLoaded => Texture.Texture is { };

    public Color OutlineColor = default;

    public float Rotation = 0f;
    public Vector2 Origin = Vector2.Zero;
    public Vector2 Scale = Vector2.One;
    private SpriteEffects Flip = SpriteEffects.None;

    public Vector2 DrawOffset;
    public int Width;
    public int Height;

    public Sprite(VirtTexture text)
    {
        Texture = text;
        DrawOffset = text.DrawOffset;
    }

    /// <summary>
    /// Forcefully gets the width of this sprite. If this uses a modded texture, it'll likely cause preloading.
    /// </summary>
    public int ForceGetWidth()
    {
        if (Width == 0)
        {
            LoadSizeFromTexture();
        }

        return Width;
    }

    /// <summary>
    /// Forcefully gets the height of this sprite. If this uses a modded texture, it'll likely cause preloading.
    /// </summary>
    public int ForceGetHeight()
    {
        if (Height == 0)
        {
            LoadSizeFromTexture();
        }

        return Height;
    }

    private void LoadSizeFromTexture()
    {
        Width = Texture.Width;
        Height = Texture.Height;
    }

    public void Render(Camera? cam, Vector2 offset)
    {
        if (Texture.Texture is { } texture)
        {
            // store some fields for later use
            // this is not done in the constructor, as that would force preloading
            ClipRect ??= Texture.ClipRect;
            if (Width == 0)
            {
                LoadSizeFromTexture();

                // Fixup properties now, at this point nothing should try to get stuff from the sprite...
                Flip = SpriteEffects.None;
                Origin = (Origin * new Vector2(Width, Height)) + DrawOffset;
                // Monogame doesn't like negative scales...
                if (Scale.X < 0)
                {
                    Scale.X = -Scale.X;
                    Flip ^= SpriteEffects.FlipHorizontally;
                    Origin.X = ClipRect!.Value.Width - Origin.X;
                }
                if (Scale.Y < 0)
                {
                    Scale.Y = -Scale.Y;
                    Flip ^= SpriteEffects.FlipVertically;
                    Origin.Y = ClipRect!.Value.Height - Origin.Y;
                }
            }

            var scale = Scale;

            if (cam is { })
            {
                var size = new Vector2(Width * scale.X, Height * scale.Y);
                var pos = Pos - Origin * scale;
                //ISprite.HollowRect(pos, (int)size.X, (int)size.Y, Color.Transparent, Color.Red).Render();
                if (!cam.IsRectVisible(pos + offset, (int)size.X, (int)size.Y))
                    return;
            }

            var flip = Flip;
            var origin = Origin;
            if (OutlineColor != default)
            {
                var color = OutlineColor;

                Render(texture, Pos + new Vector2(-1f, 0f), color, scale, flip, origin);
                Render(texture, Pos + new Vector2(1f, 0f), color, scale, flip, origin);
                Render(texture, Pos + new Vector2(0f, 1f), color, scale, flip, origin);
                Render(texture, Pos + new Vector2(0f, -1f), color, scale, flip, origin);
            }
            Render(texture, Pos, Color, scale, flip, origin);
        }
    }

    public void Render()
    {
        Render(null, default);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Render(Texture2D texture, Vector2 pos, Color color, Vector2 scale, SpriteEffects flip, Vector2 origin)
    {
        GFX.Batch.Draw(texture, pos, ClipRect, color, Rotation, origin, scale, flip, 0f);
    }

    public Sprite Centered()
    {
        Origin = new(.5f, .5f);
        return this;
    }

    public Sprite CreateSubtexture(int x, int y, int w, int h)
    {
        return this with
        {
            ClipRect = GetSubtextureRect(x, y, w, h),
            DrawOffset = new Vector2(-Math.Min(x - DrawOffset.X, 0f), -Math.Min(y - DrawOffset.Y, 0f)),
            Width = w,
            Height = h
        };
    }

    public Rectangle GetSubtextureRect(int x, int y, int w, int h)
    {
        var clipRectPos = ClipRect?.Location ?? Texture.ClipRectPos;

        var newY = clipRectPos.Y + y;
        var newX = clipRectPos.X + x;

        if (Texture is VanillaTexture)
        {
            // since vanilla atlases trim whitespace, it's important to reduce the width/height of subtextures in such a way that
            // we don't start rendering parts of other textures.
            // If this code wasn't here, vanilla jumpthrus would render part of a different sprite below them, for example

            var clipRect = ClipRect ??= Texture.ClipRect;
            if (newY + h > clipRect.Bottom)
                h = clipRect.Bottom - newY;
            if (newX + w > clipRect.Right)
                w = clipRect.Right - newX;
        }

        return new(newX, newY, w, h);
    }
}
