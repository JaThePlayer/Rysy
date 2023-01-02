using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("cobweb")]
public class Cobweb : Entity, ICustomNodeHandler
{
    public override int Depth => -1;

    public override IEnumerable<ISprite> GetSprites()
    {
        var nodes = Nodes!;
        var colors = Attr("color", "696a6a").Split(',').Select(x => x.FromRGB()).ToArray();

        return GetCobwebSprites(Pos, nodes[0], 12, true);

        IEnumerable<ISprite> GetCobwebSprites(Vector2 a, Vector2 b, int steps, bool offshoots)
        {
            var curve = new SimpleCurve(a, b, (a + b) / 2f + new Vector2(0f, 8f));

            if (offshoots)
            {
                for (int i = 1; i < nodes.Length; i++)
                {
                    foreach (var s in GetCobwebSprites(nodes[i], curve.GetPointAt((float)Room.Random.NextDouble() * .4f).Rounded(), 4, false))
                    {
                        yield return s;
                    }
                }
            }

            foreach (var item in curve.GetSprites(colors[0], steps))
            {
                yield return item;
            }
        }
    }

    public IEnumerable<ISprite> GetNodeSprites()
    {
        yield break;
    }
}
