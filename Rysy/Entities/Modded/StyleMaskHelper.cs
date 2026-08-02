using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.LuaSupport;
using Rysy.Stylegrounds;

namespace Rysy.Entities.Modded;

// temporary interop for stylemasks

[CustomEntity("StyleMaskHelper/StylegroundMask", associatedMods: ["StyleMaskHelper"])]
[CustomEntity("StyleMaskHelper/AllInOneMask", associatedMods: ["StyleMaskHelper"])]
internal sealed class StyleMask : LonnEntity, IStyleMask {
    public string Tag => Attr("tag", null!) ?? Attr("styleTag", null!);

    public override IEnumerable<ISprite> GetSprites() {
        if (!Settings.Instance.StylegroundPreview) {
            return base.GetSprites();
        }

        return [];
    }
    
    public bool RenderMasked(Style style, StylegroundRenderCtx ctx) {
        if (!style.HasTag($"mask_{Tag}"))
            return false;

        var old = Gfx.EndBatch();
        if (old is null) {
            Gfx.BeginBatch(old);
            return false;
        }

        var camera = ctx.Camera;
        float scale = camera.Scale;
        var worldPosScissor = new Rectangle(X + Room.X, Y + Room.Y, Width, Height);
        var screenPos = camera.RealToScreen(worldPosScissor.Location.ToVector2()).ToPoint();
        var newState = old.Value with {
            ScissorRect = new(screenPos.X, screenPos.Y, (int) (worldPosScissor.Width * scale), (int) (worldPosScissor.Height * scale)),
            RasterizerState = StylegroundRenderer.CullNoneWithScissor
        };
        
        Gfx.BeginBatch(newState);
        StylegroundRenderer.RenderUnmasked(style, ctx);
        Gfx.EndBatch();
        
        
        Gfx.BeginBatch(old);
        return true;
    }
}

[CustomEntity("SJ2021/StylegroundMask", associatedMods: ["StrawberryJam2021"])]
[CustomEntity("SJ2021/AllInOneMask", associatedMods: ["StrawberryJam2021"])]
internal sealed class SjAllInOneStyleMask : LonnEntity {
    public string Tag => Attr("tag", null!) ?? Attr("stylemaskTag");

    public override int Depth => Bool("behindFg") ? Depths.BGTerrain + 1 : Depths.Above;

    public override IEnumerable<ISprite> GetSprites() {
        if (!Settings.Instance.StylegroundPreview) {
            return base.GetSprites();
        }

        return [];
    }
}

internal sealed class StyleMaskHelperMaskManager : IStyleMaskManager {
    public bool IsMasked(Style style, StylegroundRenderCtx ctx) {
        return style.Tags.Any(t => t.StartsWith("mask_", StringComparison.Ordinal));
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