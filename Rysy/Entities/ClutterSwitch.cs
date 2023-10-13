using Rysy.Extensions;
using Rysy.Graphics;
using ClutterColors = Rysy.Helpers.CelesteEnums.ClutterColors;

namespace Rysy.Entities;

[CustomEntity("colorSwitch")]
public class ClutterSwitch : Entity, IPlaceable {
    public override int Depth => 0;

    public override IEnumerable<ISprite> GetSprites() {
        yield return ISprite.FromTexture(Pos + new Vector2(16, 16), "objects/resortclutter/clutter_button00") with {
            Origin = new(0.5f, 1.0f)
        };
        yield return ISprite.FromTexture(Pos + new Vector2(16, 8), $"objects/resortclutter/icon_{Attr("type", "green").ToLowerInvariant()}").Centered();
    }

    public static FieldList GetFields() => new(new {
        type = ClutterColors.Green
    });

    public static PlacementList GetPlacements() => System.Enum.GetNames<ClutterColors>()
        .Select(variant => new Placement(variant.ToLowerInvariant(), new {
            type = variant
        }))
        .ToPlacementList();
}
