using ImGuiNET;

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
        ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - (buttonWidth * 2) - xPadding * 2);
        if (ImGui.InputText($"##text{fieldName}", ref str, 1024).WithTooltip(Tooltip)) {
            ret = str;
        }

        bool anyChanged = false;

        ImGui.SameLine(0f, xPadding);
        if (ImGui.Button($"+##{fieldName}", new(buttonWidth, 0f))) {
            Array.Resize(ref split, split.Length + 1);
            split[^1] = InnerObjToString(BaseField.GetDefault());
            anyChanged = true;
        }

        ImGui.SameLine(0f, xPadding);

        if (ImGui.BeginCombo($"##lcombo{fieldName}", "", ImGuiComboFlags.NoPreview).WithTooltip(Tooltip)) {
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

            ImGui.EndCombo();
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
