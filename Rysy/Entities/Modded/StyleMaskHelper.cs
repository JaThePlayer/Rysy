using Rysy.Extensions;
using Rysy.Graphics;
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

        if (Settings.Instance is { StylegroundPreview: false })
            return null;

        return new FunctionSprite<Entity>(e, (self, spr) => {
            var lastState = GFX.EndBatch();

            StylegroundRenderer.Render(self.Room, self.Room.Map.Style, EditorState.Camera, StylegroundRenderer.Layers.BGAndFG, StylegroundRenderer.WithTag(tag!), 
                scissorRectWorldPos: self.Rectangle.MovedBy(self.Room.Pos));

            GFX.BeginBatch(lastState);
        });
    }
}