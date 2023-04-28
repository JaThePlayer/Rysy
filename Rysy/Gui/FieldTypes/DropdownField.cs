using ImGuiNET;
using Rysy.Helpers;

namespace Rysy.Gui.FieldTypes;

static class DropdownHelper {
    public static Dictionary<Type, object> DefaultStringToT = new() {
        [typeof(string)] = (Func<string?, string>) ((string? s) => s ?? ""),
        [typeof(object)] = (Func<string?, object>) ((string? s) => s ?? ""),
    };
}

public record class DropdownField<T> : Field
    where T : notnull {
    public bool NullAllowed { get; set; }

    public Func<Dictionary<T, string>> Values;

    public Func<string?, T> StringToT;

    public bool Editable { get; set; }

    public T Default { get; set; }

    private string Search = "";

    public DropdownField() {
        var obj = DropdownHelper.DefaultStringToT[typeof(T)];

        StringToT = (Func<string?, T>) obj;
    }

    public override object GetDefault() => Default!;
    public override void SetDefault(object newDefault)
        => Default = (T) Convert.ChangeType(newDefault, typeof(T));

    public override bool IsValid(object? value) {
        if (value is not T val) {
            return (NullAllowed && value is null) && base.IsValid(value);
        }

        return (Editable || Values().TryGetValue(val, out _)) && base.IsValid(value);
    }

    public override object? RenderGui(string fieldName, object value) {
        if (value is not T val) {
            //return null;
            val = StringToT(null);
        }

        var prevVal = val;

        if (Editable) {
            return ImGuiManager.EditableCombo(fieldName, ref val, Values(), StringToT, ref Search, Tooltip) ? val : null;
        } else {
            return ImGuiManager.Combo(fieldName, ref val, Values(), ref Search, Tooltip) ? val : null;
            /*
            string? humanizedName = null;
            if (value is T val)
                Values().TryGetValue(val, out humanizedName);

            humanizedName ??= value?.ToString() ?? "";

            object? ret = null;

            if (ImGui.BeginCombo(fieldName, humanizedName).WithTooltip(Tooltip)) {
                foreach (var (key, name) in Values()) {
                    if (ImGui.MenuItem(name)) {
                        ret = key;
                    }
                }

                ImGui.EndCombo();
            }

            return ret;*/
        }
    }

    public DropdownField<T> SetValues(Cache<Dictionary<T, string>> cache) {
        Values = () => cache.Value;

        return this;
    }

    public DropdownField<T> SetValues(Dictionary<T, string> values) {
        Values = () => values;

        return this;
    }

    public DropdownField<T> SetValues(Func<Dictionary<T, string>> values) {
        Values = values;

        return this;
    }

    /// <summary>
    /// Allows the field's value to be edited beyond the values from the dropdown.
    /// </summary>
    /// <returns>this</returns>
    public DropdownField<T> AllowEdits() {
        Editable = true;

        return this;
    }

    /// <summary>
    /// Allows null to be considered a valid value for this field.
    /// </summary>
    /// <returns>this</returns>
    public DropdownField<T> AllowNull() {
        NullAllowed = true;

        return this;
    }

    public override Field CreateClone() => this with { };
}
