using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("movingPlatform")]
[CustomEntity("sinkingPlatform")]
public sealed class Platform : Entity, IPlaceable {
    public override int Depth => 1;

    public override Range NodeLimits => Name == "movingPlatform" ? 1..1 : 0..0;

    public override bool ResizableX => true;

    public override IEnumerable<ISprite> GetSprites() {
        var texture = $"objects/woodPlatform/{Attr("texture", "default")}";

        if (Name == "movingPlatform") {
            if (Nodes is not [var node, ..])
                return Array.Empty<ISprite>();


            return GetPlatformSprites(this, Pos, texture).Concat(GetLineSprites(this, Pos, node));
        }

        return GetPlatformSprites(this, Pos, texture).Concat(GetLineSprites(this, Pos, new Vector2(X, Room.Height - 2)));
    }

    public override IEnumerable<ISprite> GetNodePathSprites() => NodePathTypes.None;

    public static IEnumerable<ISprite> GetPlatformSprites(Entity entity, Vector2 pos, string texturePath) {
        var w = entity.Width;
        var spr = ISprite.FromTexture(pos, texturePath);
        var middleSpr = spr.CreateSubtexture(8, 0, 8, 8);

        for (int x = 8; x < w - 8; x += 8) {
            yield return middleSpr.MovedBy(x, 0);
        }

        yield return spr.CreateSubtexture(0, 0, 8, 8);
        yield return spr.CreateSubtexture(24, 0, 8, 8).MovedBy(w - 8, 0);
        yield return spr.CreateSubtexture(16, 0, 8, 8).MovedBy((w - 8) / 2, 0);
    }

    public static IEnumerable<ISprite> GetLineSprites(Entity entity, Vector2 from, Vector2 to) {
        var w = entity.Width;
        var centerFrom = from.Add(w / 2, 4);
        var centerTo = to.Add(w / 2, 4);

        var angle = (centerTo - centerFrom).Normalized();
        var perpendicular = new Vector2(-angle.Y, angle.X);

        var line = ISprite.LineFloored(centerFrom, centerTo, "2a1923".FromRGB()) with {
            Depth = 9001,
        };

        yield return line.MovedBy(-angle - perpendicular);
        yield return line.MovedBy(-angle);
        yield return line.MovedBy(-angle + perpendicular);
        yield return line with {
            Color = "160b12".FromRGB(),
        };
    }

    public static FieldList GetFields() => new(new {
        texture = Fields.AtlasPath("default", "^objects/woodPlatform/(.*)")
    });

    public static PlacementList GetPlacements() => new() {
        new("default"),
        new("cliffside", new {
            texture = "cliffside",
        }),
    };
}