﻿using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.Linq;

namespace Rysy.Helpers;

/// <summary>
/// Represents a string->object dictionary-like type.
/// Implementing this provides many utility extension methods to retrieve data of various types from such a collection.
/// </summary>
public interface IUntypedData {
    public bool TryGetValue(string key, [NotNullWhen(true)] out object? value);
}

public readonly struct DictionaryUntypedData(Dictionary<string, object> dict) : IUntypedData {
    public bool TryGetValue(string key, [NotNullWhen(true)] out object? value) => dict.TryGetValue(key, out value);

    public Dictionary<string, object> BackingDictionary => dict;
}

public readonly struct XElementUntypedData(XElement element) : IUntypedData {
    public bool TryGetValue(string key, [NotNullWhen(true)] out object? value) {
        if (element.Attribute(key) is { } attr) {
            value = attr.Value;
            return true;
        }

        value = null;
        return false;
    }
}

public readonly struct XmlNodeUntypedData(XmlNode node) : IUntypedData {
    public bool TryGetValue(string key, [NotNullWhen(true)] out object? value) {
        value = null;
        if (node.Attributes is not { } attrs)
            return false;
        
        if (attrs[key] is { } attr) {
            value = attr.Value;
            return true;
        }

        return false;
    }
}


public static class UntypedDataExt {
    public static string Attr(this IUntypedData self, string attrName, string def = "") {
        if (self.TryGetValue(attrName, out var obj) && obj is { }) {
            return obj.ToString()!;
        }

        return def;
    }
    
    public static int Int(this IUntypedData self, string attrName, int def = 0) {
        if (self.TryGetValue(attrName, out var obj)) {
            return obj switch {
                float f => (int)f,
                int i => i,
                short s => s,
                byte b => b,
                _ => int.TryParse(obj?.ToString() ?? "", CultureInfo.InvariantCulture, out var i) ? i : def
            };
        }

        return def;
    }

    public static float Float(this IUntypedData self, string attrName, float def = 0f) {
        if (self.TryGetValue(attrName, out var obj)) {
            return obj switch {
                float f => f,
                int i => i,
                short s => s,
                byte b => b,
                _ => float.TryParse(obj?.ToString() ?? "", CultureInfo.InvariantCulture, out var f) ? f : def
            };
        }

        return def;
    }

    public static bool Bool(this IUntypedData self, string attrName, bool def = false) {
        if (self.TryGetValue(attrName, out var obj))
            return Convert.ToBoolean(obj, CultureInfo.InvariantCulture);

        return def;
    }
    
    public static bool? NullableBool(this IUntypedData self, string attrName) {
        if (self.TryGetValue(attrName, out var obj))
            return Convert.ToBoolean(obj, CultureInfo.InvariantCulture);

        return null;
    }

    public static char Char(this IUntypedData self, string attrName, char def) {
        if (self.TryGetValue(attrName, out var obj) && char.TryParse(obj.ToString(), out var result))
            return result;

        return def;
    }

    public static T Enum<T>(this IUntypedData self, string attrName, T def) where T : struct, Enum {
        if (self.TryGetValue(attrName, out var obj) && System.Enum.TryParse<T>(obj.ToString(), true, out var result))
            return result;

        return def;
    }
    
    public static T? Obj<T>(this IUntypedData self, string attrName, T? def = null) where T : class {
        if (self.TryGetValue(attrName, out var obj) && obj is T result)
            return result;

        return def;
    }

    public static Color RGB(this IUntypedData self, string attrName, Color def)
        => self.GetColor(attrName, def, ColorFormat.RGB);

    public static Color RGB(this IUntypedData self, string attrName, string def)
        => self.GetColor(attrName, def, ColorFormat.RGB);

    public static Color RGBA(this IUntypedData self, string attrName, Color def)
        => self.GetColor(attrName, def, ColorFormat.RGBA);

    public static Color RGBA(this IUntypedData self, string attrName, string def)
        => self.GetColor(attrName, def, ColorFormat.RGBA);

    public static Color ARGB(this IUntypedData self, string attrName, Color def)
        => self.GetColor(attrName, def, ColorFormat.ARGB);

    public static Color ARGB(this IUntypedData self, string attrName, string def)
        => self.GetColor(attrName, def, ColorFormat.ARGB);
    
    public static Color GetColor(this IUntypedData self, string attrName, Color def, ColorFormat format) {
        if (self.TryGetValue(attrName, out var obj) && ColorHelper.TryGet(obj.ToString()!, format, out var parsed))
            return parsed;

        return def;
    }

    public static Color GetColor(this IUntypedData self, string attrName, string def, ColorFormat format) {
        if (self.TryGetValue(attrName, out var obj) && ColorHelper.TryGet(obj.ToString()!, format, out var parsed))
            return parsed;

        if (ColorHelper.TryGet(def, format, out var defParsed)) {
            return defParsed;
        }

        return Color.White;
    }

    public static bool Has(this IUntypedData self, string attrName) => self.TryGetValue(attrName, out _);
}