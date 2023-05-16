using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("spring")]
[CustomEntity("wallSpringLeft")]
[CustomEntity("wallSpringRight")]
public sealed class Spring : SpriteEntity, IPlaceable {
    public override string TexturePath => "objects/spring/00";

    public override int Depth => -8501;

    public override Vector2 Origin => new(.5f, 1f);

    public override Color OutlineColor => Color.Black;

    public override float Rotation => Orientation switch {
        Orientations.Floor => 0f,
        Orientations.WallLeft => MathHelper.PiOver2,
        Orientations.WallRight => -MathHelper.PiOver2,
        var other => throw new NotImplementedException($"Unknown spring orientation {other}")
    };

    public Orientations Orientation => EntityData.SID switch {
        "spring" => Orientations.Floor,
        "wallSpringLeft" => Orientations.WallLeft,
        "wallSpringRight" => Orientations.WallRight,
        var other => throw new NotImplementedException($"Unknown spring entity {other}")
    };

    public override Entity? TryFlipHorizontal() => Orientation switch {
        Orientations.Floor => null,
        Orientations.WallLeft => CloneWith(placement => placement.SID = "wallSpringRight"),
        Orientations.WallRight => CloneWith(placement => placement.SID = "wallSpringLeft"),
        _ => throw new NotImplementedException()
    };

    public enum Orientations {
        Floor,
        WallLeft,
        WallRight
    }

    public static FieldList GetFields() => new();
    public static PlacementList GetPlacements() => new() {
        new Placement("Spring (Up)").ForSID("spring"),
        new Placement("Spring (Left)").ForSID("wallSpringLeft"),
        new Placement("Spring (Right)").ForSID("wallSpringRight"),
    };
}
