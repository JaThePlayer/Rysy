using Rysy.Gui.FieldTypes;
using Rysy.Helpers;

namespace Rysy;

public static class Fields {
    public static BoolField Bool(bool def) => new() { Default = def };
    public static FloatField Float(float def) => new() { Default = def };
    public static IntField Int(int def) => new() { Default = def };
    public static CharField Char(char def) => new() { Default = def };
    public static StringField String(string def) => new() { Default = def };

    public static DropdownField<string> EnumNamesDropdown<T>(T def) where T : struct, Enum
    => new DropdownField<string>() {
        Default = def.ToString(),
    }.SetValues(Enum.GetNames<T>().ToDictionary(k => k, v => v));

    public static DropdownField<string> EnumNamesDropdown<T>(string def) where T : struct, Enum
    => new DropdownField<string>() {
        Default = def,
    }.SetValues(Enum.GetNames<T>().ToDictionary(k => k, v => v));

    public static DropdownField<T> Dropdown<T>(T def, Dictionary<T, string> values, bool editable = true) where T : notnull
    => new DropdownField<T>() {
        Default = def,
        Editable = editable,
    }.SetValues(values);

    public static DropdownField<T> Dropdown<T>(T def, Func<Dictionary<T, string>> values, bool editable = true) where T : notnull
    => new DropdownField<T>() {
        Default = def,
        Editable = editable,
    }.SetValues(values);

    public static DropdownField<string> Dropdown(string def, Func<List<string>> values, bool editable = true) => new DropdownField<string>() {
        Default = def,
        Editable = editable,
    }.SetValues(() => values().ToDictionary(k => k, k => k));

    public static DropdownField<string> Dropdown(string def, List<string> values, bool editable = true) => new DropdownField<string>() {
        Default = def,
        Editable = editable,
    }.SetValues(values.ToDictionary(k => k, k => k));

    public static DropdownField<char> TileDropdown(char def, bool bg) => new DropdownField<char>() {
        Default = def,
    }.SetValues(() => {
        if (EditorState.Map is not { } map) {
            return new();
        }

        var autotiler = bg ? map.BGAutotiler : map.FGAutotiler;

        return autotiler.Tilesets.ToDictionary(t => t.Key, t => autotiler.GetTilesetDisplayName(t.Key));
    });

    public static ColorField RGBA(Color def) => new() { 
        Default = def,
        Format = ColorFormat.RGBA,
    };

    public static ColorField RGB(Color def) => new() {
        Default = def,
        Format = ColorFormat.RGB,
    };

    public static ColorField ARGB(Color def) => new() {
        Default = def,
        Format = ColorFormat.ARGB,
    };

    public static Field? GuessFromValue(object val) => val switch {
        bool b => Bool(b),
        float b => Float(b),
        double d => Float((float)d),
        int i => Float(i), // floats that represent integer values get saved as integers to map data, so we can't return Int(i)
        long l => Int((int)l),
        char c => Char(c),
        string s => String(s),
        _ => null,
    };
}
