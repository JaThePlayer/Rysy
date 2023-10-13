using ImGuiNET;

namespace Rysy.Gui.FieldTypes;

public sealed record class IntField : Field, ILonnField, IFieldConvertible<int>, IFieldConvertible<float> {
    public static string Name => "integer";

    public int Step { get; set; } = 1;

    public int Min { get; set; } = int.MinValue;
    public int Max { get; set; } = int.MaxValue;

    public int Default { get; set; }


    public override object GetDefault() => Default;
    public override void SetDefault(object newDefault)
        => Default = Convert.ToInt32(newDefault, CultureInfo.InvariantCulture);

    public override bool IsValid(object? value) => value is int i && i >= Min && i <= Max && base.IsValid(value);

    public override object? RenderGui(string fieldName, object value) {
        int b = Convert.ToInt32(value, CultureInfo.InvariantCulture);
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

    public override Field CreateClone() => this with { };

    public static Field Create(object? def, Dictionary<string, object> fieldInfoEntry) {
        var min = fieldInfoEntry!.GetValueOrDefault("minimumValue", null);
        var max = fieldInfoEntry!.GetValueOrDefault("maximumValue", null);

        var field = Fields.Int(Convert.ToInt32(def, CultureInfo.InvariantCulture));

        if (min is { })
            field.WithMin(Convert.ToInt32(min, CultureInfo.InvariantCulture));
        if (max is { })
            field.WithMin(Convert.ToInt32(max, CultureInfo.InvariantCulture));

        return field;
    }

    int IFieldConvertible<int>.ConvertMapDataValue(object value) => Convert.ToInt32(value, CultureInfo.InvariantCulture);

    float IFieldConvertible<float>.ConvertMapDataValue(object value) => Convert.ToSingle(value, CultureInfo.InvariantCulture);
}