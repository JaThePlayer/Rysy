using System.Diagnostics.CodeAnalysis;

namespace Rysy.Helpers;

/// <summary>
/// Represents a string->object dictionary-like type.
/// Implementing this provides many utility extension methods to retrieve data of various types from such a collection.
/// </summary>
public interface ILooseData {
    public bool TryGetValue(string key, [NotNullWhen(true)] out object? value);
}

public static class LooseDataExt {
    public static string Attr(this ILooseData self, string attrName, string def = "") {
        if (self.TryGetValue(attrName, out var obj) && obj is { }) {
            return obj.ToString()!;
        }

        return def;
    }
    
    public static int Int(this ILooseData self, string attrName, int def = 0) {
        if (self.TryGetValue(attrName, out var obj)) {
            return obj is int i ? i : Convert.ToInt32(obj, CultureInfo.InvariantCulture);
        }

        return def;
    }

    public static float Float(this ILooseData self, string attrName, float def = 0f) {
        if (self.TryGetValue(attrName, out var obj))
            return Convert.ToSingle(obj, CultureInfo.InvariantCulture);

        return def;
    }

    public static bool Bool(this ILooseData self, string attrName, bool def = false) {
        if (self.TryGetValue(attrName, out var obj))
            return Convert.ToBoolean(obj, CultureInfo.InvariantCulture);

        return def;
    }

    public static char Char(this ILooseData self, string attrName, char def) {
        if (self.TryGetValue(attrName, out var obj) && char.TryParse(obj.ToString(), out var result))
            return result;

        return def;
    }

    public static T Enum<T>(this ILooseData self, string attrName, T def) where T : struct, Enum {
        if (self.TryGetValue(attrName, out var obj) && System.Enum.TryParse<T>(obj.ToString(), true, out var result))
            return result;

        return def;
    }

    public static Color RGB(this ILooseData self, string attrName, Color def)
        => self.GetColor(attrName, def, ColorFormat.RGB);

    public static Color RGB(this ILooseData self, string attrName, string def)
        => self.GetColor(attrName, def, ColorFormat.RGB);

    public static Color RGBA(this ILooseData self, string attrName, Color def)
        => self.GetColor(attrName, def, ColorFormat.RGBA);

    public static Color RGBA(this ILooseData self, string attrName, string def)
        => self.GetColor(attrName, def, ColorFormat.RGBA);

    public static Color ARGB(this ILooseData self, string attrName, Color def)
        => self.GetColor(attrName, def, ColorFormat.ARGB);

    public static Color ARGB(this ILooseData self, string attrName, string def)
        => self.GetColor(attrName, def, ColorFormat.ARGB);
    
    public static Color GetColor(this ILooseData self, string attrName, Color def, ColorFormat format) {
        if (self.TryGetValue(attrName, out var obj) && ColorHelper.TryGet(obj.ToString()!, format, out var parsed))
            return parsed;

        return def;
    }

    public static Color GetColor(this ILooseData self, string attrName, string def, ColorFormat format) {
        if (self.TryGetValue(attrName, out var obj) && ColorHelper.TryGet(obj.ToString()!, format, out var parsed))
            return parsed;

        if (ColorHelper.TryGet(def, format, out var defParsed)) {
            return defParsed;
        }

        return Color.White;
    }
}