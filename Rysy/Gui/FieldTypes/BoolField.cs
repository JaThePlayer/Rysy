using ImGuiNET;

namespace Rysy.Gui.FieldTypes;

public record class BoolField : Field {
    public bool Default { get; set; }

    public override object GetDefault() => Default;
    public override void SetDefault(object newDefault) 
        => Default = Convert.ToBoolean(newDefault);

    public override bool IsValid(object? value) => value is bool && base.IsValid(value);

    public override object? RenderGui(string fieldName, object value) {
        bool b = Convert.ToBoolean(value);
        if (ImGui.Checkbox(fieldName, ref b).WithTooltip(Tooltip))
            return b;

        return null;
    }

    public override Field CreateClone() => this with { };
}
