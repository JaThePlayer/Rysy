using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("cliffflag")]
public class CliffFlags : Entity, ICustomNodeHandler {
    public static Color LineColor = Color.Lerp(Color.Gray, Color.DarkBlue, 0.25f);
    public static Color[] Colors = new Color[]
    {
        "d85f2f".FromRGB(),
        "d82f63".FromRGB(),
        "2fd8a2".FromRGB(),
        "d8d62f".FromRGB(),
    };

    public static Color[] HighlightColors;

    static CliffFlags() {
        HighlightColors = new Color[Colors.Length];
        for (int i = 0; i < Colors.Length; i++) {
            HighlightColors[i] = Color.Lerp(Colors[i], Color.White, 0.3f);
        }
    }

    public override int Depth => 8999;

    public IEnumerable<ISprite> GetNodeSprites() => NodePathTypes.None;

    // TODO: Move to helper
    public override IEnumerable<ISprite> GetSprites() {
        var (p1, p2) = (Pos, Nodes![0]);
        var (start, end) = p1.X < p2.X ? (p1, p2) : (p2, p1);

        var droopAmount = 0.2f;

        var len = (start - end).Length();

        var wireCurve = new SimpleCurve() {
            Start = start,
            End = end,
            Control = (end + start) / 2 + new Vector2(0, len / 8f * 1.5f)
        };

        foreach (var item in wireCurve.GetSprites(LineColor, 16)) {
            yield return item;
        }

        float p = 0f;
        var pos = start;
        bool skip = true;
        while (p < 1f) {
            var cloth = NextCloth(10, 10, 10, 10, 2, 8);
            p += (skip ? cloth.Step : cloth.Length) / len;
            p = Math.Min(p, 1f);
            var nextPos = wireCurve.GetPointAt(p);

            if (!skip && p < 1f) {
                var color = Colors[cloth.Color];
                var clothCurve = new SimpleCurve() {
                    Start = pos,
                    End = nextPos,
                    Control = (pos + nextPos) / 2 + new Vector2(0f, cloth.Length * droopAmount * 2.4f)
                };

                foreach (var item in clothCurve.GetSpritesForFloatyRectangle(new((int) pos.X, (int) pos.Y, cloth.Length, cloth.Height), color)) {
                    yield return item;
                }

                // highlights
                var highlightColor = HighlightColors[cloth.Color];
                yield return ISprite.Rect(pos, 1, cloth.Height, highlightColor);
                yield return ISprite.Rect(nextPos, 1, cloth.Height, highlightColor);

                // pins
                yield return ISprite.Rect(pos.AddY(-1), 1, 3, Color.Gray);
                yield return ISprite.Rect(nextPos.AddY(-1), 1, 3, Color.Gray);
            }

            skip = !skip;
            pos = nextPos;
        }
    }

    private Cloth NextCloth(int minFlagHeight, int maxFlagHeight, int minFlagLength, int maxFlagLength, int minSpace, int maxSpace) => new Cloth {
        Color = Room.Random.Next(Colors.Length),
        Height = Room.Random.Next(minFlagHeight, maxFlagHeight),
        Length = Room.Random.Next(minFlagLength, maxFlagLength),
        Step = Room.Random.Next(minSpace, maxSpace)
    };

    private struct Cloth {
        public int Color;

        public int Height;

        public int Length;

        public int Step;
    }
}
