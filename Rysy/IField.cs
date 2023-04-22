using Rysy.Gui.FieldTypes;

namespace Rysy;

public interface IField {
    public string? Tooltip { get; set; }

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
}

public sealed class FieldList : Dictionary<string, IField> { }
