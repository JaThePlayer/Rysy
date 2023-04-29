using ImGuiNET;
using Rysy.Extensions;
using Rysy.Helpers;

namespace Rysy.Gui.FieldTypes;

public record class ColorField : Field {
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

    public override bool IsValid(object? value) {
        return ValueToColor(value, out _) && base.IsValid(value);
    }

    public override object? RenderGui(string fieldName, object value) {
        if (!ValueToColor(value, out var color)) {
            color = Color.White;
        }

        switch (Format) {
            case ColorFormat.RGB: {
                var c = color.ToNumVec3();
                if (ImGui.ColorEdit3(fieldName, ref c, ImGuiColorEditFlags.DisplayHex).WithTooltip(Tooltip)) {
                    return new Color(c).ToString(Format);
                }
                break;
            }
            case ColorFormat.ARGB:
            case ColorFormat.RGBA: {
                var c = color.ToNumVec4();
                if (ImGui.ColorEdit4(fieldName, ref c, ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.DisplayHex).WithTooltip(Tooltip)) {
                    return new Color(c).ToString(Format);
                }
                break;
            }
        }

        return null;
    }

    public override Field CreateClone() => this with { };

    public ColorField AllowXNAColors() {
        XnaColorsAllowed = true;

        return this;
    }
}
