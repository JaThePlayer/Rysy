using ImGuiNET;
using Rysy.Extensions;

namespace Rysy.Helpers;

public static class ColorHelper {
    static Dictionary<string, Color> cache = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
    static Dictionary<string, Color[]> colorArrayCache = new Dictionary<string, Color[]>(StringComparer.OrdinalIgnoreCase);
    static ColorHelper() {
        foreach (var prop in typeof(Color).GetProperties())
            if (prop.GetValue(default(Color), null) is Color color)
                cache[prop.Name] = color;
        cache[""] = Color.White;
        colorArrayCache[""] = null!;
    }


    /// <summary>
    /// Gets a <see cref="Color"/> from the <paramref name="color"/> string, by either using an XNA color name, or converting a hex code from a given format
    /// </summary>
    public static Color Get(string color, ColorFormat format = ColorFormat.RGBA, bool allowXNA = true) {
        if (allowXNA && cache.TryGetValue(color, out var xnaColor))
            return xnaColor;

        return format switch {
            ColorFormat.RGB => RGB(color),
            ColorFormat.RGBA => RGBA(color),
            ColorFormat.ARGB => ARGB(color),
            _ => throw new NotImplementedException($"Unknown color format {format}"),
        };
    }

    /// <summary>
    /// Tries to convert a string to a color using the given format.
    /// </summary>
    public static bool TryGet(string colorString, ColorFormat format, out Color color, bool allowXNA = true) {
        try {
            color = Get(colorString, format, allowXNA);
            return true;
        } catch {
            color = default;
            return false;
        }
    }

    /// <summary>
    /// Parses a <see cref="Color"/> from the <paramref name="hexCode"/>, encoded as RRGGBB
    /// Doesn't handle XNA Color names, use <see cref="ColorHelperExtensions.ToColor(string, ColorFormat)"/> with <see cref="ColorFormat.RGB"/> as the second parameter if this is needed
    /// </summary>
    public static Color RGB(ReadOnlySpan<char> hexCode) {
        if (hexCode.IsEmpty)
            return Color.White;

        hexCode = PrepareSpan(hexCode);
        var packedValue = GetPacked(hexCode);
        return hexCode.Length switch {
            // allow 7-length as RGB because of Temple of Zoom from SC having 00bc000 as spinner tint... why
            6 or 7 => new Color((byte) (packedValue >> 16), (byte) (packedValue >> 8), (byte) packedValue), //rgb
            _ => default,
        };
    }

    /// <summary>
    /// Parses a <see cref="Color"/> from the <paramref name="hexCode"/>, encoded as AARRGGBB.
    /// Doesn't handle XNA Color names, use <see cref="ColorHelperExtensions.ToColor(string, ColorFormat)"/> with <see cref="ColorFormat.ARGB"/> as the second parameter if this is needed
    /// </summary>
    public static Color ARGB(ReadOnlySpan<char> hexCode) {
        hexCode = PrepareSpan(hexCode);
        var packedValue = GetPacked(hexCode);
        return hexCode.Length switch {
            6 => new Color((byte) (packedValue >> 16), (byte) (packedValue >> 8), (byte) packedValue), //rgb
            8 => new Color((byte) (packedValue >> 16), (byte) (packedValue >> 8), (byte) packedValue, (byte) (packedValue >> 24)), // argb
            _ => default,
        };
    }

    /// <summary>
    /// Parses a <see cref="Color"/> from the <paramref name="hexCode"/>, encoded as RRGGBBAA
    /// Doesn't handle XNA Color names, use <see cref="ColorHelperExtensions.ToColor(string, ColorFormat)"/> with <see cref="ColorFormat.RGBA"/> as the second parameter if this is needed
    /// </summary>
    public static Color RGBA(ReadOnlySpan<char> hexCode) {
        hexCode = PrepareSpan(hexCode);
        var packedValue = GetPacked(hexCode);
        return hexCode.Length switch {
            // allow 7-length as RGB because of Temple of Zoom from SC having 00bc000 as spinner tint... why
            6 or 7 => new Color((byte) (packedValue >> 16), (byte) (packedValue >> 8), (byte) packedValue), //rgb
            8 => new Color((byte) (packedValue >> 24), (byte) (packedValue >> 16), (byte) (packedValue >> 8), (byte) packedValue), // rgba
            _ => default,
        };
    }

