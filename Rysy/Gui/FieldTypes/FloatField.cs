using ImGuiNET;
using Rysy.Helpers;

namespace Rysy.Gui.FieldTypes;

public sealed record class FloatField : Field, IFieldConvertible<int>, IFieldConvertible<float>, ILonnField {
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

    int IFieldConvertible<int>.ConvertMapDataValue(object value) => Convert.ToInt32(value, CultureInfo.InvariantCulture);

    float IFieldConvertible<float>.ConvertMapDataValue(object value) => Convert.ToSingle(value, CultureInfo.InvariantCulture);

    public static string Name => "number";

    public static Field Create(object? def, IUntypedData fieldInfoEntry) {
        if (fieldInfoEntry.TryGetValue("options", out _) 
            && Fields.CreateLonnDropdown(fieldInfoEntry, def ?? "", x => {
                try {
                    return Convert.ToSingle(x, CultureInfo.InvariantCulture);
                } catch {
                    Console.WriteLine($"FAILED TO CONVERT: {x} [{x?.GetType()}] to single");
                    return 0f;
                }
            }) is {} dropdown) {
            return dropdown;
        }
        
        var min = fieldInfoEntry.Float("minimumValue", float.MinValue);
        var max = fieldInfoEntry.Float("maximumValue", float.MaxValue);

        var field = Fields.Float(Convert.ToSingle(def, CultureInfo.InvariantCulture))
            .WithMin(min)
            .WithMax(max);

        return field;
    }
}