using Rysy.Extensions;
using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("wire")]
public class Wire : Entity {
    public override int Depth => Bool("above") ? -8500 : 2000;

    public Color Color => RGB("color", "595866");

    public override IEnumerable<ISprite> GetNodeSprites(int nodeIndex) => Array.Empty<ISprite>();
    public override IEnumerable<ISprite> GetNodePathSprites() => Array.Empty<ISprite>();

    public override IEnumerable<ISprite> GetSprites()
        => ISprite.GetCurveSprite(Pos, Nodes![0], new(0f, 24f), Color, 16);

    public override ISelectionCollider GetMainSelection()
        => ISelectionCollider.FromRect(Pos.Add(-2, -2), 4, 4);

    public override ISelectionCollider GetNodeSelection(int nodeIndex)
        => ISelectionCollider.FromRect(Nodes[nodeIndex].Pos.Add(-2, -2), 4, 4);

    public override Range NodeLimits => 1..1;
}
