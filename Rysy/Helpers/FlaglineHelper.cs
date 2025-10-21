using Hexa.NET.ImGui;
using Rysy.Extensions;
using Rysy.Graphics;

namespace Rysy.Helpers;

public static class FlaglineHelper {
    public class Options {
        public Color LineColor { get; set; }

        public Color PinColor { get; set; }

        public List<Color> Colors { get; set; }

        public List<Color> HighlightColors { get; set; }

        public Range FlagHeight { get; set; }
        public Range FlagLength { get; set; }
        public Range Space { get; set; }

        public Options CreateHighlightColors(Func<Color, Color> colorTransformer) {
            HighlightColors = Colors.Select(colorTransformer).ToList();

            return this;
        }
    }

    public static IEnumerable<ISprite> GetSprites(Vector2 p1, Vector2 p2, Options options) {
        var (start, end) = p1.X < p2.X ? (p1, p2) : (p2, p1);

        var droopAmount = 0.2f;

        var len = (start - end).Length();

        var wireCurve = new SimpleCurve() {
            Start = start,
            End = end,
            Control = (end + start) / 2 + new Vector2(0, len / 8f * 1.5f)
        };

        foreach (var item in wireCurve.GetSprite(options.LineColor, 16)) {
            yield return item;
        }

        float p = 0f;
        var pos = start;
        bool skip = true;
        while (p < 1f) {
            var cloth = NextCloth(pos, options);
            p += (skip ? cloth.Step : cloth.Length) / len;
            p = Math.Min(p, 1f);
            var nextPos = wireCurve.GetPointAt(p);

            if (!skip && p < 1f) {
                var color = options.Colors[cloth.Color];
                var clothCurve = new SimpleCurve() {
                    Start = pos,
                    End = nextPos,
                    Control = (pos + nextPos) / 2 + new Vector2(0f, cloth.Length * droopAmount * 2.4f)
                };

                foreach (var item in clothCurve.GetSpritesForFloatyRectangle(new((int) pos.X, (int) pos.Y, cloth.Length, cloth.Height), color)) {
                    yield return item;
                }

                // highlights
                var highlightColor = options.HighlightColors[cloth.Color];
                yield return ISprite.Rect(pos, 1, cloth.Height, highlightColor);
                yield return ISprite.Rect(nextPos, 1, cloth.Height, highlightColor);

                // pins
                yield return ISprite.Rect(pos.AddY(-1), 1, 3, options.PinColor);
                yield return ISprite.Rect(nextPos.AddY(-1), 1, 3, options.PinColor);
            }

            skip = !skip;
            pos = nextPos;
        }
    }

    private static Cloth NextCloth(Vector2 pos, Options options) {
        var c = new Cloth {
            Color = pos.SeededRandomExclusive(options.Colors.Count),
            Height = pos.SeededRandomInclusive(options.FlagHeight),
            Length = pos.SeededRandomInclusive(options.FlagLength),
            Step = pos.SeededRandomInclusive(options.Space)
        };

        return c;
    }

    private struct Cloth {
        public int Color;

        public int Height;

        public int Length;

        public int Step;
    }
}
