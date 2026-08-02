using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.LuaSupport;
using Rysy.Mods;
using Rysy.Shared.Collections;
using Rysy.Stylegrounds;

namespace Rysy.Entities.Modded;

// temporary interop for stylemasks

[CustomEntity("StyleMaskHelper/StylegroundMask", associatedMods: ["StyleMaskHelper"])]
[CustomEntity("StyleMaskHelper/AllInOneMask", associatedMods: ["StyleMaskHelper"])]
internal sealed class StyleMask : LonnEntity {
    public enum FadeType {
        None,
        LeftToRight,
        RightToLeft,
        TopToBottom,
        BottomToTop,
        Custom
    }

    public FadeType Fade => Enum("fade", FadeType.None);

    public float AlphaFrom => Float("alphaFrom");
    public float AlphaTo => Float("alphaTo");
    
    public string Tag => Attr("tag", Attr("styleTag"));

    public string FullTag { get; private set; }

    public string FadeMask => $"fademasks/{Attr("customFade")}";

    public override void OnChanged(EntityDataChangeCtx changed) {
        base.OnChanged(changed);

        FullTag = $"mask_{Tag}";
    }

    public override IEnumerable<ISprite> GetSprites() {
        if (!Settings.Instance.StylegroundPreview) {
            return base.GetSprites();
        }

        return [];
    }
    
    public PooledList<MaskSlice> GetMaskSlices(Vector2 drawPos, float scale) {
        var slices = new PooledList<MaskSlice>();
        var visibleRect = new Rectangle((int)drawPos.X, (int)drawPos.Y, (int) (Width * scale),
            (int) (Height * scale));
        
        var offset = Vector2.Zero;
        var source = Rectangle.Empty;
        switch (Fade) {
            case FadeType.None:
            case FadeType.Custom:
                slices.Add(new MaskSlice(drawPos, visibleRect));
                break;
            case FadeType.LeftToRight:
            case FadeType.RightToLeft:
                //offset = drawPos;
                source = visibleRect;
                for (var x = (int)offset.X; x < Width; x++) {
                    if (x - (int)offset.X < source.Width) {
                        slices.Add(new MaskSlice(
                            drawPos + new Vector2(x * scale, offset.Y),
                            new Rectangle((int)(source.X + (x - offset.X) * scale), source.Y, (int)scale, source.Height),
                            Fade == FadeType.LeftToRight ? x / (float)Width : 1 - x / (float)Width
                        ));
                    }
                }
                break;
            case FadeType.TopToBottom:
            case FadeType.BottomToTop:
                //offset = drawPos;
                source = visibleRect;
                for (var y = (int)offset.Y; y < Height; y++) {
                    if (y - (int)offset.Y < source.Height) {
                        slices.Add(new MaskSlice(
                            drawPos + new Vector2(offset.X, y * scale),
                            new Rectangle(source.X, (int)(source.Y + (y - offset.Y) * scale), source.Width, (int)scale),
                            Fade == FadeType.TopToBottom ? y / (float)Height : 1 - y / (float)Height
                        ));
                    }
                }
                break;
        }
        return slices;
    }


    public struct MaskSlice(Vector2 position, Rectangle source, float val = 1f) {
        public Vector2 Position = position;
        public Rectangle Source = source;

        public float GetValue(float from, float to) {
            return float.Lerp(from, to, val); // was Calc.LerpClamp
        }
    }
}

[CustomEntity("SJ2021/StylegroundMask", associatedMods: ["StrawberryJam2021"])]
[CustomEntity("SJ2021/AllInOneMask", associatedMods: ["StrawberryJam2021"])]
internal sealed class SjAllInOneStyleMask : LonnEntity {
    public string Tag => Attr("tag", Attr("stylemaskTag"));

    public override int Depth => Bool("behindFg") ? Depths.BGTerrain + 1 : Depths.Above;

    public override IEnumerable<ISprite> GetSprites() {
        if (!Settings.Instance.StylegroundPreview) {
            return base.GetSprites();
        }

        return [];
    }
}

internal sealed class StyleMaskHelperMaskManager : IStyleMaskManager {
    private Effect? MaskEffect;
    private bool _maskEffectNotFound;
    
