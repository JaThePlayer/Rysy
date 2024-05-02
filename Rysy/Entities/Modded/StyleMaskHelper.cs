using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.LuaSupport;
using Rysy.Stylegrounds;

namespace Rysy.Entities.Modded;

// temporary interop for stylemasks

[CustomEntity("StyleMaskHelper/StylegroundMask", associatedMods: new string[] { "StyleMaskHelper" })]
[CustomEntity("StyleMaskHelper/AllInOneMask", associatedMods: new string[] { "StyleMaskHelper" })]
internal sealed class StyleMask : LonnEntity {
    public string Tag => Attr("tag", null!) ?? Attr("styleTag", null!);

    public override IEnumerable<ISprite> GetSprites() {
        var tag = Tag;

        if (tag.IsNullOrWhitespace() || StyleMaskHelper.GetSprite($"mask_{tag}", this) is not { } maskSprite)
            return base.GetSprites();

        return maskSprite;
    }
}

[CustomEntity("SJ2021/StylegroundMask", associatedMods: new string[] { "StrawberryJam2021" })]
[CustomEntity("SJ2021/AllInOneMask", associatedMods: new string[] { "StrawberryJam2021" })]
internal sealed class SJAllInOneStyleMask : LonnEntity {
    public string Tag => Attr("tag", null!) ?? Attr("stylemaskTag");

    public override int Depth => Bool("behindFg") ? Depths.BGTerrain + 1 : Depths.Above;

    public override IEnumerable<ISprite> GetSprites() {
        var tag = Tag;
        if (tag.IsNullOrWhitespace() || StyleMaskHelper.GetSprite($"sjstylemask_{tag}", this) is not { } maskSprite)
            return base.GetSprites();

        return maskSprite;
    }
}


static class StyleMaskHelper {
    public static ISprite? GetSprite(string? tag, Entity e) {
        if (tag.IsNullOrWhitespace())
            return null;

        return new FunctionSprite<Entity>(e, (spr, ctx, self) => {
            if (Settings.Instance is { StylegroundPreview: false })
                return;
            
            if (ctx.Camera?.IsRectVisible(self.Rectangle.MovedBy(ctx.CameraOffset)) ?? true) {
                var lastState = GFX.EndBatch();
                StylegroundRenderer.Render(self.Room, self.Room.Map.Style, ctx.Camera ?? EditorState.Camera, StylegroundRenderer.Layers.BGAndFG, s => s.HasTag(tag!), 
                    scissorRectWorldPos: self.Rectangle.MovedBy(self.Room.Pos));

                GFX.BeginBatch(lastState);
            }
        });
    }
}