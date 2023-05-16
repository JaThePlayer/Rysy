using Rysy.Helpers;

namespace Rysy.Gui.FieldTypes;

public record class ColorField : Field, ILonnField {
    public static string Name => "color";

    public Color Default { get; set; }

    public bool XnaColorsAllowed { get; set; } = true;

    public ColorFormat Format { get; set; }

    public override object GetDefault() => Default.ToString(Format);

    public override void SetDefault(object newDefault) {
        if (newDefault is Color c) {
            Default = c;
            return;
        }

        if (newDefault is string && ValueToColor(newDefault, out c)) {
            Default = c;
            return;
        }

        Default = default;
    }

    public bool ValueToColor(object? value, out Color color) {
        return ColorHelper.TryGet(value?.ToString() ?? "", Format, out color, XnaColorsAllowed);
    }

    public override string ValueToString(object? value) {
        return value switch {
            Color c => ColorHelper.ToString(c, Format),
            _ => base.ValueToString(value),
        };
    }

    public override bool IsValid(object? value) {
        return ValueToColor(value, out _) && base.IsValid(value);
    }

    public override object? RenderGui(string fieldName, object value) {
        if (!ValueToColor(value, out var color)) {
            color = Color.White;
        }

        if (ImGuiManager.ColorEdit(fieldName, ref color, Format, Tooltip)) {
            return color.ToString(Format);
        }

        return null;
    }

    public override Field CreateClone() => this with { };

    public ColorField AllowXNAColors() {
        XnaColorsAllowed = true;

        return this;
    }

    public static Field Create(object? def, Dictionary<string, object> fieldInfoEntry) {
        var allowXNA = (bool) Convert.ChangeType(fieldInfoEntry.GetValueOrDefault("allowXNAColors", false), typeof(bool));
        var defColor = def is string defString ? ColorHelper.Get(defString) : Color.White;

        var colorField = Fields.RGBA(defColor);
        if (allowXNA)
            colorField.AllowXNAColors();

        return colorField;
    }
}
