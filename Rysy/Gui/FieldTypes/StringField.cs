using ImGuiNET;

namespace Rysy.Gui.FieldTypes;

public record class StringField : IField {
    public string? Tooltip { get; set; }

    public string Default { get; set; }

    public object GetDefault() => Default;

    public void SetDefault(object newDefault)
        => Default = Convert.ToString(newDefault) ?? "";

    public bool IsValid(object value) => value is string;

    public object? RenderGui(string fieldName, object value) {
        var b = value.ToString();
        if (ImGui.InputText(fieldName, ref b, 256).WithTooltip(Tooltip))
            return b;

        return null;
    }

    public IField CreateClone() => this with { };
}