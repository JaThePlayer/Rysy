using ImGuiNET;

namespace Rysy.Gui.FieldTypes;

public class EditableDropdownField<T> : IField {
    public List<T> Values;

    public T Default { get; set; }

    public object GetDefault() => Default!;

    public object? RenderGui(string fieldName, object value) {
        if (value is not T val) {
            return null;
        }

        T? ret = default;

        var humanizedName = value.ToString();

        ImGui.SetNextItemWidth(ImGui.CalcItemWidth() - ImGui.CalcTextSize("+").X - ImGui.GetStyle().FramePadding.X * 2);
        if (ImGui.InputText("#text", ref fieldName, 128)) {
            // TODO: HANDLE
        }

        ImGui.SameLine();
        if (ImGui.BeginCombo("##combo", humanizedName, ImGuiComboFlags.NoPreview)) {
            foreach (var key in Values) {
                if (ImGui.MenuItem(key?.ToString())) {
                    ret = key;
                }
            }

            ImGui.EndCombo();
        }
        ImGui.SameLine();
        ImGui.Text(fieldName);

        return ret;
    }
}
