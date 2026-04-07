using Hexa.NET.ImGui;

namespace Rysy.Gui.FieldTypes;

/// <summary>
/// Abstract field type for fields which store more complex data types as strings.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract record ComplexTypeField<T> : Field, IFieldConvertible<T> {
    public T Default;

    public abstract T Parse(string data);

    public abstract string ConvertToString(T data);

    /// <summary>
    /// Renders the detailed window, which allows the user to edit the data.
    /// Returns whether the user made any changes.
    /// </summary>
    public abstract bool RenderDetailedWindow(ref T data);

    public override object GetDefault() => Default!;

    public override void SetDefault(object newDefault)
        => Default = ConvertMapDataValue(newDefault);

    public override object? RenderGui(string fieldName, object value) {
        var str = value.ToString() ?? "";

        var data = Parse(str);

        var xPadding = ImGui.GetStyle().FramePadding.X;
        var buttonWidth = ImGui.GetFrameHeight();
        const int buttonAmt = 1;

        ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - (buttonWidth * buttonAmt) - xPadding * buttonAmt);
        ImGui.BeginDisabled(true);
        ImGui.InputText($"##text{fieldName}", ref str, (uint)str.Length).WithTooltip(Tooltip);
        ImGui.EndDisabled();

        bool anyChanged = false;

        ImGui.SameLine(0f, xPadding);

        if (ImGui.BeginCombo($"##lcombo{fieldName}", "", ImGuiComboFlags.NoPreview).WithTooltip(Tooltip)) {
            var oldStyles = ImGuiManager.PopAllStyles();

            anyChanged = RenderDetailedWindow(ref data);

            ImGui.EndCombo();
            ImGuiManager.PushAllStyles(oldStyles);
        }

        ImGui.SameLine(0f, ImGui.GetStyle().FramePadding.X);
        ImGui.Text(fieldName);
        true.WithTooltip(Tooltip);

        return anyChanged ? ConvertToString(data) : null;
    }

    public override Field CreateClone() => this with { };
    
    public T ConvertMapDataValue(object value) => Parse(value?.ToString() ?? "");
}

/// <summary>
/// Field which combines several other fields and stores their value as a single string.
/// </summary>
internal sealed record ComplexField : ComplexTypeField<string[]> {
    public required char Separator { get; init; }
    
    public required IReadOnlyList<InnerField> InnerFields { get; init; }

    private object _default;

    public record InnerField(string Name, string Tooltip, object? Default, Field Field) {
        public static InnerField Create(string langKey, Field field) {
            return new InnerField(langKey, $"{langKey}.tooltip", field.GetDefault(), field);
        }
    }

    public override string[] Parse(string data) {
        return data.Split(Separator);
    }

    public override string ConvertToString(string[] data) {
        return string.Join(Separator, data);
    }

    public override bool RenderDetailedWindow(ref string[] data) {
        var edited = false;

        foreach (var (idx, (name, tooltip, def, field)) in InnerFields.Index()) {
            ImGui.PushID(name);

            var val = data.ElementAtOrDefault(idx) ?? def ?? "";
            var valid = field.IsValid(val);

            field.Tooltip = Tooltip.CreateTranslatedOrNull(tooltip);
            field.Context = Context;
            
            try {
                var newVal = field.RenderGuiWithValidation(name.Translate(), val, valid);
                if (newVal is { }) {
                    if (data.Length <= idx)
                        Array.Resize(ref data, idx + 1);
                    data[idx] = newVal.ToStringInvariant();
                    edited = true;
                }
            } finally {
                ImGui.PopID();
            }
        }

        return edited;
    }

    public override object GetDefault() => _default;

    public override void SetDefault(object newDefault) {
        _default = newDefault;
    }

    public override Field CreateClone() {
        return new ComplexField { Separator = Separator, InnerFields = InnerFields, _default = _default };
    }
}
