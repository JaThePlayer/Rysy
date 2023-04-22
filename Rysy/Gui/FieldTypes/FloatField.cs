using ImGuiNET;

namespace Rysy.Gui.FieldTypes;

public record class FloatField : IField {
    public string? Tooltip { get; set; }

    public float Default { get; set; }

    public object GetDefault() => Default;

    public void SetDefault(object newDefault)
        => Default = Convert.ToSingle(newDefault);

    public bool IsValid(object value) => value is float or int;

    public object? RenderGui(string fieldName, object value) {
        float b = Convert.ToSingle(value);
        if (ImGui.InputFloat(fieldName, ref b).WithTooltip(Tooltip))
            return b;

        return null;
    }

    public IField CreateClone() => this with { };
}