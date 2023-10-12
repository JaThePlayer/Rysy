using ImGuiNET;
using Rysy.Helpers;
using System.Diagnostics.CodeAnalysis;

namespace Rysy.Gui.FieldTypes;

static class DropdownHelper {
    public static Dictionary<Type, object> DefaultStringToT = new() {
        [typeof(string)] = (Func<string?, string>) ((string? s) => s ?? ""),
        [typeof(char)] = (Func<string?, char>) ((string? s) => s is [char c] ? c : '\0'),
        [typeof(object)] = (Func<string?, object>) ((string? s) => s ?? ""),
        [typeof(int)] = (Func<string?, int>) ((string? s) => s is { } ? int.Parse(s) : 0),
        [typeof(float)] = (Func<string?, float>) ((string? s) => s is { } ? float.Parse(s) : 0),
    };
}

public record class DropdownField<T> : Field
    where T : notnull {
    public bool NullAllowed { get; set; }

    public Func<IDictionary<T, string>> Values;

    public Func<string?, T> StringToT;

    public bool Editable { get; set; }

    public T Default { get; set; }

    private string Search = "";

    public DropdownField() {
        var obj = DropdownHelper.DefaultStringToT[typeof(T)];

        StringToT = (Func<string?, T>) obj;
    }

    private IDictionary<T, string> GetValues() {
        if (typeof(T) == typeof(object)) {
            // the value we get might be an int, but dropdown values might be floats etc.
            // to avoid issues with finding the dropdown values in those cases, we'll compare the string representations...
            return new Dictionary<T, string>(Values(), new ToStringEqualityComparer());
        }

        return Values();
    }

    public override object GetDefault() => Default!;
    public override void SetDefault(object newDefault)
        => Default = (T) Convert.ChangeType(newDefault, typeof(T));

    public override bool IsValid(object? value) {
        if (value is not T val) {
            return (NullAllowed && value is null) && base.IsValid(value);
        }

        return (Editable || GetValues().TryGetValue(val, out _)) && base.IsValid(value);
    }

    public override object? RenderGui(string fieldName, object value) {
        if (value is not T val) {
            val = StringToT(value is string s ? s : null);
        }

        var prevVal = val;

        if (Editable) {
            return ImGuiManager.EditableCombo(fieldName, ref val, GetValues(), StringToT, ref Search, Tooltip) ? val : null;
        } else {
            return ImGuiManager.Combo(fieldName, ref val, GetValues(), ref Search, Tooltip) ? val : null;
        }
    }

    public DropdownField<T> SetValues(Cache<IDictionary<T, string>> cache) {
        Values = () => cache.Value;

        return this;
    }

    public DropdownField<T> SetValues(IDictionary<T, string> values) {
        Values = () => values;

        return this;
    }

    public DropdownField<T> SetValues(Func<IDictionary<T, string>> values) {
        Values = values;

        return this;
    }

    /// <summary>
    /// Allows or disallows the field's value to be edited beyond the values from the dropdown.
    /// </summary>
    /// <returns>this</returns>
    public DropdownField<T> AllowEdits(bool editable = true) {
        Editable = editable;

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

    private struct ToStringEqualityComparer : IEqualityComparer<T> {
        public bool Equals(T? x, T? y) {
            if (x is null) {
                return y is null;
            }
            if (y is null) {
                return x is null;
            }

            return x.ToString()?.ToLowerInvariant() == y.ToString()?.ToLowerInvariant();
        }

        public int GetHashCode([DisallowNull] T obj) {
            return obj.ToString()!.ToLowerInvariant().GetHashCode();
        }
    }
}
