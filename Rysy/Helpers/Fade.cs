using Rysy.Extensions;

namespace Rysy.Helpers;

public sealed class Fade {
    public List<Region> Regions { get; }

    public Fade(string fadeString) {
        var regionStrings = fadeString.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        Regions = new(regionStrings.Length);
        foreach (var regionStr in regionStrings) {
            float fromPos, toPos, fromAlpha, toAlpha;
            var regionSpan = regionStr.AsSpan();
            // same as region span, but with n replaced with - so that the numbers can be properly parsed
            var parseableRegionSpan = regionStr.Replace('n', '-').AsSpan();

            var dashPos = regionSpan.IndexOf('-');
            var commaPos = regionSpan.IndexOf(',');

            fromPos = parseableRegionSpan[0..dashPos].ToSingle();
            toPos = parseableRegionSpan[(dashPos + 1)..commaPos].ToSingle();

            regionSpan = regionSpan[(commaPos + 1)..];
            parseableRegionSpan = parseableRegionSpan[(commaPos + 1)..];
            dashPos = regionSpan.IndexOf('-');

            fromAlpha = parseableRegionSpan[0..dashPos].ToSingle();
            toAlpha = parseableRegionSpan[(dashPos + 1)..].ToSingle();

            Regions.Add(new(fromPos, toPos, fromAlpha, toAlpha));
        }
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

    public sealed record class Region(float FromPos, float ToPos, float FromAlpha, float ToAlpha) {
        public NumVector2 PositionRange() => new(FromPos, ToPos);

        public NumVector2 AlphaRange() => new(FromAlpha, ToAlpha);

        public float GetValueAt(float pos) => pos.ClampedMap(FromPos, ToPos, FromAlpha, ToAlpha);

        public override string ToString() {
            return $"{FormatNumber(FromPos)}-{FormatNumber(ToPos)},{FormatNumber(FromAlpha)}-{FormatNumber(ToAlpha)}";
        }

        private string FormatNumber(float n) => n.ToString(CultureInfo.InvariantCulture).Replace('-', 'n');
    }
}
