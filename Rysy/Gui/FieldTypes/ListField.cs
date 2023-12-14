using ImGuiNET;
using Rysy.Extensions;
using System;

namespace Rysy.Gui.FieldTypes;

public record class ListField : Field, IFieldConvertibleToList {
    public string Separator = ",";

    public Field BaseField { get; set; }

    public Func<object, string> InnerObjToString;

    public Func<string, bool> ElementCanBeRemoved = _ => true;

    public int MinElements = 1;
    public int MaxElements = -1;

    /// <summary>
    /// Specifies whether editing the full string preview is possible.
    /// If this is false, only editing via the individual fields is possible.
    /// </summary>
    public bool AllowEdits = true;

    public string? Default { get; set; }

    public ListField(Field baseField) {
        ArgumentNullException.ThrowIfNull(baseField);

        BaseField = baseField;
        InnerObjToString = o => PrepareBaseField().ValueToString(o);

        Default = InnerObjToString(PrepareBaseField().GetDefault());
    }

    public ListField(Field baseField, string @default) {
        ArgumentNullException.ThrowIfNull(baseField);

        BaseField = baseField;
        InnerObjToString = o => PrepareBaseField().ValueToString(o);

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
            if (!PrepareBaseField().IsValid(item))
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

        if (PrepareBaseField() is IListFieldExtender ext) {
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
        ImGui.BeginDisabled(!AllowEdits);
        if (ImGui.InputText($"##text{fieldName}", ref str, 1024).WithTooltip(Tooltip)) {
            ret = str;
        }
        ImGui.EndDisabled();

        bool anyChanged = false;

        if (split.Length == 0) {
            //ImGuiManager.TranslatedText("rysy.fields.list.noElements");
            split = new string[] { "" };
        }

        ImGui.SameLine(0f, xPadding);

        if (ImGui.BeginCombo($"##lcombo{fieldName}", "", ImGuiComboFlags.NoPreview).WithTooltip(Tooltip)) {
            var oldStyles = ImGuiManager.PopAllStyles();
            
            for (int i = 0; i < split.Length; i++) {
                var item = split[i];

                if (!PrepareBaseField().IsValid(item))
                    ImGuiManager.PushInvalidStyle();

                if (PrepareBaseField().RenderGui(i.ToString(CultureInfo.InvariantCulture), item) is { } newValue) {
                    split[i] = InnerObjToString(newValue);
                    anyChanged = true;
                }
                ImGuiManager.PopInvalidStyle();

                ImGui.SameLine();

                ImGui.BeginDisabled(split.Length <= MinElements || !ElementCanBeRemoved(item));
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


    public IReadOnlyList<T> ConvertMapDataValueToList<T>(object value) {
        var split = Split(value?.ToString() ?? "");
        var list = new List<T>(split.Length);

        switch (PrepareBaseField()) {
            case IFieldConvertible<T> convertible:
                for (int i = 0; i < split.Length; i++) {
                    list.Add(convertible.ConvertMapDataValue(split[i]));
                }
                break;
            case IFieldConvertible convertible:
                for (int i = 0; i < split.Length; i++) {
                    list.Add(convertible.ConvertMapDataValue<T>(split[i]));
                }
                break;
            default:
                throw new Exception($"Can't convert {nameof(ListField)}[{BaseField.GetType().Name}] to {typeof(T)}, as {BaseField.GetType().Name} does not implement {typeof(IFieldConvertible<T>)} or {typeof(IFieldConvertible)}");
        }

        return list;
    }

    public ListField WithMinAndMaxElements(int min, int max) {
        return this with {
            MinElements = min,
            MaxElements = max,
        };
    }

    public ListField WithMinElements(int min) {
        return this with {
            MinElements = min,
        };
    }

    public ListField WithMaxElements(int max) {
        return this with {
            MaxElements = max,
        };
    }

    private Field PrepareBaseField() {
        BaseField.Context = Context;

        return BaseField;
    }
}
