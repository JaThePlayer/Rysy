using Rysy.Extensions;

namespace Rysy;

public abstract record class Field {
    public string? Tooltip { get; set; }
    public string? NameOverride { get; set; }

    public Func<object?, bool>? Validator { get; set; }

    public abstract object GetDefault();
    public abstract void SetDefault(object newDefault);

    public abstract object? RenderGui(string fieldName, object value);

    public virtual bool IsValid(object? value) => Validator?.Invoke(value) ?? true;

    public abstract Field CreateClone();
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
    /// Adds a validator to this field, which disallows saving the property if it returns false
    /// </summary>
    public static T WithValidator<T>(this T field, Func<object?, bool> validator) where T : Field {
        field.Validator += validator;

        return field;
    }
}

public sealed class FieldList : Dictionary<string, Field> { }
