using Rysy.Gui.FieldTypes;
using Rysy.Helpers;

namespace Rysy;

public static class Fields {
    public static BoolField Bool(bool def) => new() { Default = def };
    public static FloatField Float(float def) => new() { Default = def };
    public static IntField Int(int def) => new() { Default = def };
    public static CharField Char(char def) => new() { Default = def };
    public static StringField String(string def) => new() { Default = def };
    public static DropdownField<T> Dropdown<T>(T def, Dictionary<T, string> values) where T : notnull
    => new() { 
        Default = def,
        Values = values
    };

    public static DropdownField<string> EnumNamesDropdown<T>(T def) where T : struct, Enum
    => new() {
        Default = def.ToString(),
        Values = Enum.GetNames<T>().ToDictionary(k => k, v => v)
    };

    public static DropdownField<string> Dropdown(string def, List<string> values) => new() {
        Default = def, 
        Values = values.ToDictionary(k => k, k => k) 
    };

    public static EditableDropdownField<string> EditableDropdown(string def, List<string> values) => new() {
        Default = def,
        Values = values.ToDictionary(v => v, v => v)
    };

    public static EditableDropdownField<T> EditableDropdown<T>(T def, Dictionary<string, T> dict)
        where T : IEquatable<T>
    => new() {
        Default = def,
        Values = dict
    };

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

    public static IField? GuessFromValue(object val) => val switch {
        bool b => Bool(b),
        float b => Float(b),
        double d => Float((float)d),
        int i => Int(i),
        long l => Int((int)l),
        char c => Char(c),
        string s => String(s),
        _ => null,
    };
}
