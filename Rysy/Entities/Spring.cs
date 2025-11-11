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

    public Orientations Orientation => EntityData.Sid switch {
        "spring" => Orientations.Floor,
        "wallSpringLeft" => Orientations.WallLeft,
        "wallSpringRight" => Orientations.WallRight,
        var other => throw new NotImplementedException($"Unknown spring entity {other}")
    };

    public override Entity? TryFlipHorizontal() => Orientation switch {
        Orientations.Floor => null,
        Orientations.WallLeft => CloneWith(placement => placement.Sid = "wallSpringRight"),
        Orientations.WallRight => CloneWith(placement => placement.Sid = "wallSpringLeft"),
        _ => throw new NotImplementedException()
    };

    public override Entity? TryRotate(RotationDirection dir) => CloneWith(pl => pl.WithSid(dir.AddRotationTo(Orientation) switch {
        Orientations.Floor => "spring",
        Orientations.WallLeft => "wallSpringLeft",
        Orientations.WallRight => "wallSpringRight",
        var v => throw new NotImplementedException(v.ToString()),
    }));

    public enum Orientations {
        Floor,
        WallLeft,
        WallRight
    }

    public static FieldList GetFields() => new(new {
        playerCanUse = true
    });

    public static PlacementList GetPlacements() => new() {
        new Placement("up").WithSid("spring"),
        new Placement("right").WithSid("wallSpringLeft"),
        new Placement("left").WithSid("wallSpringRight"),
    };
}
