using System.Text.Unicode;

namespace Rysy.Helpers;

public readonly struct Fade {
    public ReadOnlyArray<Region> Regions { get; }

    public Fade(ReadOnlyArray<Region> regions) {
        Regions = regions;
    }

    public float GetValueAt(float pos) {
        float value = 1f;
        foreach (var r in Regions) {
            value *= r.GetValueAt(pos);
        }
        return value;
    }

    public override string ToString() {
        return string.Join(':', Regions.Select(r => r.ToString()));
    }

    public record struct Region(float FromPos, float ToPos, float FromAlpha, float ToAlpha) : ISimpleParsable<Region> {
        public NumVector2 PositionRange() => new(FromPos, ToPos);

        public NumVector2 AlphaRange() => new(FromAlpha, ToAlpha);

        public float GetValueAt(float pos) => pos.ClampedMap(FromPos, ToPos, FromAlpha, ToAlpha);

        /// <summary>
        /// Returns a correctly formatted string to represent this region in map data.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return string.Create(CultureInfo.InvariantCulture, $"{new FadeFloat(FromPos)}-{new FadeFloat(ToPos)},{new FadeFloat(FromAlpha)}-{new FadeFloat(ToAlpha)}");
        }

        public static bool TryParse(ReadOnlySpan<char> regionSpan, IFormatProvider? provider, out Region result) {
            // Format: start-end,startAlpha-endAlpha
            var parser = new SpanParser(regionSpan);

            if (!parser.ReadUntil<FadeFloat>('-').TryUnpack(out var start)
                || !parser.ReadUntil<FadeFloat>(',').TryUnpack(out var end)
                || !parser.ReadUntil<FadeFloat>('-').TryUnpack(out var startAlpha)
                || !parser.Read<FadeFloat>().TryUnpack(out var endAlpha)
               ) {
                result = default;
                return false;
            }

            result = new(start.Value, end.Value, startAlpha.Value, endAlpha.Value);
            return true; 
        }
    }

    /// <summary>
    /// A wrapper over a <see cref="float"/> that parses 'n' as a negative sign indicator instead of '-'.
    /// </summary>
    private struct FadeFloat(float value) : ISimpleParsable<FadeFloat>, ISpanFormattable {
        public float Value = value;
        
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out FadeFloat result) {
            bool isNegative = false;
            if (s.StartsWith('n')) {
                s = s[1..];
                isNegative = true;
            }

            if (s.StartsWith('-')) {
                result = default;
                return false;
            }
            
            var isValid = float.TryParse(s, provider, out result.Value);
            if (isNegative) {
                result.Value = -result.Value;
            }
            return isValid;
        }

        public override string ToString() {
            return ToString(null, CultureInfo.InvariantCulture);
        }

        public string ToString(string? format, IFormatProvider? formatProvider) {
            return Value.ToString(format, formatProvider).Replace('-', 'n');
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) {
            var isValid = Value.TryFormat(destination, out charsWritten, format, provider);
            if (isValid && charsWritten > 0 && destination[0] == '-') {
                destination[0] = 'n';
            }
            return isValid;
        }
    }
}
