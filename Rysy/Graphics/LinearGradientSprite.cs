using Rysy.Helpers;
using Rysy.Selections;

namespace Rysy.Graphics;

public record LinearGradientSprite : ISprite {
    public Rectangle Bounds { get; }
    
    public LinearGradient Gradient { get; }
    
    public LinearGradient.Directions Direction { get; }

    public LinearGradientSprite(Rectangle rect, LinearGradient gradient, LinearGradient.Directions dir) {
        Bounds = rect;
        Gradient = gradient;
        Direction = dir;
    }

    private PolygonSprite? _polygonSprite;
    
    public int? Depth { get; set; }
    public Color Color { get; set; }

    private float Alpha = 1f;

    public ISprite WithMultipliedAlpha(float alpha) => this with { Alpha = Alpha * alpha, _polygonSprite = null };

    public bool IsLoaded => true;
    
    public void Render(SpriteRenderCtx ctx) {
        var bounds = Bounds;
        
        if (ctx.Camera is {} cam && !cam.IsRectVisible(bounds))
            return;

        _polygonSprite ??= new(Gradient.GetVertexes(Direction, bounds));
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

    public VertexPositionColor[] GetVertexes(Directions dir, Rectangle bounds) {
        var ret = new VertexPositionColor[Entries.Count * 6];
        var span = ret.AsSpan();
        
        float yUnit = bounds.Height / 100f;
        float xUnit = bounds.Width / 100f;
        
        var start = 0f;
        foreach (var entry in Entries) {
            var c1 = entry.ColorFrom;
            var c2 = entry.ColorTo;

            var end = start + entry.Percent;
            
            var (x1, x2, y1, y2) = dir switch {
                Directions.Vertical => (bounds.Left, bounds.Right, start * yUnit, end * yUnit),
                Directions.Horizontal => (start * xUnit, end * xUnit, bounds.Top, bounds.Bottom),
                _ => (0f, 0f, 0f, 0f)
            };
            
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
            }

            start = end;
            span = span[6..];
        }

        return ret;
    }
    
    public static LinearGradient Parse(string s, IFormatProvider? provider) 
        => Parse(s.AsSpan(), provider);

    public static bool TryParse(string? s, IFormatProvider? provider, out LinearGradient result) =>
        TryParse(s.AsSpan(), provider, out result);

    public static LinearGradient Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
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