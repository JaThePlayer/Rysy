using ImGuiNET;

namespace Rysy.Gui.FieldTypes;

public class IntField : IField {
    public int Step { get; set; } = 1;

    public int Min { get; set; } = int.MinValue;
    public int Max { get; set; } = int.MaxValue;

    public int Default { get; set; }

    public object GetDefault() => Default;

    public bool IsValid(object value) => value is int i && i >= Min && i <= Max;

    public object? RenderGui(string fieldName, object value) {
        int b = Convert.ToInt32(value);
        if (ImGui.InputInt(fieldName, ref b, Step))
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
}