    /*
// https://web.archive.org/web/20190422181017/http://chilliant.blogspot.com/2014/04/rgbhsv-in-hlsl-5.html
public static Color HsvToColor(float h, float s, float v) {
    float R = MathF.Abs(h * 6 - 3) - 1;
    float G = 2 - MathF.Abs(h * 6 - 2);
    float B = 2 - MathF.Abs(h * 6 - 4);
    return new Color((Saturate(new Vector3(R, G, B)) - Vector3.One) * s + Vector3.One) * v;
}*/

    //https://www.splinter.com.au/converting-hsv-to-rgb-colour-using-c/
    /// <summary>
    /// 
    /// </summary>
    /// <param name="h">The hue value (0-360)</param>
    /// <param name="s">The saturation value (0-1)</param>
    /// <param name="v">The v value (0-1)</param>
    /// <returns></returns>
    public static Color HSVToColor(float h, float s, float v) {
        float H = h;
        while (H < 0)
            H += 360;

        while (H >= 360)
            H -= 360;

        float R, G, B;
        if (v <= 0)
            R = G = B = 0;
        else if (s <= 0)
            R = G = B = v;
        else {
            float hf = H / 60.0f;
            int i = (int) Math.Floor(hf);
            float f = hf - i;
            float pv = v * (1 - s);
            float qv = v * (1 - s * f);
            float tv = v * (1 - s * (1 - f));
            switch (i) {
                // Red is the dominant color
                case 0:
                    R = v;
                    G = tv;
                    B = pv;
                    break;

                // Green is the dominant color
                case 1:
                    R = qv;
                    G = v;
                    B = pv;
                    break;
                case 2:
                    R = pv;
                    G = v;
                    B = tv;
                    break;

                // Blue is the dominant color
                case 3:
                    R = pv;
                    G = qv;
                    B = v;
                    break;
                case 4:
                    R = tv;
                    G = pv;
                    B = v;
                    break;

                // Red is the dominant color
                case 5:
                    R = v;
                    G = pv;
                    B = qv;
                    break;

                // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.
                case 6:
                    R = v;
                    G = tv;
                    B = pv;
                    break;
                case -1:
                    R = v;
                    G = pv;
                    B = qv;
                    break;

                // The color is not defined, we should throw an error.
                default:
                    R = G = B = v; // Just pretend its black/white
                    break;
            }
        }

        return new Color(R, G, B);
    }

    /// <summary>
    /// Gets the color of rainbow spinners in a given location in the room.
    /// </summary>
    public static Color GetRainbowColor(Room room, Vector2 pos) {
        if (room.GetOverridenRainbowColor(pos, 0f) is { } overridenRainbowColor)
            return overridenRainbowColor;
        
        float num = 280f;
        float value = ((room.Pos + pos).Length()) % num / num;
        return HSVToColor((0.4f + Easing.YoYo(value) * 0.4f) * 360f, 0.4f, 0.9f);
    }
    
    /// <summary>
    /// Gets the color of rainbow spinners in a given location in the room.
    /// </summary>
    public static Color GetRainbowColorAnimated(Room room, Vector2 pos) {
        var time = Time.Elapsed;
        if (room.GetOverridenRainbowColor(pos, time) is { } overridenRainbowColor)
            return overridenRainbowColor;
        
        float num = 280f;
        float value = ((room.Pos + pos).Length() + time * 50f) % num / num;
        return HSVToColor((0.4f + Easing.YoYo(value) * 0.4f) * 360f, 0.4f, 0.9f);
    }

    private static uint GetPacked(ReadOnlySpan<char> s)
        => uint.Parse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    private static ReadOnlySpan<char> PrepareSpan(ReadOnlySpan<char> s) {
        s = s.Trim();
        if (s.StartsWith("#"))
            s = s[1..];

        return s;
    }

