using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("templeMirror")]
public sealed class TempleMirror : NineSliceEntity, IPlaceable {
    public override int Depth => 8995;

    public override string TexturePath => "scenery/templemirror";

    public override IEnumerable<ISprite> GetSprites() {
        return base.GetSprites().Prepend(ISprite.Rect(Rectangle.MovedBy(2, 2).AddSize(-4, -4), new(5, 7, 14)));
    }

    public static FieldList GetFields() => new(new {
        reflectionX = 0.0f,
        reflectionY = 0.0f
    });

    public static PlacementList GetPlacements() => new("mirror");
}