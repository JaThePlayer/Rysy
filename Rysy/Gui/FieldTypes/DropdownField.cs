using ImGuiNET;

namespace Rysy.Gui.FieldTypes;

public class DropdownField<T> : IField where T : notnull {
    public Dictionary<T, string> Values;

    public T Default { get; set; }

    public object GetDefault() => Default!;

    public object? RenderGui(string fieldName, object value) {
        if (value is not T val) {
            return null;
        }

        Values.TryGetValue(val, out var humanizedName);
        humanizedName ??= value.ToString();

        T? ret = default;

        if (ImGui.BeginCombo(fieldName, humanizedName, ImGuiComboFlags.NoPreview)) {
            foreach (var (key, name) in Values) {
                if (ImGui.MenuItem(name)) {
                    ret = key;
                }
            }

            ImGui.EndCombo();
        }

        return ret;
    }
}