    public bool IsMasked(Style style, StylegroundRenderCtx ctx) {
        var masks = ctx.Room.Entities.OfType<StyleMask>();
        if (!style.Tags.Any(t => t.StartsWith("mask_", StringComparison.Ordinal)))
            return false;

        var old = Gfx.EndBatch();
        if (old is null) {
            Gfx.BeginBatch(old);
            return false;
        }

        using var buffer = RenderTargetPool.Get(1920, 1080);
        using var maskBuffer = RenderTargetPool.Get(1920, 1080);
        var gd = Gfx.Batch.GraphicsDevice;

        var oldTargets = gd.GetRenderTargets();
        gd.SetRenderTarget(buffer.Target);
        gd.Clear(Color.Transparent);
        
        Gfx.BeginBatch(old);
        StylegroundRenderer.RenderUnmasked(style, ctx);
        Gfx.EndBatch();

        gd.SetRenderTargets(oldTargets);
        Gfx.BeginBatch();
        
        var camera = ctx.Camera;
        float scale = camera.Scale;
        foreach (var mask in masks) {
            if (!style.HasTag(mask.FullTag))
                continue;
            
            var worldPosScissor = new Rectangle(mask.X + ctx.Room.X, mask.Y + ctx.Room.Y, mask.Width, mask.Height);
            var screenPos = camera.RealToScreen(worldPosScissor.Location.ToVector2()).ToPoint();

            if (mask.Fade is StyleMask.FadeType.Custom && Gfx.Atlas.TryGet(mask.FadeMask, out var fadeMask)) {
                Gfx.EndBatch();
                gd.SetRenderTarget(maskBuffer.Target);
                gd.Clear(Color.Transparent);
                Gfx.BeginBatch();
                
                (ISprite.FromTexture(screenPos.ToVector2(), fadeMask) with {
                        Scale = new Vector2(worldPosScissor.Width / (float)fadeMask.Width * (scale), (float)worldPosScissor.Height / fadeMask.Height * (scale))
                    })
                    .RenderWithColor(SpriteRenderCtx.Default(), Color.White);
                Gfx.EndBatch();
                gd.SetRenderTargets(oldTargets);
                gd.Textures[1] = buffer.Target;
                if (MaskEffect is null && !_maskEffectNotFound) {
                    if (ModRegistry.Filesystem.TryReadAllBytes("Effects/StyleMaskHelper/Mask.cso") is not { } bytes) {
                        Logger.Write("StyleMaskHelper", LogLevel.Error, $"Failed to find fade mask shader");
                        _maskEffectNotFound = true;
                    } else {
                        MaskEffect = new Effect(gd, bytes);
                    }
                }

                if (MaskEffect is not null) {
                    Gfx.Batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, MaskEffect, Matrix.Identity);
                    Gfx.Batch.Draw(maskBuffer.Target, new Rectangle(0, 0, 1920, 1080), null, Color.White);
                    Gfx.Batch.End();
                }
                gd.SetRenderTargets(oldTargets);
                Gfx.BeginBatch();
                continue;
            }
            
            using var slices = mask.GetMaskSlices(screenPos.ToVector2(), scale);
            foreach (var slice in slices) {
                Gfx.Batch.Draw(buffer.Target, slice.Position, slice.Source, Color.White * slice.GetValue(mask.AlphaFrom, mask.AlphaTo));
            }
        }
        Gfx.EndBatch();
        Gfx.BeginBatch(old);
        
        return true;
    }
}

/*
static class StyleMaskHelper {
    public static ISprite? GetSprite(string? tag, Entity e) {
        if (tag.IsNullOrWhitespace())
            return null;

        return new FunctionSprite<Entity>(e, (spr, ctx, self) => {
            if (Settings.Instance is { StylegroundPreview: false })
                return;
            
            if (ctx.Camera?.IsRectVisible(self.Rectangle.MovedBy(ctx.CameraOffset)) ?? true) {
                var cam = ctx.Camera ?? EditorState.Current?.Camera;
                if (cam is null) {
                    return;
                }

                var lastState = Gfx.EndBatch();
                
                StylegroundRenderer.Render(self.Room, self.Room.Map.Style, cam, StylegroundRenderer.Layers.BgAndFg, s => s.HasTag(tag!), 
                    scissorRectWorldPos: self.Rectangle.MovedBy(self.Room.Pos));

                Gfx.BeginBatch(lastState);
            }
        });
    }
}
*/