    /// <summary>
    /// Turns the given color into an rgba-formatted color hexcode. If color.A == 255, returns an rgb string instead.
    /// </summary>
    /// <param name="color"></param>
    /// <returns></returns>
    public static string ToRGBAString(Color color) {
        if (color.A == 255)
            return $"{color.R:x2}{color.G:x2}{color.B:x2}";

        return $"{color.R:x2}{color.G:x2}{color.B:x2}{color.A:x2}";
    }

    /// <summary>
    /// Turns the given color into an argb-formatted color hexcode. If color.A == 255, returns an rgb string instead.
    /// </summary>
    /// <param name="color"></param>
    /// <returns></returns>
    public static string ToARGBString(Color color) {
        if (color.A == 255)
            return $"{color.R:x2}{color.G:x2}{color.B:x2}";

        return $"{color.A:x2}{color.R:x2}{color.G:x2}{color.B:x2}";
    }

    /// <summary>
    /// Converts a color into its hex representation, using the provided format
    /// </summary>
    public static string ToString(Color color, ColorFormat format) {
        return format switch {
            ColorFormat.RGBA => ToRGBAString(color),
            ColorFormat.RGB => ToRGBAString(color),
            ColorFormat.ARGB => ToARGBString(color),
            _ => throw new NotImplementedException(),
        };
    }
}

public enum ColorFormat {
    RGB,
    RGBA,
    ARGB,
}

public static class ColorHelperExtensions {
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
    /// <inheritdoc cref="ColorHelper.Get"/>
    /// </summary>
    public static Color ToColor(this string str, ColorFormat format = ColorFormat.RGBA) => ColorHelper.Get(str, format);

    /// <summary>
    /// <inheritdoc cref="ColorHelper.TryGet"/>
    /// </summary>
    public static bool TryToColor(this string colorString, ColorFormat format, out Color color) => ColorHelper.TryGet(colorString, format, out color);

    /// <summary>
    /// <inheritdoc cref="ColorHelper.ToRGBAString(Color)"/>
    /// </summary>
    public static string ToRGBAString(this Color color) => ColorHelper.ToRGBAString(color);

    /// <summary>
    /// <inheritdoc cref="ColorHelper.ToString(Color, ColorFormat)"/>
    /// </summary>
    public static string ToString(this Color color, ColorFormat format) => ColorHelper.ToString(color, format);

    /// <summary>
    /// Adds an HSV value to this color, returning a new color.
    /// </summary>
    /// <param name="c">The color to add to</param>
    /// <param name="h">The hue value (0-180f)</param>
    /// <param name="s">The saturation value (0, 100f)</param>
    /// <param name="v">The value value (0, 100f)</param>
    /// <returns></returns>
    public static Color AddHSV(this Color c, float h, float s, float v) {
        var cv = c.ToNumVec4();

        ImGui.ColorConvertRGBtoHSV(cv.X, cv.Y, cv.Z, out var oh, out var os, out var ov);
        ImGui.ColorConvertHSVtoRGB(oh + h.Div(180f), os + s.Div(100f), ov + v.Div(100f), out cv.X, out cv.Y, out cv.Z);

        return new(cv.ToXna());
    }
}

internal readonly struct RgbaOrXnaColor : ISpanParsable<RgbaOrXnaColor> {
    public Color Color { get; init; }
    
    public static RgbaOrXnaColor Parse(string s, IFormatProvider? provider) 
        => Parse(s.AsSpan(), provider);

    public static bool TryParse(string? s, IFormatProvider? provider, out RgbaOrXnaColor result) =>
        TryParse(s.AsSpan(), provider, out result);

    public static RgbaOrXnaColor Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        if (!TryParse(s, provider, out var parsed))
        {
            throw new Exception($"Invalid RGBA color: {s}");
        }

        return parsed;
    }

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out RgbaOrXnaColor result) {
        if (!ColorHelper.TryGet(s.ToString(), ColorFormat.RGBA, out var color)) {
            result = default;
            return false;
        }
        
        result = new() { Color = color };
        return true;
    }
}
