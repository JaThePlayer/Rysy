using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("wire")]
public class Wire : Entity, ICustomNodeHandler {
    public override int Depth => Bool("above") ? -8500 : 2000;

    public Color Color => RGB("color", "595866");

    public IEnumerable<ISprite> GetNodeSprites() {
        yield break;
    }

    public override IEnumerable<ISprite> GetSprites()
        => ISprite.GetCurveSprites(Pos, Nodes![0], new(0f, 24f), Color, 16);

    public override Range NodeLimits => 1..1;
}
