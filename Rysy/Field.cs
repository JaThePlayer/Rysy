using Rysy.Extensions;
using Rysy.Gui;
using Rysy.Gui.FieldTypes;
using Rysy.Gui.Windows;
using Rysy.Helpers;

namespace Rysy;

public abstract record class Field {
    public Tooltip Tooltip { get; set; }
    public string? NameOverride { get; set; }

    public FormContext Context { get; internal set; }

    /// <summary>
    /// An arbitrary function that checks whether a value is valid. Called by <see cref="IsValid(object)"/>
    /// </summary>
    public Func<FormContext, object?, ValidationResult>? Validator { get; set; }

    /// <summary>
    /// Whether this field is hidden and should not be shown in the entity edit window
    /// </summary>
    public Func<FormContext, bool> IsHidden { get; set; } = _ => false;

    /// <summary>
    /// Gets the default value for this field
    /// </summary>
    public abstract object GetDefault();

    public virtual bool IsValidType(object? value) => true;
    
    public virtual Field GetAlternativeForInvalidFieldDefaultType(object? value) {
        return Fields.GuessFromValue(value, fromMapData: true) ?? Fields.String(value?.ToString() ?? "");
    }

    /// <summary>
    /// Sets the default value for this field
    /// </summary>
    public abstract void SetDefault(object newDefault);

    /// <summary>
    /// Renders this field using ImGui.
    /// </summary>
    /// <param name="fieldName">The name of this field, to be used for the field's label</param>
    /// <param name="value">The current value of this field</param>
    /// <returns>If the value got changed by the user, returns that new value. Otherwise, returns null</returns>
    public abstract object? RenderGui(string fieldName, object value);

    public object? RenderGui(object value) {
        return RenderGui(NameOverride ?? "##", value);
    }

    public object? RenderGuiWithValidation(string fieldName, object value, ValidationResult validationResult) {
        var prevTooltip = Tooltip;
        if (validationResult.HasErrors)
            ImGuiManager.PushInvalidStyle();
        else if (validationResult.HasWarnings)
            ImGuiManager.PushWarningStyle();
        
        try {
            Tooltip = Tooltip.WrapWithValidation(validationResult);
            return RenderGui(NameOverride ?? fieldName, value);
        } finally {
            ImGuiManager.PopInvalidStyle();
            ImGuiManager.PopWarningStyle();
            Tooltip = prevTooltip;
        }
    }

    public object? RenderGuiWithValidation(object value, out ValidationResult validationResult) {
        validationResult = IsValid(value);
        return RenderGuiWithValidation(NameOverride ??= "???", value, validationResult);
    }

    /// <summary>
    /// Checks whether <paramref name="value"/> is a valid value for this field. Make sure to call base.IsValid, as it handles the <see cref="Validator"/>
    /// </summary>
    /// <param name="value">The value to check</param>
    /// <returns>Whether this value is valid</returns>
    public virtual ValidationResult IsValid(object? value) {
        if (Validator is null)
            return ValidationResult.Ok;
        
        return Validator.Invoke(Context, value);
    }

    /// <summary>
    /// Converts the value to a string, such that it can be parsed back into the value by this field.
    /// </summary>
    public virtual string ValueToString(object? value) {
        return value?.ToString() ?? "";
    }

    /// <summary>
    /// Creates a clone of this field
    /// </summary>
    /// <returns>A clone of this field</returns>
    public abstract Field CreateClone();
}

public record struct FieldNullReturn;

/// <summary>
/// Defines how to convert a field from lonn's fieldInformation into a Rysy field
/// </summary>
public interface ILonnField {
    /// <summary>
    /// The name of this field, used for lonn interop
    /// </summary>
    public static abstract string Name { get; }

    /// <summary>
    /// Creates an instance of this field from a default value and an entry in the 'fieldInformation' table
    /// </summary>
    /// <param name="def">The default value for this field</param>
    /// <param name="fieldInfoEntry">Entry in the 'fieldInformation' table from a Lonn plugin</param>
    /// <returns>The field instance</returns>
    public static abstract Field Create(object? def, IUntypedData fieldInfoEntry);
}

public static class FieldExtensions {
    /// <summary>
    /// Adds a tooltip to this <see cref="Field"/>, and returns this instance.
    /// </summary>
    public static T WithTooltip<T>(this T field, string? tooltip) where T : Field {
        field.Tooltip = tooltip;

        return field;
    }

    /// <summary>
    /// Adds a tooltip to this <see cref="Field"/>, and returns this instance, translating it.
    /// If no translation is found, the tooltip gets set to null.
    /// </summary>
    public static T WithTooltipTranslated<T>(this T field, string? tooltip) where T : Field {
        field.Tooltip = tooltip?.TranslateOrNull();

        return field;
    }

    /// <summary>
    /// Overrides the name of this <see cref="Field"/>, and returns this instance.
    /// </summary>
    public static T WithName<T>(this T field, string? name) where T : Field {
        field.NameOverride = name;

        return field;
    }

    /// <summary>
    /// Overrides the name of this <see cref="Field"/>, and returns this instance, translating it.
    /// If no translation is found, <see cref="Field.NameOverride"/> gets set to null.
    /// </summary>
    public static T WithNameTranslated<T>(this T field, string? name) where T : Field {
        field.NameOverride = name?.TranslateOrNull();

        return field;
    }

