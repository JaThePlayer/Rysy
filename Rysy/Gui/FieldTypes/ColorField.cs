using Rysy.Helpers;

namespace Rysy.Gui.FieldTypes;

public record class ColorField : Field, ILonnField {
    public static string Name => "color";

    // stores the original string passed to SetDefault.
    // this is so that if the map stored a non-lowercase hex color, the field won't get marked as edited due to the string->Color->string roundtrip which will make it all lowercase.
    // set to null as soon as the field gets edited.
    private string? ValueString;

    public Color? Default { get; set; }

    public bool XnaColorsAllowed { get; set; } = true;

    public ColorFormat Format { get; set; }

    public bool NullAllowed;

    public override object GetDefault() => ValueString ?? Default?.ToString(Format)!;

    public override void SetDefault(object newDefault) {
        if (newDefault is Color c) {
            Default = c;
            return;
        }

        if (newDefault is string str && ValueToColor(newDefault, out c)) {
            ValueString = str;
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
        if (value is null && !NullAllowed) 
            return false;

        return ValueToColor(value, out _) && base.IsValid(value);
    }

    public override object? RenderGui(string fieldName, object? value) {
        if (!ValueToColor(value, out var color)) {
            color = Color.White;
        }

        if (value is string str)
            ValueString = str;

        if (ImGuiManager.ColorEdit(fieldName, ref color, Format, Tooltip)) {
            ValueString = null;
            return color.ToString(Format);
        }

        return null;
    }

    public override Field CreateClone() => this with { };

    public ColorField AllowXNAColors() {
        XnaColorsAllowed = true;

        return this;
    }

    public ColorField AllowNull() {
        NullAllowed = true;

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
