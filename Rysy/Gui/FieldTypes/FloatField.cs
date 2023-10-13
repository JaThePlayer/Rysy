using ImGuiNET;

namespace Rysy.Gui.FieldTypes;

public record class FloatField : Field {
    public float Default { get; set; }

    public float Min { get; set; } = float.MinValue;
    public float Max { get; set; } = float.MaxValue;

    public override object GetDefault() => Default;

    public override void SetDefault(object newDefault)
        => Default = Convert.ToSingle(newDefault, CultureInfo.InvariantCulture);

    public override bool IsValid(object? value) => (value switch {
        int i when i >= Min && i <= Max => true,
        float i when i >= Min && i <= Max => true,
        _ => false
    }) && base.IsValid(value);

    public override object? RenderGui(string fieldName, object value) {
        float b = Convert.ToSingle(value, CultureInfo.InvariantCulture);
        if (ImGui.InputFloat(fieldName, ref b).WithTooltip(Tooltip))
            return b;

        return null;
    }

    public override Field CreateClone() => this with { };

    public FloatField WithMin(float min) {
        Min = min;
        return this;
    }

    public FloatField WithMax(float max) {
        Max = max;
        return this;
    }
}