using Rysy.Helpers;
using Rysy.Selections;

namespace Rysy.Graphics;

public record LinearGradientSprite : ISprite {
    public Rectangle Bounds { get; }
    
    public LinearGradient Gradient { get; }
    
    public LinearGradient.Directions Direction { get; }

    public LinearGradientSprite(Rectangle rect, LinearGradient gradient, LinearGradient.Directions dir, 
        bool loopX = false, bool loopY = false) {
        Bounds = rect;
        Gradient = gradient;
        Direction = dir;
        LoopX = loopX;
        LoopY = loopY;
    }

    private PolygonSprite? _polygonSprite;
    
    public int? Depth { get; set; }
    public Color Color { get; set; }
    
    public bool LoopX { get; set; }
    
    public bool LoopY { get; set; }

    private float Alpha = 1f;

    public ISprite WithMultipliedAlpha(float alpha) => this with { Alpha = Alpha * alpha, _polygonSprite = null };

    public bool IsLoaded => true;
    
    public void Render(SpriteRenderCtx ctx) {
        var bounds = Bounds;
        
        if (ctx.Camera is {} cam && !cam.IsRectVisible(bounds))
            return;

        if (_polygonSprite is null) {
            VertexPositionColor[]? vertexes = null;
            Gradient.GetVertexes(ref vertexes, Direction, bounds, Vector2.Zero, LoopX, LoopY, out var amt);
            _polygonSprite = new(vertexes!);
        }
        
        _polygonSprite?.Render(ctx);
    }

    public ISelectionCollider GetCollider() => ISelectionCollider.FromRect(Bounds);
}

// From Frost Helper
public sealed class LinearGradient : ISpanParsable<LinearGradient>
{
    public static LinearGradient ErrorGradient => new() {
        Entries = [
            new() {
                Percent = 100,
                ColorFrom = Color.Red * 0.3f,
                ColorTo = Color.Red * 0.3f,
            }
        ]
    };
    
    public LinearGradient() { }
    
    public List<Entry> Entries { get; init; } = [];
    private float? entryPercentageSum = null;
    
    public void GetVertexes(ref VertexPositionColor[]? into, Directions dir, Rectangle bounds, Vector2 basePos, bool loopX, bool loopY, out int vertexCount) {
        entryPercentageSum ??= Entries.Sum(e => e.Percent);

        float yUnit = bounds.Height / 100f;
        float xUnit = bounds.Width / 100f;
        
        // Perf: Modulo the position of looping directions by the size of the gradient
        if (loopY && dir == Directions.Vertical) {
            basePos.Y %= entryPercentageSum.Value * yUnit * 2;
        } else if (loopX && dir == Directions.Horizontal) {
            basePos.X %= entryPercentageSum.Value * xUnit * 2;
        }
        
        vertexCount = 0;
        into ??= new VertexPositionColor[Entries.Count * 6];
        
        March(ref into, dir, bounds, basePos, loopX, loopY, ref vertexCount, moveInverted: false);

        if (dir == Directions.Vertical && loopY && basePos.Y > 0f) {
            // we've started moved down a bit, we need to march upwards
            March(ref into, dir, bounds, basePos, loopX, loopY, ref vertexCount, moveInverted: true);
        }
        else if (dir == Directions.Horizontal && loopY && basePos.X > 0f) {
            // we've started moved right a bit, we need to march left
            March(ref into, dir, bounds, basePos, loopX, loopY, ref vertexCount, moveInverted: true);
        }
    }

