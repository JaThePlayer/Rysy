using ImGuiNET;

namespace Rysy.Gui.FieldTypes;

static class EditableDropdownHelper {
    public static Dictionary<Type, object> DefaultStringToT = new() {
        [typeof(string)] = (Func<string, string>)((string s) => s),

    };
}

public class EditableDropdownField<T> : IField
    where T : IEquatable<T> {
    public Dictionary<string, T> Values;

    public EditableDropdownField() {
        var obj = EditableDropdownHelper.DefaultStringToT[typeof(T)];

        StringToT = (Func<string, T>)obj;
    }

    public Func<string, T> StringToT;

    public T Default { get; set; }

    public object GetDefault() => Default!;

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

        return ImGuiManager.EditableCombo(fieldName, ref val, Values, StringToT) ? val : null;
    }
}
