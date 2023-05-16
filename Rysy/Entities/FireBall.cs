using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("fireBall")]
public sealed class FireBall : SpriteEntity, IPlaceable {
    public override string TexturePath => Bool("notCoreMode", false) ? "objects/fireball/fireball09" : "objects/fireball/fireball01";

    public override int Depth => 0;

    public override Range NodeLimits => 0..;

    public static FieldList GetFields() => new(new {
        amount = 3,
        offset = 0.0,
        speed = 1.0,
        notCoreMode = false
    });

    public static PlacementList GetPlacements() => new() {
        new("fireball"),
        new("iceball", new {
            notCoreMode = true
        })
    };
}
