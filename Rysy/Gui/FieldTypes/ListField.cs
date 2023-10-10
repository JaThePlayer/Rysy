using ImGuiNET;
using Rysy.Extensions;

namespace Rysy.Gui.FieldTypes;

public record class ListField : Field {
    public string Separator = ",";

    public Field BaseField;

    public Func<object, string> InnerObjToString;

    public int MinElements = 1;
    public int MaxElements = -1;

    public string? Default { get; set; }

    public ListField(Field baseField) {
        ArgumentNullException.ThrowIfNull(baseField);

        BaseField = baseField;
        InnerObjToString = BaseField.ValueToString;

        Default = InnerObjToString(BaseField.GetDefault());
    }

    public ListField(Field baseField, string @default) {
        ArgumentNullException.ThrowIfNull(baseField);

        BaseField = baseField;
        InnerObjToString = BaseField.ValueToString;

        Default = @default;
    }

    public override bool IsValid(object? value) {
        if (value is not string str) {
            return base.IsValid(value);
        }
        var split = Split(str);

        if (split.Length < MinElements) {
            return false;
        }
        if (MaxElements > -1 && split.Length > MaxElements) {
            return false;
        }

        foreach (var item in split) {
            if (!BaseField.IsValid(item))
                return false;
        }

        return base.IsValid(value);
    }

    public override Field CreateClone() {
        return this with { };
    }

    public override object GetDefault() => Default!;

    public override void SetDefault(object newDefault) {
        Default = (string?)newDefault ?? "";
    }

    public ListField WithSeparator(char separator) {
        Separator = separator.ToString();
        return this;
    }

    public ListField WithSeparator(string separator) {
        Separator = separator;
        return this;
    }

    private string[] Split(string value) {
        var split = value.Split(Separator);
        if (split is [""])
            split = Array.Empty<string>();

        return split;
    }

    private bool TypeSpecificGui(ref string[] split) {
        bool ret = false;

        if (BaseField is IListFieldExtender ext) {
            var ctx = new ListFieldContext(this, split);
            ext.RenderPostListElementsGui(ctx);
            if (ctx.Changed) {
                ret = true;
                split = ctx.ValuesArray;
            }
        }

        return ret;
    }

    public override object? RenderGui(string fieldName, object value) {
        if (value is not string str) {
            str = "";
        }

        string? ret = null;
        var split = Split(str);

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

                ImGui.BeginDisabled(split.Length <= MinElements);
                if (ImGui.Button($"-##{i}")) {
                    // remove this item
                    var tempList = split.ToList();
                    tempList.RemoveAt(i);
                    split = tempList.ToArray();
                    anyChanged = true;
                }
                ImGui.EndDisabled();

                ImGui.SameLine();
                ImGui.BeginDisabled(MaxElements > -1 && split.Length >= MaxElements);
                if (ImGui.Button($"+##{i}")) {
                    // insert a new item at this location
                    var tempList = split.ToList();
                    tempList.Insert(i, item);
                    split = tempList.ToArray();
                    anyChanged = true;
                }
                ImGui.EndDisabled();
            }

            anyChanged |= TypeSpecificGui(ref split);

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
