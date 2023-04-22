using ImGuiNET;

namespace Rysy.Gui.FieldTypes;

static class EditableDropdownHelper {
    public static Dictionary<Type, object> DefaultStringToT = new() {
        [typeof(string)] = (Func<string, string>)((string s) => s),
        [typeof(object)] = (Func<string, object>) ((string s) => s),
    };
}

public record class EditableDropdownField<T> : IField
    where T : notnull 
{
    public string Tooltip { get; set; }

    public Dictionary<T, string> Values;

    public Func<string, T> StringToT;

    public T Default { get; set; }

    public EditableDropdownField() {
        var obj = EditableDropdownHelper.DefaultStringToT[typeof(T)];

        StringToT = (Func<string, T>)obj;
    }

    public object GetDefault() => Default!;
    public void SetDefault(object newDefault)
        => Default = (T) newDefault;

    public bool IsValid(object value) {
        if (value is not T val) {
            return false;
        }

        return true;
    }

    public object? RenderGui(string fieldName, object value) {
        if (value is not T val) {
            return null;
        }

        var prevVal = val;

        return ImGuiManager.EditableCombo(fieldName, ref val, Values, StringToT, Tooltip) ? val : null;
    }

    public IField CreateClone() => this with { };
}
