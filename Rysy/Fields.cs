using Rysy.Gui.FieldTypes;

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

    public static DropdownField<string> Dropdown(string def, List<string> values) => new() {
        Default = def, 
        Values = values.ToDictionary(k => k, k => k) 
    };

    public static EditableDropdownField<string> EditableDropdown(string def, List<string> values) => new() {
        Default = def,
        Values = values
    };

    public static IField? GuessFromValue(object val) => val switch {
        bool b => Bool(b),
        float b => Float(b),
        int i => Int(i),
        char c => Char(c),
        string s => String(s),
        _ => null,
    };
}
