using ImGuiNET;
using Rysy.Extensions;
using Rysy.Helpers;

namespace Rysy.Gui.FieldTypes;

public record class ListField : Field {
    public char Separator = ',';

    public Field BaseField;

    public Func<object, string> InnerObjToString;

    public string? Default { get; set; }

    public ListField(Field baseField) {
        BaseField = baseField;
        InnerObjToString = BaseField.ValueToString;

        Default = InnerObjToString(BaseField.GetDefault());
    }

    public ListField(Field baseField, string @default) {
        BaseField = baseField;
        InnerObjToString = BaseField.ValueToString;

        Default = @default;
    }

    public override Field CreateClone() {
        return this with { };
    }

    public override object GetDefault() => Default!;

    public override void SetDefault(object newDefault) {
        Default = (string?)newDefault ?? "";
    }

    NumVector3 HsvFilter = default;

    private Color AddHSV(Color c, float h, float s, float v) {
        var cv = c.ToNumVec4();

        ImGui.ColorConvertRGBtoHSV(cv.X, cv.Y, cv.Z, out var oh, out var os, out var ov);
        ImGui.ColorConvertHSVtoRGB(oh + h.Div(180f), os + s.Div(100f), ov + v.Div(100f), out cv.X, out cv.Y, out cv.Z);

        return new(cv.ToXna());
    }

    private bool TypeSpecificGui(string[] split) {
        bool ret = false;

        switch (BaseField) {
            case ColorField colorField: {
                if (!ImGui.BeginMenu("HSV Filter")) {
                    HsvFilter = default;
                    break;
                }

                ImGui.Text("From:");
                foreach (var item in split) {
                    ImGui.SameLine();

                    if (ColorHelper.TryGet(item, colorField.Format, out var color)) {
                        ImGui.ColorButton(item, color.ToNumVec4(), ImGuiColorEditFlags.NoTooltip);
                    }
                }

                ImGui.Text("To:");
                foreach (var item in split) {
                    ImGui.SameLine();

                    if (ColorHelper.TryGet(item, colorField.Format, out var color)) {
                        var cv = color.ToNumVec4();
                        ImGui.ColorConvertRGBtoHSV(cv.X, cv.Y, cv.Z, out var h, out var s, out var v);
                        ImGui.ColorConvertHSVtoRGB(h + HsvFilter.X.Div(180f), s + HsvFilter.Y.Div(100f), v + HsvFilter.Z.Div(100f), out cv.X, out cv.Y, out cv.Z);

                        ImGui.ColorButton(item, cv, ImGuiColorEditFlags.NoTooltip);
                    }
                }

                ImGui.DragFloat("H", ref HsvFilter.X, 1f, v_min: -180f, v_max: 180f);
                ImGui.DragFloat("S", ref HsvFilter.Y, 1f, v_min: -100f, v_max: 100f);
                ImGui.DragFloat("V", ref HsvFilter.Z, 1f, v_min: -100f, v_max: 100f);

                if (ImGui.Button("Apply")) {
                    for (int i = 0; i < split.Length; i++) {
                        if (ColorHelper.TryGet(split[i], colorField.Format, out var color)) {
                            split[i] = ColorHelper.ToString(AddHSV(color, HsvFilter.X, HsvFilter.Y, HsvFilter.Z), colorField.Format);
                        }
                    }
                    HsvFilter = default;
                    ret = true;
                }

                ImGui.EndMenu();

                break;
            }
        }

        return ret;
    }

    public override object? RenderGui(string fieldName, object value) {
        if (value is not string str) {
            str = "";
        }

        string? ret = null;
        var split = str.Split(Separator);
        if (split is [ "" ])
            split = Array.Empty<string>();

        var xPadding = ImGui.GetStyle().FramePadding.X;
        var buttonWidth = ImGui.GetFrameHeight();
        const int ButtonAmt = 1;

        ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - (buttonWidth * ButtonAmt) - xPadding * ButtonAmt);
        if (ImGui.InputText($"##text{fieldName}", ref str, 1024).WithTooltip(Tooltip)) {
            ret = str;
        }

        bool anyChanged = false;

        /*
        ImGui.SameLine(0f, xPadding);
        if (ImGui.Button($"+##{fieldName}", new(buttonWidth, 0f))) {
            Array.Resize(ref split, split.Length + 1);
            split[^1] = InnerObjToString(BaseField.GetDefault());
            anyChanged = true;
        }*/

        ImGui.SameLine(0f, xPadding);

        if (ImGui.BeginCombo($"##lcombo{fieldName}", "", ImGuiComboFlags.NoPreview).WithTooltip(Tooltip)) {
            var oldStyles = ImGuiManager.PopAllStyles();
            for (int i = 0; i < split.Length; i++) {
                var item = split[i];

                if (BaseField.RenderGui(i.ToString(), item) is { } newValue) {
                    split[i] = InnerObjToString(newValue);
                    anyChanged = true;
                }

                ImGui.SameLine();
                if (ImGui.Button($"-##{i}")) {
                    // remove this item
                    var tempList = split.ToList();
                    tempList.RemoveAt(i);
                    split = tempList.ToArray();
                    anyChanged = true;
                }

                ImGui.SameLine();
                if (ImGui.Button($"+##{i}")) {
                    // insert a new item at this location
                    var tempList = split.ToList();
                    tempList.Insert(i, item);
                    split = tempList.ToArray();
                    anyChanged = true;
                }
            }

            anyChanged |= TypeSpecificGui(split);

            ImGui.EndCombo();
            ImGuiManager.PushAllStyles(oldStyles);
        }

        ImGui.SameLine(0f, ImGui.GetStyle().FramePadding.X);
        ImGui.Text(fieldName);
        true.WithTooltip(Tooltip);

        if (anyChanged) {
            ret = string.Join(Separator, split).TrimEnd(Separator);
        }

        return ret;
    }

}
