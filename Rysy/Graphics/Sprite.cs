﻿using Rysy.Extensions;
using Rysy.Graphics.TextureTypes;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Rysy.Graphics;

public record struct Sprite : ISprite, IEnumerable<ISprite> {
    public int? Depth { get; set; }

    public Vector2 Pos;

    public VirtTexture Texture;
    public Rectangle? ClipRect;

    public Color Color { get; set; } = Color.White;
    public void MultiplyAlphaBy(float alpha) {
        Color *= alpha;
        OutlineColor *= alpha;
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

            if (cam is { }) {
                var size = new Vector2(Width * scale.X, Height * scale.Y);
                var pos = Pos - origin * scale;
                //ISprite.HollowRect(pos, (int)size.X, (int)size.Y, Color.Transparent, Color.Red).Render();
                if (!cam.IsRectVisible(pos + offset, (int) size.X, (int) size.Y))
                    return;
            }

            var flip = Flip;
            if (OutlineColor != default) {
                var color = OutlineColor;

                Render(texture, Pos + new Vector2(-1f, 0f), color, scale, flip, origin);
                Render(texture, Pos + new Vector2(1f, 0f), color, scale, flip, origin);
                Render(texture, Pos + new Vector2(0f, 1f), color, scale, flip, origin);
                Render(texture, Pos + new Vector2(0f, -1f), color, scale, flip, origin);
            }
            Render(texture, Pos, Color, scale, flip, origin);
        }
    }

    public Rectangle? GetRenderRect() {
        if (Texture.Texture is not { } texture) {
            return null;
        }

        CacheFields();

        var scale = Scale;
        var size = new Vector2(ClipRect!.Value.Width * scale.X, ClipRect.Value.Height * scale.Y);
        Vector2 pos;
        if (Rotation == 0f) {
            pos = Pos - _multOrigin * scale;
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
        );
        var r2 = Pos + new Vector2(
            Math.Max(p4.X, Math.Max(p3.X, Math.Max(p1.X, p2.X))),
            Math.Max(p4.Y, Math.Max(p3.Y, Math.Max(p1.Y, p2.Y)))
        );

        if (OutlineColor != default) {
            r1 -= new Vector2(1);
            r2 -= new Vector2(1);

            return RectangleExt.FromPoints(r1.ToPoint(), r2.ToPoint()).AddSize(2, 2);
        }

        return RectangleExt.FromPoints(r1.ToPoint(), r2.ToPoint());
    }

    private void CacheFields() {
        ClipRect ??= Texture.ClipRect;
        if (Width == 0) {
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

    public void Render() {
        Render(null, default);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Render(Texture2D texture, Vector2 pos, Color color, Vector2 scale, SpriteEffects flip, Vector2 origin) {
        GFX.Batch.Draw(texture, pos, ClipRect, color, Rotation, origin, scale, flip, 0f);
    }

    public Sprite Centered() {
        Origin = new(.5f, .5f);
        return this;
    }

    public Sprite Offset(Vector2 offset) {
        Pos += offset;
        return this;
    }

    public Sprite CreateSubtexture(int x, int y, int w, int h) {
        return this with {
            ClipRect = GetSubtextureRect(x, y, w, h),
            DrawOffset = new Vector2(-Math.Min(x - DrawOffset.X, 0f), -Math.Min(y - DrawOffset.Y, 0f)),
            Width = w,
            Height = h
        };
    }

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
    }

    public IEnumerator<ISprite> GetEnumerator() => new SingleSpriteEnumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => new SingleSpriteEnumerator(this);

    private record struct SingleSpriteEnumerator(Sprite Sprite) : IEnumerator<ISprite> {
        private bool _moved = false;
    
        public ISprite Current => Sprite;

        object IEnumerator.Current => Sprite;

        public void Dispose() {
        }

        public bool MoveNext() {
            if (!_moved) {
                _moved = true;
                return true;
            }
            return false;
        }

        public void Reset() {
        }
    }
}