    private void March(ref VertexPositionColor[] ret, Directions dir, Rectangle bounds, Vector2 basePos, bool loopX, bool loopY,
        ref int vertexCount, bool moveInverted) {
        var span = ret.AsSpan(vertexCount);
        
        float yUnit = bounds.Height / 100f;
        float xUnit = bounds.Width / 100f;
        
        var i = 0;
        var entries = Entries;
        var inc = 1;
        var start = 0f;
        while (true) {
            var entry = entries[i];

            var end = start + entry.Percent * (moveInverted ? -1 : 1);
            
            var (x1, x2, y1, y2) = dir switch {
                Directions.Vertical => (bounds.Left, bounds.Right, start * yUnit, end * yUnit),
                Directions.Horizontal => (start * xUnit, end * xUnit, bounds.Top, bounds.Bottom),
                _ => (0f, 0f, 0f, 0f)
            };

            // No point in moving on a looping axis if the gradient is not in that direction
            if (!(loopX && dir == Directions.Vertical)) {
                x1 += basePos.X;
                x2 += basePos.X;
            }
            if (!(loopY && dir == Directions.Horizontal)) {
                y1 += basePos.Y;
                y2 += basePos.Y;
            }

            // Cull entries above or to the left of the screen
            if ((x1 >= bounds.Left || x2 >= bounds.Left) && (y1 >= bounds.Top || y2 >= bounds.Top)) {
                if (span.Length < 6) {
                    Array.Resize(ref ret, ret.Length + 6);
                    span = ret.AsSpan()[^6..];
                }

                var c1 = inc < 0 ? entry.ColorTo : entry.ColorFrom;
                var c2 = inc < 0 ? entry.ColorFrom : entry.ColorTo;
                
                // explicit bounds check to help the JIT
                if (span.Length >= 6) {
                    switch (dir) {
                        case Directions.Vertical:
                            span[0] = new VertexPositionColor(new Vector3(x1, y1, 0f), c1);
                            span[1] = new VertexPositionColor(new Vector3(x2, y1, 0f), c1);
                            span[2] = new VertexPositionColor(new Vector3(x2, y2, 0f), c2);
                            span[3] = new VertexPositionColor(new Vector3(x1, y1, 0f), c1);
                            span[4] = new VertexPositionColor(new Vector3(x2, y2, 0f), c2);
                            span[5] = new VertexPositionColor(new Vector3(x1, y2, 0f), c2);
                            break;
                        case Directions.Horizontal:
                            span[0] = new VertexPositionColor(new Vector3(x1, y1, 0f), c1);
                            span[1] = new VertexPositionColor(new Vector3(x1, y2, 0f), c1);
                            span[2] = new VertexPositionColor(new Vector3(x2, y2, 0f), c2);
                            span[3] = new VertexPositionColor(new Vector3(x1, y1, 0f), c1);
                            span[4] = new VertexPositionColor(new Vector3(x2, y2, 0f), c2);
                            span[5] = new VertexPositionColor(new Vector3(x2, y1, 0f), c2);
                            break;
                    }

                    vertexCount += 6;
                    span = span[6..];
                }
            }
            
            // Return early if we have already covered the entire screen
            if (dir == Directions.Vertical && (moveInverted ? y2 < 0f : y2 >= bounds.Bottom)) {
                break;
            }

            if (dir == Directions.Horizontal && (moveInverted ? x2 < 0f : x2 >= bounds.Right)) {
                break;
            }

            // We ran out of entries
            if (i + inc >= entries.Count || i + inc < 0) {
                // change direction if we're looping in the same direction as the gradient
                if (loopY && dir == Directions.Vertical && (moveInverted ? y2 >= 0f : y2 < bounds.Bottom)) {
                    inc *= -1;
                } else if (loopX && dir == Directions.Horizontal && (moveInverted ? x2 >= 0f : x2 < bounds.Right)) {
                    inc *= -1;
                } else
                    break;
            } else {
                i += inc;
            }

            start = end;
        }
    }
    
    public static LinearGradient Parse(string s, IFormatProvider? provider = null) 
        => Parse(s.AsSpan(), provider);

    public static bool TryParse(string? s, IFormatProvider? provider, out LinearGradient result) =>
        TryParse(s.AsSpan(), provider, out result);

    public static LinearGradient Parse(ReadOnlySpan<char> s, IFormatProvider? provider = null)
    {
        if (!TryParse(s, provider, out var parsed))
            throw new Exception("Invalid gradient");

        return parsed;
    }

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out LinearGradient result)
    {
        result = new();

        var p = new SpanParser(s);
        while (p.SliceUntil(';').TryUnpack(out var entryParser))
        {
            entryParser.TrimStart();
            if (!entryParser.ReadUntil<RgbaOrXnaColor>(',').TryUnpack(out var colorFrom))
                return false;
            entryParser.TrimStart();
            if (!entryParser.ReadUntil<RgbaOrXnaColor>(',').TryUnpack(out var colorTo))
                return false;
            entryParser.TrimStart();
            if (!entryParser.TryRead<float>(out var percent))
                return false;

            result.Entries.Add(new Entry {
                ColorFrom = colorFrom.Color,
                ColorTo = colorTo.Color,
                Percent = percent
            });
        }

        return true;
    }

    public struct Entry : ISpanParsable<Entry>
    {
        public Color ColorFrom;
        public Color ColorTo;
        public float Percent;
        
        public static Entry Parse(string s, IFormatProvider? provider) 
            => Parse(s.AsSpan(), provider);

        public static bool TryParse(string? s, IFormatProvider? provider, out Entry result) =>
            TryParse(s.AsSpan(), provider, out result);

        public static Entry Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            if (!TryParse(s, provider, out var parsed))
                throw new Exception("Invalid gradient entry");

            return parsed;
        }

        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Entry result) {
            result = default;
            var p = new SpanParser(s);
            
            p.TrimStart();
            if (!p.ReadUntil<RgbaOrXnaColor>(',').TryUnpack(out var colorFrom))
                return false;
            p.TrimStart();
            if (!p.ReadUntil<RgbaOrXnaColor>(',').TryUnpack(out var colorTo))
                return false;
            p.TrimStart();
            if (!p.TryRead<float>(out var percent))
                return false;

            result = new Entry { ColorFrom = colorFrom.Color, ColorTo = colorTo.Color, Percent = percent };
            return true;
        }

        public override string ToString() => $"{ColorFrom.ToRGBAString()},{ColorTo.ToRGBAString()},{Percent.ToString(CultureInfo.InvariantCulture)}";
    }

    public enum Directions {
        Vertical,
        Horizontal
    }
}