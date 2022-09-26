namespace Rysy;

public static class ColorHelper
{
    static Dictionary<string, Color> cache = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
    static Dictionary<string, Color[]> colorArrayCache = new Dictionary<string, Color[]>(StringComparer.OrdinalIgnoreCase);
    static ColorHelper()
    {
        foreach (var prop in typeof(Color).GetProperties())
        {
            if (prop.GetValue(default(Color), null) is Color color)
                cache[prop.Name] = color;
        }
        cache[""] = Color.White;
        colorArrayCache[""] = null!;
    }


    /// <summary>
    /// Gets a <see cref="Color"/> from the <paramref name="color"/> string, by either using an XNA color name, or converting a hex code from a given format
    /// </summary>
    public static Color Get(string color, ColorFormat format = ColorFormat.RGBA)
    {
        if (cache.TryGetValue(color, out var xnaColor)) return xnaColor;

        return format switch
        {
            ColorFormat.RGB => RGB(color),
            ColorFormat.RGBA => RGBA(color),
            ColorFormat.ARGB => ARGB(color),
            _ => throw new NotImplementedException($"Unknown color format {format}"),
        };
    }

    /// <summary>
    /// Parses a <see cref="Color"/> from the <paramref name="hexCode"/>, encoded as RRGGBB
    /// Doesn't handle XNA Color names, use <see cref="ColorHelperExtensions.ToColor(string, ColorFormat)"/> with <see cref="ColorFormat.RGB"/> as the second parameter if this is needed
    /// </summary>
    public static Color RGB(ReadOnlySpan<char> hexCode)
    {
        hexCode = PrepareSpan(hexCode);
        var packedValue = GetPacked(hexCode);
        return hexCode.Length switch
        {
            // allow 7-length as RGB because of Temple of Zoom from SC having 00bc000 as spinner tint... why
            6 or 7 => new Color((byte)(packedValue >> 16), (byte)(packedValue >> 8), (byte)packedValue), //rgb
            _ => default,
        };
    }

    /// <summary>
    /// Parses a <see cref="Color"/> from the <paramref name="hexCode"/>, encoded as AARRGGBB
    /// Doesn't handle XNA Color names, use <see cref="ColorHelperExtensions.ToColor(string, ColorFormat)"/> with <see cref="ColorFormat.ARGB"/> as the second parameter if this is needed
    /// </summary>
    public static Color ARGB(ReadOnlySpan<char> hex)
    {
        hex = PrepareSpan(hex);
        var packedValue = GetPacked(hex);
        return hex.Length switch
        {
            6 => new Color((byte)(packedValue >> 16), (byte)(packedValue >> 8), (byte)packedValue), //rgb
            8 => new Color((byte)(packedValue >> 16), (byte)(packedValue >> 8), (byte)(packedValue), (byte)(packedValue >> 24)), // argb
            _ => default,
        };
    }

    /// <summary>
    /// Parses a <see cref="Color"/> from the <paramref name="hexCode"/>, encoded as RRGGBBAA
    /// Doesn't handle XNA Color names, use <see cref="ColorHelperExtensions.ToColor(string, ColorFormat)"/> with <see cref="ColorFormat.RGBA"/> as the second parameter if this is needed
    /// </summary>
    public static Color RGBA(ReadOnlySpan<char> hex)
    {
        hex = PrepareSpan(hex);
        var packedValue = GetPacked(hex);
        return hex.Length switch
        {
            // allow 7-length as RGB because of Temple of Zoom from SC having 00bc000 as spinner tint... why
            6 or 7 => new Color((byte)(packedValue >> 16), (byte)(packedValue >> 8), (byte)packedValue), //rgb
            8 => new Color((byte)(packedValue >> 24), (byte)(packedValue >> 16), (byte)(packedValue >> 8), (byte)packedValue), // rgba
            _ => default,
        };
    }

    private static uint GetPacked(ReadOnlySpan<char> s)
        => uint.Parse(s, System.Globalization.NumberStyles.HexNumber);

    private static ReadOnlySpan<char> PrepareSpan(ReadOnlySpan<char> s)
    {
        s = s.Trim();
        if (s.StartsWith("#"))
        {
            s = s[1..];
        }

        return s;
    }
}

public enum ColorFormat
{
    RGB,
    RGBA,
    ARGB,
}

public static class ColorHelperExtensions
{
    /// <summary>
    /// <inheritdoc cref="ColorHelper.RGB(ReadOnlySpan{char})"/>
    /// </summary>
    public static Color FromRGB(this string hexCode) => ColorHelper.RGB(hexCode);

    /// <summary>
    /// <inheritdoc cref="ColorHelper.RGBA(ReadOnlySpan{char})"/>
    /// </summary>
    public static Color FromRGBA(this string hexCode) => ColorHelper.RGBA(hexCode);

    /// <summary>
    /// <inheritdoc cref="ColorHelper.ARGB(ReadOnlySpan{char})"/>
    /// </summary>
    public static Color FromARGB(this string hexCode) => ColorHelper.ARGB(hexCode);

    /// <summary>
    /// <inheritdoc cref="ColorHelper.RGB(ReadOnlySpan{char})"/>
    /// </summary>
    public static Color FromRGB(this ReadOnlySpan<char> hexCode) => ColorHelper.RGB(hexCode);

    /// <summary>
    /// <inheritdoc cref="ColorHelper.RGBA(ReadOnlySpan{char})"/>
    /// </summary>
    public static Color FromRGBA(this ReadOnlySpan<char> hexCode) => ColorHelper.RGBA(hexCode);

    /// <summary>
    /// <inheritdoc cref="ColorHelper.ARGB(ReadOnlySpan{char})"/>
    /// </summary>
    public static Color FromARGB(this ReadOnlySpan<char> hexCode) => ColorHelper.ARGB(hexCode);

    /// <summary>
    /// <inheritdoc cref="ColorHelper.Get(string, ColorFormat)"/>
    /// </summary>
    public static Color ToColor(this string str, ColorFormat format = ColorFormat.RGBA) => ColorHelper.Get(str, format);
}
