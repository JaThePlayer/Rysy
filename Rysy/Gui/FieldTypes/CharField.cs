using ImGuiNET;

namespace Rysy.Gui.FieldTypes;

public record class CharField : IField {
    public string Tooltip { get; set; }

    public char Default { get; set; }

    public bool IsValid(object value) => value is char;

    public object GetDefault() => Default;

    public void SetDefault(object newDefault)
        => Default = Convert.ToChar(newDefault);


    public object? RenderGui(string fieldName, object value) {
        var b = Convert.ToChar(value).ToString();
        if (ImGui.InputText(fieldName, ref b, 1).WithTooltip(Tooltip))
            return b[0];

        return null;
    }

    public IField CreateClone() => this with { };
}