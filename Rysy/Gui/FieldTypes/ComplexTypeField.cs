using ImGuiNET;
using Rysy.Graphics;
using Rysy.Helpers;

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
        const int ButtonAmt = 1;

        ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - (buttonWidth * ButtonAmt) - xPadding * ButtonAmt);
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