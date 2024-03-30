using Rysy.Extensions;

namespace Rysy.Helpers;

public readonly struct Fade {
    public ReadOnlyArray<Region> Regions { get; }

    public Fade(string fadeString) {
        var regionStrings = fadeString.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var regions = new Region[regionStrings.Length];
        var i = 0;
        foreach (var regionStr in regionStrings) {
            regions[i++] = Region.Parse(regionStr);
        }

        Regions = new(regions);
    }

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

    public record struct Region(float FromPos, float ToPos, float FromAlpha, float ToAlpha) {
        public NumVector2 PositionRange() => new(FromPos, ToPos);

        public NumVector2 AlphaRange() => new(FromAlpha, ToAlpha);

        public float GetValueAt(float pos) => pos.ClampedMap(FromPos, ToPos, FromAlpha, ToAlpha);

        /// <summary>
        /// Returns a correctly formatted string to represent this region in map data.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return $"{FormatNumber(FromPos)}-{FormatNumber(ToPos)},{FormatNumber(FromAlpha)}-{FormatNumber(ToAlpha)}";
        }

        private string FormatNumber(float n) => n.ToString(CultureInfo.InvariantCulture).Replace('-', 'n');

        public static Region Parse(ReadOnlySpan<char> regionSpan) {
            // Format: start-end,startAlpha-endAlpha
            
            // same as region span, but with n replaced with - so that the numbers can be properly parsed
            var parseableRegionSpan = Interpolator.Shared.Clone(regionSpan);
            parseableRegionSpan.Replace('n', '-');

            var dashPos = regionSpan.IndexOf('-');
            var commaPos = regionSpan.IndexOf(',');
            if (dashPos == -1)
                return default;
            if (commaPos == -1)
                return default;

            float fromPos = parseableRegionSpan[..dashPos].ToSingle();
            float toPos = parseableRegionSpan[(dashPos + 1)..commaPos].ToSingle();

            // Move past the comma to parse alpha
            regionSpan = regionSpan[(commaPos + 1)..];
            parseableRegionSpan = parseableRegionSpan[(commaPos + 1)..];
            
            dashPos = regionSpan.IndexOf('-');
            if (dashPos == -1)
                return default;

            float fromAlpha = parseableRegionSpan[0..dashPos].ToSingle();
            float toAlpha = parseableRegionSpan[(dashPos + 1)..].ToSingle();

            return new(fromPos, toPos, fromAlpha, toAlpha);
        }
    }
}
