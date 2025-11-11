using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.Selections;

namespace Rysy.Entities;

[CustomEntity("cobweb")]
public class Cobweb : Entity, IPlaceable {
    public override int Depth => -1;
    public override Range NodeLimits => 1..;

    public override IEnumerable<ISprite> GetSprites() {
        var nodes = Nodes!;
        var colors = Attr("color", "696a6a").Split(',').Select(x => x.FromRgb()).ToArray();

        return GetCobwebSprites(Pos, nodes[0], 12, true);

        IEnumerable<ISprite> GetCobwebSprites(Vector2 a, Vector2 b, int steps, bool offshoots) {
            var curve = new SimpleCurve(a, b, (a + b) / 2f + new Vector2(0f, 8f));

            if (offshoots) {
                for (int i = 1; i < nodes.Count; i++) {
                    foreach (var s in GetCobwebSprites(nodes[i], curve.GetPointAt(0.5f).Rounded(), 4, false)) {
                        yield return s;
                    }
                }
            }

            foreach (var item in curve.GetSprite(colors[0], steps)) {
                yield return item;
            }
        }
    }

    public override IEnumerable<ISprite> GetNodeSprites(int nodeIndex) => Array.Empty<ISprite>();
    public override IEnumerable<ISprite> GetNodePathSprites() => Array.Empty<ISprite>();

    public override ISelectionCollider GetMainSelection()
        => ISelectionCollider.FromRect(Pos.Add(-2, -2), 4, 4);

    public override ISelectionCollider GetNodeSelection(int nodeIndex)
        => ISelectionCollider.FromRect(Nodes[nodeIndex].Pos.Add(-2, -2), 4, 4);
    public static FieldList GetFields() => new(new {
        color = Fields.Rgb("696A6A")
    });

    public static PlacementList GetPlacements() => new("cobweb");
}
