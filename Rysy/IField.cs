using Rysy.Extensions;

namespace Rysy;

public interface IField {
    public string? Tooltip { get; set; }
    public string? NameOverride { get; set; }

    public object GetDefault();
    public void SetDefault(object newDefault);

    public object? RenderGui(string fieldName, object value);

    public bool IsValid(object value);

    public IField CreateClone();
}

public static class FieldExtensions {
    /// <summary>
    /// Adds a tooltip to this <see cref="IField"/>, and returns this instance.
    /// </summary>
    public static T WithTooltip<T>(this T field, string? tooltip) where T : IField {
        field.Tooltip = tooltip;

        return field;
    }

    /// <summary>
    /// Adds a tooltip to this <see cref="IField"/>, and returns this instance, translating it.
    /// If no translation is found, the tooltip gets set to null.
    /// </summary>
    public static T WithTooltipTranslated<T>(this T field, string? tooltip) where T : IField {
        field.Tooltip = tooltip?.TranslateOrNull();

        return field;
    }

    /// <summary>
    /// Overrides the name of this <see cref="IField"/>, and returns this instance.
    /// </summary>
    public static T WithName<T>(this T field, string? name) where T : IField {
        field.NameOverride = name;

        return field;
    }
}

public sealed class FieldList : Dictionary<string, IField> { }
