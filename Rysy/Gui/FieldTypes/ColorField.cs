using ImGuiNET;
using Rysy.Extensions;
using Rysy.Helpers;

namespace Rysy.Gui.FieldTypes;

public sealed record class ColorField : Field, ILonnField, IListFieldExtender, IFieldConvertible<string>, IFieldConvertible<Color> {
    public static string Name => "color";

    // stores the original string passed to SetDefault.
    // this is so that if the map stored a non-lowercase hex color, the field won't get marked as edited due to the string->Color->string roundtrip which will make it all lowercase.
    // set to null as soon as the field gets edited.
    //private string? ValueString;

    //public Color? Default { get; set; }
    public string? Default { get; set; }

    public bool XnaColorsAllowed { get; set; } = true;

    public ColorFormat Format { get; set; }

    public bool NullAllowed;

    public bool EmptyIsNull;

    public override object GetDefault() => Default!;

    public override void SetDefault(object newDefault) {
        if (newDefault is Color c) {
            Default = c.ToString(Format);
            return;
        }

        if (newDefault is string str) {
            Default = str;
            return;
        }

        Default = default;
    }

    public bool ValueToColor(object? value, out Color color) {
        var valueStr = ValueToString(value);
        if (string.IsNullOrWhiteSpace(valueStr)) {
            color = default;
            return NullAllowed;
        }

        // ColorHelper supports 7-length colors due to frost helper backwards compat, but we shouldn't allow it anymore.
        if (valueStr.Length == 7) {
            color = default;
            return false;
        }
        
        return ColorHelper.TryGet(valueStr, Format, out color, XnaColorsAllowed);
    }

    public override string ValueToString(object? value) {
        return value switch {
            "" when EmptyIsNull => null!,
            Color c => ColorHelper.ToString(c, Format),
            _ => base.ValueToString(value),
        };
    }

    public override ValidationResult IsValid(object? value) {
        if (value is null && !NullAllowed) 
            return ValidationResult.CantBeNull;

        return ValueToColor(value, out _) ? base.IsValid(value) : ValidationResult.MustBeColor(Format);
    }

    public override object? RenderGui(string fieldName, object? value) {
        string? hexCodeOverride = value?.ToString() ?? "";
        
        if (ImGuiManager.ColorEditAllowEmpty(fieldName, ref hexCodeOverride, Format, Tooltip)) {
            return hexCodeOverride;
        }
/*
        if (!ValueToColor(value, out var color)) {
            color = Color.White;
        }
        
        if (value is string str)
            ValueString = str;

        if (NullAllowed && EmptyIsNull) {
            if (ImGuiManager.ColorEditAllowEmpty(fieldName, ref hexCodeOverride, Format, Tooltip)) {
                ValueString = hexCodeOverride;
                return hexCodeOverride;
            }
        } else {
            if (ImGuiManager.ColorEdit(fieldName, ref color, Format, Tooltip, hexCodeOverride)) {
                ValueString = null;
                return color.ToString(Format);
            }
        }
*/

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
    
    public ColorField TreatEmptyAsNull() {
        EmptyIsNull = true;

        return this;
    }

    public static Field Create(object? def, IUntypedData fieldInfoEntry) {
        var format = ColorFormat.RGB;
        if (fieldInfoEntry.Bool("useAlpha")) {
            format = ColorFormat.RGBA;
        }
        
        var allowXNA = fieldInfoEntry.Bool("allowXNAColors", false);

        var colorField = new ColorField() {
            Format = format,
        };
        
        if (allowXNA)
            colorField.AllowXNAColors();

        if (fieldInfoEntry.Bool("allowEmpty")) {
            colorField.AllowNull().TreatEmptyAsNull();
        }
        
        colorField.SetDefault(def?.ToString() ?? "");

        return colorField;
    }

    NumVector3 HsvFilterStorage = default;

    public void RenderPostListElementsGui(ListFieldContext ctx) {
        if (!ImGui.BeginMenu("HSV Filter")) {
            HsvFilterStorage = default;
            return;
        }

        var values = ctx.Values;

        ImGui.Text("From:");
        foreach (var item in values) {
            ImGui.SameLine();

            if (ColorHelper.TryGet(item, Format, out var color)) {
                ImGui.ColorButton(item, color.ToNumVec4(), ImGuiColorEditFlags.NoTooltip);
            }
        }

        ImGui.Text("To:");
        foreach (var item in values) {
            ImGui.SameLine();

            if (ColorHelper.TryGet(item, Format, out var color)) {
                var cv = color.ToNumVec4();
                ImGui.ColorConvertRGBtoHSV(cv.X, cv.Y, cv.Z, out var h, out var s, out var v);
                ImGui.ColorConvertHSVtoRGB(h + HsvFilterStorage.X.Div(180f), s + HsvFilterStorage.Y.Div(100f), v + HsvFilterStorage.Z.Div(100f), out cv.X, out cv.Y, out cv.Z);

                ImGui.ColorButton(item, cv, ImGuiColorEditFlags.NoTooltip);
            }
        }

        ImGui.DragFloat("H", ref HsvFilterStorage.X, 1f, v_min: -180f, v_max: 180f);
        ImGui.DragFloat("S", ref HsvFilterStorage.Y, 1f, v_min: -100f, v_max: 100f);
        ImGui.DragFloat("V", ref HsvFilterStorage.Z, 1f, v_min: -100f, v_max: 100f);

        if (ImGui.Button("Apply")) {
            for (int i = 0; i < values.Count; i++) {
                if (ColorHelper.TryGet(values[i], Format, out var color)) {
                    ctx.SetValue(i, ColorHelper.ToString(color.AddHSV(HsvFilterStorage.X, HsvFilterStorage.Y, HsvFilterStorage.Z), Format));
                }
            }
            HsvFilterStorage = default;
        }

        ImGui.EndMenu();
    }

    string IFieldConvertible<string>.ConvertMapDataValue(object value) 
        => ValueToString(value);

    Color IFieldConvertible<Color>.ConvertMapDataValue(object value) {
        if (ValueToColor(value, out var color)) {
            return color;
        }

        return Default.ToColorOr(default(Color), Format);
    }
}
