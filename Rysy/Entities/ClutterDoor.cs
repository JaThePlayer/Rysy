using Rysy.Extensions;
using Rysy.Graphics;
using ClutterColors = Rysy.Helpers.CelesteEnums.ClutterColors;

namespace Rysy.Entities;

[CustomEntity("clutterDoor")]
internal sealed class ClutterDoor : Entity, IPlaceable {
    public override int Depth => 0;

    public override IEnumerable<ISprite> GetSprites() {
        yield return ISprite.FromTexture(Pos, "objects/door/ghost_door00").Centered();
        yield return ISprite.FromTexture(Pos, $"objects/resortclutter/icon_{Attr("type", "green").ToLowerInvariant()}").Centered();
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
