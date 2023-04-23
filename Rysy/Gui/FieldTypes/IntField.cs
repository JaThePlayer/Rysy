using ImGuiNET;

namespace Rysy.Gui.FieldTypes;

public record class IntField : IField {
    public string? Tooltip { get; set; }
    public string? NameOverride { get; set; }
    public int Step { get; set; } = 1;

    public int Min { get; set; } = int.MinValue;
    public int Max { get; set; } = int.MaxValue;

    public int Default { get; set; }

    public object GetDefault() => Default;
    public void SetDefault(object newDefault)
        => Default = Convert.ToInt32(newDefault);

    public bool IsValid(object value) => value is int i && i >= Min && i <= Max;

    public object? RenderGui(string fieldName, object value) {
        int b = Convert.ToInt32(value);
        if (ImGui.InputInt(fieldName, ref b, Step).WithTooltip(Tooltip))
            return b;

        return null;
    }

    public IntField WithStep(int step) {
        Step = step;

        return this;
    }

    public IntField WithMin(int min) {
        Min = min;
        return this;
    }

    public IntField WithMax(int max) {
        Max = max;
        return this;
    }

    public IntField WithRange(Range range) {
        Min = range.Start.IsFromEnd ? int.MinValue : range.Start.Value;
        Max = range.End.IsFromEnd ? int.MaxValue : range.End.Value;

        return this;
    }

    public IField CreateClone() => this with { };
}