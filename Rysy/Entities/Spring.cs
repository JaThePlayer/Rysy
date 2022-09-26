using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("spring")]
[CustomEntity("wallSpringLeft")]
[CustomEntity("wallSpringRight")]
public sealed class Spring : SpriteEntity
{
    public override string TexturePath => "objects/spring/00";

    public override int Depth => -8501;

    public override Vector2 Origin => new(.5f, 1f);

    public override Color OutlineColor => Color.Black;

    public override float Rotation => Orientation switch
    {
        Orientations.Floor => 0f,
        Orientations.WallLeft => MathHelper.PiOver2,
        Orientations.WallRight => -MathHelper.PiOver2,
        var other => throw new NotImplementedException($"Unknown spring orientation {other}")
    };

    public Orientations Orientation => EntityData.Name switch
    {
        "spring" => Orientations.Floor,
        "wallSpringLeft" => Orientations.WallLeft,
        "wallSpringRight" => Orientations.WallRight,
        var other => throw new NotImplementedException($"Unknown spring entity {other}")
    };

    public enum Orientations
    {
        Floor,
        WallLeft,
        WallRight
    }
}
