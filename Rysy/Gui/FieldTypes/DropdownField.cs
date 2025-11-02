using Hexa.NET.ImGui;
using Rysy.Graphics;
using Rysy.Gui.Windows;
using Rysy.Helpers;
using System.Diagnostics.CodeAnalysis;

namespace Rysy.Gui.FieldTypes;

internal static class DropdownHelper {
    public static Dictionary<Type, object> DefaultStringToT = new() {
        [typeof(string)] = (Func<string?, string>) ((string? s) => s ?? ""),
        [typeof(char)] = (Func<string?, char>) ((string? s) => s is [char c] ? c : '\0'),
        [typeof(object)] = (Func<string?, object>) ((string? s) => s ?? ""),
        [typeof(int)] = (Func<string?, int>) ((string? s) => s is { } ? int.TryParse(s, CultureInfo.InvariantCulture, out var ret) ? ret : 0 : 0),
        [typeof(float)] = (Func<string?, float>) ((string? s) => s is { } ? float.TryParse(s, CultureInfo.InvariantCulture, out var ret) ? ret : 0 : 0),
    };
}

public record class DropdownField<T> : Field, IFieldConvertible<T>, IFieldConvertible
    where T : notnull {
    public bool NullAllowed { get; set; }
    
    public bool EmptyIsNull { get; set; }
    
    public Func<FormContext, IDictionary<T, string>> Values;

    public Func<string?, T> StringToT;

    public Func<T, Searchable, bool>? MenuItemRenderer;

    public bool Editable { get; set; }

    public T Default { get; set; }

    private string Search = "";

    public DropdownField() {
        if (DropdownHelper.DefaultStringToT.TryGetValue(typeof(T), out var obj)) {
            StringToT = (Func<string?, T>) obj;
        }
    }

    public DropdownField(Func<string?, T> stringToT) {
        StringToT = stringToT;
    }

    private IDictionary<T, string> GetValues() {
        if (typeof(T) == typeof(object)) {
            var values = Values(Context);
            if (values.Keys.All(k => k is string))
                return values;
            // the value we get might be an int, but dropdown values might be floats etc.
            // to avoid issues with finding the dropdown values in those cases, we'll compare the string representations...
            return new Dictionary<T, string>(values, new ToStringEqualityComparer());
        }

        return Values(Context);
    }

    public override object GetDefault() => Default!;
    public override void SetDefault(object newDefault)
        => Default = (T) Convert.ChangeType(newDefault, typeof(T), CultureInfo.InvariantCulture);

    private T? RealValue(T? value) {
        if (value is null || EmptyIsNull && value is "")
            return default;

        return value;
    }
    
    public override ValidationResult IsValid(object? value) {
        if (value is null) {
            return NullAllowed ? ValidationResult.Ok : ValidationResult.CantBeNull;
        }
        
        var val = ConvertMapDataValue(value);

        if (val is not { }) {
            return NullAllowed ? ValidationResult.Ok : ValidationResult.CantBeNull;
        }

        return (Editable || GetValues().TryGetValue(val, out _)) ? base.IsValid(value) : ValidationResult.InvalidDropdownElement;
    }

    public override bool IsValidType(object? value) {
        if (typeof(T) == typeof(int) && value is float) {
            return true;
        }
        if (typeof(T) == typeof(float) && value is int) {
            return true;
        }
        
        return value is T or string or null;
    }

    public override object? RenderGui(string fieldName, object value) {
        var val = ConvertMapDataValue(value);

        var prevVal = val;

        if (Editable) {
            return ImGuiManager.EditableCombo(fieldName, ref val, GetValues(), StringToT, ref Search, Tooltip,
                menuItemRenderer: MenuItemRenderer) ? RealValue(val) : null;
        } else {
            return ImGuiManager.Combo(fieldName, ref val, GetValues(), ref Search, Tooltip, 
                menuItemRenderer: MenuItemRenderer) ? RealValue(val) : null;
        }
    }

    public DropdownField<T> SetValues(Cache<IDictionary<T, string>> cache) {
        Values = _ => cache.Value;

        return this;
    }

    public DropdownField<T> SetValues(IDictionary<T, string> values) {
        Values = _ => values;

        return this;
    }

    public DropdownField<T> SetValues(Func<IDictionary<T, string>> values) {
        Values = _ => values();

        return this;
    }
    
    public DropdownField<T> SetValues(Func<FormContext, IDictionary<T, string>> values) {
        Values = values;

        return this;
    }
    
    /// <summary>
    /// Adds a sprite-based tooltip to menu entries.
    /// <param name="render">Getter for the sprite to render. (T key, string displayName) -> ISprite</param>
    /// </summary>
    public DropdownField<T> AddSpriteTooltips(Func<T, Searchable, ISprite?> render) {
        MenuItemRenderer = (key, searchable) => {
            var clicked = ImGui.MenuItem(searchable.TextWithMods);

            if (ImGui.IsItemHovered()) {
                var sprite = render(key, searchable);
                ImGuiManager.SpriteTooltip("path_field_preview", sprite);
            }
        
            return clicked;
        };

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
    
    public DropdownField<T> ConvertEmptyToNull() {
        EmptyIsNull = true;

        return this;
    }

    public override Field CreateClone() => this with { };

    public T ConvertMapDataValue(object? value) {
        if (value is not T val) {
            if (StringToT is null)
                throw new Exception($"Objects of type {typeof(T)} cannot be handled by a dropdown by default. Provide a StringToT method manually!");
            val = StringToT(value is string s ? s : null);
        }

        return RealValue(val)!;
    }

    T1 IFieldConvertible.ConvertMapDataValue<T1>(object value) {
        if (value is T1 t1)
            return t1;
        
        if (typeof(T1) == typeof(int) && value is float f) {
            return (T1)(object)(int)f;
        }
        
        if (!typeof(T1).IsEnum) {
            throw new Exception($"{typeof(DropdownField<T>)} can't convert to {typeof(T1)} as its not {typeof(T)} or an enum type");
        }

        return (T1)Enum.Parse(typeof(T1), value?.ToString() ?? "", ignoreCase: true);
    }

    private struct ToStringEqualityComparer : IEqualityComparer<T> {
        public bool Equals(T? x, T? y) {
            if (x is null) {
                return y is null;
            }
            if (y is null) {
                return x is null;
            }

            return x.ToString()?.Equals(y.ToString(), StringComparison.OrdinalIgnoreCase) ?? y.ToString() is null;
        }

        public int GetHashCode([DisallowNull] T obj) {
            return obj.ToString()?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0;
        }
    }
}

public record class DropdownField : Field {
    private string Search = "";
    
    public IDictionary<object, string> Values { get; set; }
    
    public Func<object, Searchable, bool>? MenuItemRenderer;
    
    public bool Editable { get; set; }
    
    public object Default { get; set; }
    
    public Func<string, object> ValueTransformer { get; set; } = x => x;
    public Func<object?, string> DisplayTransformer { get; set; } = x => x.ToStringInvariant();
    
    public override object GetDefault() => Default;

    public override void SetDefault(object newDefault) => Default = newDefault;

    public override object RenderGui(string fieldName, object value) {
        if (Editable) {
            return ImGuiManager.EditableCombo(fieldName, ref value, Values, ValueTransformer, ref Search, Tooltip,
                menuItemRenderer: MenuItemRenderer, tToString: DisplayTransformer) ? value : null;
        } else {
            return ImGuiManager.Combo(fieldName, ref value, Values, ref Search, Tooltip, 
                menuItemRenderer: MenuItemRenderer) ? value : null;
        }
    }

    public override Field CreateClone() => this with { };
}