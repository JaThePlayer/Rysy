using System.Diagnostics.CodeAnalysis;
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
    extension(IUntypedData self)
    {
        public string? AttrNullable(string attrName) {
            if (self.TryGetValue(attrName, out var obj) && obj is { }) {
                return obj.ToString();
            }

            return null;
        }

        public string Attr(string attrName, string def = "") {
            if (self.TryGetValue(attrName, out var obj) && obj is { }) {
                return obj.ToString()!;
            }

            return def;
        }

        public int Int(string attrName, int def = 0) {
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

        public float Float(string attrName, float def = 0f) {
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

        public bool Bool(string attrName, bool def = false) {
            if (self.TryGetValue(attrName, out var obj))
                return Convert.ToBoolean(obj, CultureInfo.InvariantCulture);

            return def;
        }

        public bool? NullableBool(string attrName) {
            if (self.TryGetValue(attrName, out var obj))
                return Convert.ToBoolean(obj, CultureInfo.InvariantCulture);

            return null;
        }

        public char Char(string attrName, char def) {
            if (self.TryGetValue(attrName, out var obj) && char.TryParse(obj.ToString(), out var result))
                return result;

            return def;
        }

        public T Enum<T>(string attrName, T def) where T : struct, Enum {
            if (self.TryGetValue(attrName, out var obj) && System.Enum.TryParse<T>(obj.ToString(), true, out var result))
                return result;

            return def;
        }

        public T? Obj<T>(string attrName, T? def = null) where T : class {
            if (self.TryGetValue(attrName, out var obj) && obj is T result)
                return result;

            return def;
        }

        public Color Rgb(string attrName, Color def)
            => self.GetColor(attrName, def, ColorFormat.Rgb);

        public Color Rgb(string attrName, string def)
            => self.GetColor(attrName, def, ColorFormat.Rgb);

        public Color Rgba(string attrName, Color def)
            => self.GetColor(attrName, def, ColorFormat.Rgba);

        public Color Rgba(string attrName, string def)
            => self.GetColor(attrName, def, ColorFormat.Rgba);

        public Color Argb(string attrName, Color def)
            => self.GetColor(attrName, def, ColorFormat.Argb);

        public Color Argb(string attrName, string def)
            => self.GetColor(attrName, def, ColorFormat.Argb);

        public Color GetColor(string attrName, Color def, ColorFormat format) {
            if (self.TryGetValue(attrName, out var obj) && ColorHelper.TryGet(obj.ToString() ?? "", format, out var parsed))
                return parsed;

            return def;
        }

        public Color GetColor(string attrName, string def, ColorFormat format) {
            if (self.TryGetValue(attrName, out var obj) && ColorHelper.TryGet(obj.ToString() ?? "", format, out var parsed))
                return parsed;

            if (ColorHelper.TryGet(def, format, out var defParsed)) {
                return defParsed;
            }

            return Color.White;
        }

        public bool Has(string attrName) => self.TryGetValue(attrName, out _);
    }
}