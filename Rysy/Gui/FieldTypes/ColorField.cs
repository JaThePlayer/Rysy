using ImGuiNET;
using Rysy.Extensions;
using Rysy.Helpers;

namespace Rysy.Gui.FieldTypes;

public record class ColorField : IField {
    public string? Tooltip { get; set; }
    public string? NameOverride { get; set; }

    public Color Default { get; set; }

    public ColorFormat Format { get; set; }

    public object GetDefault() => Default.ToString(Format);

    public void SetDefault(object newDefault) {
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

    public bool ValueToColor(object value, out Color color) {
        return ColorHelper.TryGet((string) value, Format, out color);
    }

    public bool IsValid(object value) {
        return ValueToColor(value, out _);
    }

    public object? RenderGui(string fieldName, object value) {
        if (!ValueToColor(value, out var color)) {
            color = Color.White;
        }

        switch (Format) {
            case ColorFormat.RGB: {
                var c = color.ToNumVec3();
                if (ImGui.ColorEdit3(fieldName, ref c).WithTooltip(Tooltip)) {
                    return new Color(c).ToString(Format);
                }
                break;
            }
            case ColorFormat.ARGB:
            case ColorFormat.RGBA: {
                var c = color.ToNumVec4();
                if (ImGui.ColorEdit4(fieldName, ref c, ImGuiColorEditFlags.AlphaBar).WithTooltip(Tooltip)) {
                    return new Color(c).ToString(Format);
                }
                break;
            }
        }

        return null;
    }

    public IField CreateClone() => this with { };
}