    /// <summary>
    /// Makes this field use translation keys for its name and tooltips, using the standard .tooltip postfix.
    /// </summary>
    public static T Translated<T>(this T field, string key) where T : Field {
        field.NameOverride = key.TranslateOrNull();
        field.Tooltip = Tooltip.CreateTranslatedOrNull($"{key}.tooltip");
        
        return field;
    }

    /// <summary>
    /// Adds a validator to this field, which disallows saving the property if it returns false
    /// </summary>
    public static T WithValidator<T>(this T field, Func<object?, ValidationResult> validator) where T : Field {
        field.Validator += (_, val) => validator(val);

        return field;
    }
    
    /// <summary>
    /// Adds a validator to this field, which disallows saving the property if it returns false
    /// </summary>
    public static T WithValidator<T>(this T field, Func<FormContext, object?, ValidationResult> validator) where T : Field {
        field.Validator += validator;

        return field;
    }

    /// <summary>
    /// Makes this field hidden from entity edit windows.
    /// </summary>
    public static T MakeHidden<T>(this T field) where T : Field {
        field.IsHidden = _ => true;
        return field;
    }
    
    /// <summary>
    /// Makes this field hidden from entity edit windows based on a condition
    /// </summary>
    public static T MakeHidden<T>(this T field, Func<FormContext, bool> isHidden) where T : Field {
        field.IsHidden = isHidden;
        return field;
    }

    /// <summary>
    /// Converts this field into a <see cref="ListField"/>, where each element will be an instance of <paramref name="field"/>.
    /// </summary>
    public static ListField ToList(this Field field, char separator = ',')
        => ToList(field, separator.ToString());

    /// <summary>
    /// Converts this field into a <see cref="ListField"/>, where each element will be an instance of <paramref name="field"/>.
    /// </summary>
    public static ListField ToList(this Field field, string separator = ",") {
        return new(field) {
            Separator = separator,
        };
    }
}

public sealed class FieldList : Dictionary<string, Field> {
    public FieldList() {

    }

    public Func<object, List<string>>? Order;
    public Func<FormContext, IEnumerable<string>>? GetDynamicallyHiddenFields;

    public IEnumerable<KeyValuePair<string, Field>> OrderedEnumerable(object functionArg) {
        if (Order is null || Order?.Invoke(functionArg) is not { } order)
            return this.OrderBy(p => p.Key);

        return this.OrderBy(p => {
            var i = order.IndexOf(p.Key);

            return i == -1 ? int.MaxValue : i;
        });
    }

    /// <summary>
    /// Creates a field list by using an anonymous object of the style of { fieldName = value, field2 = value2, ...}
    /// Values can also be fields, which will be used directly.
    /// If the value is an enum, the field is automatically turned into an uneditable dropdown.
    /// </summary>
    /// <param name="lonnStyleDecl">The object to use to create fields</param>
    public FieldList(object lonnStyleDecl) {
        var props = lonnStyleDecl.GetType().GetProperties();
        var order = new List<string>();

        foreach (var prop in props) {
            var value = prop.GetValue(lonnStyleDecl);

            if (value is Field f) {
                order.Add(prop.Name);
                Add(prop.Name, f);
                continue;
            }

            if (prop.PropertyType.IsEnum && value is { }) {
                order.Add(prop.Name);
                Add(prop.Name, Fields.EnumNamesDropdown(value, prop.PropertyType));
                continue;
            }

            if (Fields.GuessFromValue(value, fromMapData: false) is { } field) {
                order.Add(prop.Name);
                Add(prop.Name, field);
            }
        }

        Order = (e) => order;
    }

    public FieldList Ordered(List<string> strings) {
        Order = (_) => strings;

        return this;
    }

    public FieldList Ordered(Func<Entity, List<string>> getter) {
        Order = (arg) => getter((Entity) arg!);

        return this;
    }

    public FieldList Ordered<T>(Func<T, List<string>> getter) {
        Order = (arg) => getter((T) arg!);

        return this;
    }

    public FieldList Ordered(params string[] strings)
        => Ordered(new List<string>(strings));

    public FieldList SetHiddenFields(IEnumerable<string> toHide) {
        var toHideList = toHide.ToList(); // avoid multiple enumeration
        GetDynamicallyHiddenFields = _ => toHideList;

        return this;
    }
    
    public FieldList SetHiddenFields(Func<FormContext, IEnumerable<string>>? toHide) {
        if (toHide is {})
            GetDynamicallyHiddenFields = toHide;

        return this;
    }

    public void AddTranslations(string tooltipKeyPrefix, string nameKeyPrefix, string defaultTooltipKeyPrefix, string defaultNameKeyPrefix) {
        foreach (var (name, f) in this) {
            if (f.Tooltip.IsNull) {
                f.Tooltip = Tooltip.CreateTranslatedOrNull($"{tooltipKeyPrefix}.{name}", $"{defaultTooltipKeyPrefix}.{name}");
            }
            f.NameOverride ??= name.TranslateOrNull(nameKeyPrefix) ?? name.TranslateOrNull(defaultNameKeyPrefix);
        }
    }
    
    public void AddTranslations(string defaultTooltipKeyPrefix, string defaultNameKeyPrefix) {
        foreach (var (name, f) in this) {
            if (f.Tooltip.IsNull) {
                f.Tooltip = Tooltip.CreateTranslatedOrNull($"{defaultTooltipKeyPrefix}.{name}");
            }
            f.NameOverride ??= name.TranslateOrNull(defaultNameKeyPrefix);
        }
    }
}
