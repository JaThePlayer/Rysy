using Rysy.Extensions;
using Rysy.Gui.FieldTypes;
using Rysy.Gui.Windows;

namespace Rysy;

public abstract record class Field {
    public string? Tooltip { get; set; }
    public string? NameOverride { get; set; }

    public FormContext Context { get; internal set; }

    /// <summary>
    /// An arbitrary function that checks whether a value is valid. Called by <see cref="IsValid(object)"/>
    /// </summary>
    public Func<object?, bool>? Validator { get; set; }

    /// <summary>
    /// Gets the default value for this field
    /// </summary>
    public abstract object GetDefault();

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

    /// <summary>
    /// Checks whether <paramref name="value"/> is a valid value for this field. Make sure to call base.IsValid, as it handles the <see cref="Validator"/>
    /// </summary>
    /// <param name="value">The value to check</param>
    /// <returns>Whether this value is valid</returns>
    public virtual bool IsValid(object? value) => Validator?.Invoke(value) ?? true;

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
    public static abstract Field Create(object? def, Dictionary<string, object> fieldInfoEntry);
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
    /// Adds a validator to this field, which disallows saving the property if it returns false
    /// </summary>
    public static T WithValidator<T>(this T field, Func<object?, bool> validator) where T : Field {
        field.Validator += validator;

        return field;
    }

    /// <summary>
    /// Converts this field into a <see cref="ListField"/>, where each element will be an instance of <paramref name="field"/>.
    /// </summary>
    public static ListField ToList(this Field field, char separator = ',') {
        return new(field) {
            Separator = separator,
        };
    }
}

public sealed class FieldList : Dictionary<string, Field> {
    public FieldList() {

    }

    /// <summary>
    /// Creates a field list by using an anonymous object of the style of { fieldName = value, field2 = value2, ...}
    /// Values can also be fields, which will be used directly.
    /// If the value is an enum, the field is automatically turned into an uneditable dropdown.
    /// </summary>
    /// <param name="lonnStyleDecl">The object to use to create fields</param>
    public FieldList(object lonnStyleDecl) {
        var props = lonnStyleDecl.GetType().GetProperties();

        foreach (var prop in props) {
            var value = prop.GetValue(lonnStyleDecl);
            if (value is Field f) {
                Add(prop.Name, f);
                continue;
            }

            if (prop.PropertyType.IsEnum && value is { }) {
                Add(prop.Name, Fields.EnumNamesDropdown(value, prop.PropertyType));
                continue;
            }

            if (Fields.GuessFromValue(value, fromMapData: false) is { } field)
                Add(prop.Name, field);
        }
    }
}
