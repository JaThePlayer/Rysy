using ImGuiNET;

namespace Rysy.Gui.FieldTypes;

public record class BoolField : IField {
    public string? Tooltip { get; set; }
    public string? NameOverride { get; set; }

    public bool Default { get; set; }

    public object GetDefault() => Default;
    public void SetDefault(object newDefault) 
        => Default = Convert.ToBoolean(newDefault);

    public bool IsValid(object value) => value is bool;

    public object? RenderGui(string fieldName, object value) {
        bool b = Convert.ToBoolean(value);
        if (ImGui.Checkbox(fieldName, ref b).WithTooltip(Tooltip))
            return b;

        return null;
    }

    public IField CreateClone() => this with { };
}
