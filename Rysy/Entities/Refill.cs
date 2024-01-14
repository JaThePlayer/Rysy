using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("refill")]
public class Refill : SpriteEntity, IPlaceable {
    public override int Depth => -100;

    public override Color OutlineColor => Color.Black;

    public override string TexturePath =>
        Bool("twoDash", false)
            ? "objects/refillTwo/idle00"
            : "objects/refill/idle00";

    public static FieldList GetFields() => new(new {
        oneUse = false,
        twoDash = false
    });

    public static PlacementList GetPlacements() => [
        new("one_dash"),
        new("two_dashes", new { twoDash = true })
    ];

    public override bool CanTrim(string key, object val) => IsDefault(key, val);
}
