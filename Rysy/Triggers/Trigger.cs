using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Triggers;

public class Trigger : Entity, INodeSpriteProvider {
    public virtual Color Color => Color.LightSkyBlue;

    public override int Depth => Depths.Top;

    public IEnumerable<ISprite> GetNodeSprites(int nodeIndex) {
        var node = Nodes![nodeIndex];
        var rect = new Rectangle((int) node.X - 2, (int) node.Y - 2, 4, 4);
        yield return ISprite.OutlinedRect(rect, Color * 0.35f, Color);
    }

    public override IEnumerable<ISprite> GetSprites() {
        var rect = new Rectangle((int) Pos.X, (int) Pos.Y, Width, Height);
        yield return ISprite.OutlinedRect(rect, Color * 0.35f, Color);
        yield return new PicoTextRectSprite() {
            Text = TriggerHelpers.Humanize(EntityData.Name),
            Pos = rect,
            Color = Color.White,
            Scale = 0.5f,
        };
    }
}
