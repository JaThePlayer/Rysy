using ImGuiNET;

namespace Rysy.Gui.FieldTypes;

public record class DropdownField<T> : IField where T : notnull {
    public string Tooltip { get; set; }

    public Dictionary<T, string> Values;

    public T Default { get; set; }

    public object GetDefault() => Default!;

    public void SetDefault(object newDefault)
        => Default = (T)newDefault;

    public bool IsValid(object value) {
        if (value is not T val) {
            return false;
        }

        return Values.TryGetValue(val, out _);
    }

    public object? RenderGui(string fieldName, object value) {
        if (value is not T val) {
            return null;
        }

        Values.TryGetValue(val, out var humanizedName);
        humanizedName ??= value.ToString();

        T? ret = default;

        if (ImGui.BeginCombo(fieldName, humanizedName).WithTooltip(Tooltip)) {
            foreach (var (key, name) in Values) {
                if (ImGui.MenuItem(name)) {
                    ret = key;
                }
            }

            ImGui.EndCombo();
        }

        return ret;
    }

    public IField CreateClone() => this with { };
}
