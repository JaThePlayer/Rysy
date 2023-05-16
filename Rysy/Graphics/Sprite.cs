using Rysy.Extensions;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Rysy.Graphics;

public record struct Sprite : ISprite {
    public int? Depth { get; set; }

    public Vector2 Pos;

    public VirtTexture Texture;
    public Rectangle? ClipRect;

    public Color Color { get; set; } = Color.White;

    public ISprite WithMultipliedAlpha(float alpha) {
        return this with {
            Color = Color * alpha,
            OutlineColor = OutlineColor * alpha,
        };
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

    private Vector2 _multOrigin;

    private Vector2 SubtextureOffset;

    private bool Prepared;

    public Sprite(VirtTexture text) {
        Texture = text;
        DrawOffset = text.DrawOffset;
    }

    /// <summary>
    /// Forcefully gets the width of this sprite. If this uses a modded texture, it'll likely cause preloading.
    /// </summary>
    public int ForceGetWidth() {
        if (Width == 0) {
            LoadSizeFromTexture();
        }

        return Width;
    }

    /// <summary>
    /// Forcefully gets the height of this sprite. If this uses a modded texture, it'll likely cause preloading.
    /// </summary>
    public int ForceGetHeight() {
        if (Height == 0) {
            LoadSizeFromTexture();
        }

        return Height;
    }

    private void LoadSizeFromTexture() {
        Width = Texture.Width;
        Height = Texture.Height;
    }

    public void Render(Camera? cam, Vector2 offset) {
        if (Texture.Texture is { } texture) {
            // store some fields for later use
            // this is not done in the constructor, as that would force preloading
            CacheFields();

            var scale = Scale;
            var origin = _multOrigin;

            // todo: figure out if calculating rotated rectangles for culling is worth it
            if (cam is { } && Rotation == 0f) {
                var size = new Vector2(Width * scale.X, Height * scale.Y);
                var rPos = Pos - origin * scale;
                //ISprite.OutlinedRect(rPos + offset, (int)size.X, (int)size.Y, Color.Transparent, Color.Red).Render();
                if (!cam.IsRectVisible(rPos + offset, (int) size.X, (int) size.Y))
                    return;
            }

            var flip = Flip;
            var pos = Pos + SubtextureOffset.Rotate(Rotation);

            if (OutlineColor != default) {
                var color = OutlineColor;

                Render(texture, pos + new Vector2(-1f, 0f), color, scale, flip, origin);
                Render(texture, pos + new Vector2(1f, 0f), color, scale, flip, origin);
                Render(texture, pos + new Vector2(0f, 1f), color, scale, flip, origin);
                Render(texture, pos + new Vector2(0f, -1f), color, scale, flip, origin);
            }

            if (Color != default)
                Render(texture, pos, Color, scale, flip, origin);

            {
                //var size = new Vector2(Width * scale.X, Height * scale.Y);
                //var pos = Pos - origin * scale;
                //ISprite.OutlinedRect(pos + offset, (int) size.X, (int) size.Y, Color.Transparent, Color.Red * 0.1f).Render();
            }

        }
    }

    public void Render() {
        Render(null, default);
    }

    public void RenderWithColor(Color color) {
        var old = Color;
        Color = color;
        Render();
        Color = old;
    }

    public ISelectionCollider GetCollider() => ISelectionCollider.FromSprite(this);

    public Rectangle? GetRenderRect() {
        if (Texture.Texture is not { } texture) {
            return null;
        }

        CacheFields();

        var scale = Scale;
        var size = new Vector2(ClipRect!.Value.Width * scale.X, ClipRect.Value.Height * scale.Y);
        Vector2 pos;
        if (Rotation == 0f) {
            pos = Pos - _multOrigin * scale + SubtextureOffset;
            if (OutlineColor != default) {
                return new Rectangle((int) pos.X - 1, (int) pos.Y - 1, (int) size.X + 2, (int) size.Y + 2);
            } else {
                return new Rectangle((int) pos.X, (int) pos.Y, (int) size.X, (int) size.Y);
            }
        }

        // rotate our points, by rotating the offset
        var off = -_multOrigin;

        var p1 = off.Rotate(Rotation);
        var p2 = (off + new Vector2(size.X, 0)).Rotate(Rotation);
        var p3 = (off + new Vector2(0, size.Y)).Rotate(Rotation);
        var p4 = (off + size).Rotate(Rotation);

        var r1 = Pos + new Vector2(
            Math.Min(p4.X, Math.Min(p3.X, Math.Min(p1.X, p2.X))),
            Math.Min(p4.Y, Math.Min(p3.Y, Math.Min(p1.Y, p2.Y)))
        ) + SubtextureOffset.Rotate(Rotation);
        var r2 = Pos + new Vector2(
            Math.Max(p4.X, Math.Max(p3.X, Math.Max(p1.X, p2.X))),
            Math.Max(p4.Y, Math.Max(p3.Y, Math.Max(p1.Y, p2.Y)))
        ) + SubtextureOffset.Rotate(Rotation);

        if (OutlineColor != default) {
            r1 -= new Vector2(1);
            r2 -= new Vector2(1);

            return RectangleExt.FromPoints(r1.ToPoint(), r2.ToPoint()).AddSize(2, 2);
        }

        return RectangleExt.FromPoints(r1.ToPoint(), r2.ToPoint());
    }

    private void CacheFields() {
        ClipRect ??= Texture.ClipRect;
        //if (Width == 0) {
        if (!Prepared) {
            Prepared = true;

            if (Width == 0)
                LoadSizeFromTexture();

            // Fixup properties now, at this point nothing should try to get stuff from the sprite...
            Flip = SpriteEffects.None;
            _multOrigin = (Origin * new Vector2(Width, Height)) + DrawOffset;
            // Monogame doesn't like negative scales...
            if (Scale.X < 0) {
                Scale.X = -Scale.X;
                Flip ^= SpriteEffects.FlipHorizontally;
                _multOrigin.X = ClipRect!.Value.Width - _multOrigin.X;
            }
            if (Scale.Y < 0) {
                Scale.Y = -Scale.Y;
                Flip ^= SpriteEffects.FlipVertically;
                _multOrigin.Y = ClipRect!.Value.Height - _multOrigin.Y;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Render(Texture2D texture, Vector2 pos, Color color, Vector2 scale, SpriteEffects flip, Vector2 origin) {
        GFX.Batch.Draw(texture, pos, ClipRect, color, Rotation, origin, scale, flip, 0f);
    }

    public Sprite Centered() {
        return this with {
            Origin = new(.5f, .5f)
        };
    }

    public Sprite MovedBy(Vector2 offset) {
        return this with {
            Pos = Pos + offset
        };
    }

    public Sprite MovedBy(float x, float y) {
        return this with {
            Pos = Pos + new Vector2(x, y),
        };
    }

    public Sprite CreateSubtexture(Rectangle subtext) => CreateSubtexture(subtext.X, subtext.Y, subtext.Width, subtext.Height);

    public Sprite CreateSubtexture(int x, int y, int w, int h) {
        var clip = Texture.GetSubtextureRect(x, y, w, h, out var offset, ClipRect);
        return this with {
            ClipRect = clip,
            DrawOffset = new Vector2(-Math.Min(x - DrawOffset.X, 0f), -Math.Min(y - DrawOffset.Y, 0f)),
            Width = w,
            Height = h,
            Pos = Pos,
            SubtextureOffset = offset,
        };
    }

    /*
    public Rectangle GetSubtextureRect(int x, int y, int w, int h) {
        var clipRectPos = ClipRect?.Location ?? Texture.ClipRectPos;

        var newY = clipRectPos.Y + y;
        var newX = clipRectPos.X + x;

        if (Texture is VanillaTexture) {
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
    }*/

    public Rectangle GetSubtextureRect(int x, int y, int w, int h) => Texture.GetSubtextureRect(x, y, w, h, out _, ClipRect);

    public IEnumerator<ISprite> GetEnumerator() => this.ToSelfEnumerator<ISprite>();

    IEnumerator IEnumerable.GetEnumerator() => this.ToSelfEnumerator();
}
