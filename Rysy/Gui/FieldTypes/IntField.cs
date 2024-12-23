using ImGuiNET;
using Rysy.Helpers;

namespace Rysy.Gui.FieldTypes;

public sealed record class IntField : Field, ILonnField, IFieldConvertible<int>, IFieldConvertible<float> {
    public static string Name => "integer";

    public int Step { get; set; } = 1;

    public int Min { get; set; } = int.MinValue;
    public int Max { get; set; } = int.MaxValue;
    
    public int RecommendedMin { get; set; } = int.MinValue;
    public int RecommendedMax { get; set; } = int.MaxValue;

    public int RecommendedStep { get; set; } = 1;

    public bool NullAllowed { get; set; }
    
    public int? Default { get; set; }

    /// <summary>
    /// Divides the number displayed in the gui by this number.
    /// </summary>
    public int DisplayScale { get; set; } = 1;

    public override object GetDefault() => Default!;
    public override void SetDefault(object newDefault)
        => Default = Convert.ToInt32(newDefault, CultureInfo.InvariantCulture);

    private int? ParseInput(object? value) {
        return value switch {
            int i => i,
            float f when float.IsInteger(f) => (int)f,
            string s when int.TryParse(s, CultureInfo.InvariantCulture, out var f) => f,
            _ => null,
        };
    }
    
    public override ValidationResult IsValid(object? value) {
        if ((NullAllowed && value is null)) {
            return ValidationResult.Ok;
        }
        
        if (ParseInput(value) is not {} v) {
            return ValidationResult.MustBeInt;
        }

        if (v < Min)
            return ValidationResult.TooSmall(Min);
        if (v > Max)
            return ValidationResult.TooLarge(Max);

        var baseValid = base.IsValid(value);
        if (!baseValid.IsOk)
            return baseValid;
        
        return ValidationResult.Combine(
            baseValid,
            ValidationMessage.TooSmallRecommended(v, RecommendedMin),
            ValidationMessage.TooLargeRecommended(v, RecommendedMax),
            ValidationMessage.NotRecommendedMultiple(v, RecommendedStep)
        );
    }

    public override object? RenderGui(string fieldName, object value) {
        var v = ParseInput(value) ?? 0;
        int b = v / DisplayScale;
        if (ImGui.InputInt(fieldName, ref b, Step).WithTooltip(Tooltip))
            return b * DisplayScale;

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
    
    public IntField WithRecommendedMin(int min) {
        RecommendedMin = min;
        return this;
    }

    public IntField WithMax(int max) {
        Max = max;
        return this;
    }
    
    public IntField WithRecommendedMax(int max) {
        RecommendedMax = max;
        return this;
    }
    
    public IntField WithRecommendedStep(int step) {
        Step = step;
        RecommendedStep = step;
        return this;
    }
    
    public IntField WithDisplayScale(int scale) {
        DisplayScale = scale;
        return this;
    }

    public IntField WithRange(Range range) {
        Min = range.Start.IsFromEnd ? int.MinValue : range.Start.Value;
        Max = range.End.IsFromEnd ? int.MaxValue : range.End.Value;

        return this;
    }

    public IntField AllowNull() {
        NullAllowed = true;
        return this;
    }

    public override Field CreateClone() => this with { };

    public static Field Create(object? def, IUntypedData fieldInfoEntry) {
        if (fieldInfoEntry.TryGetValue("options", out _) 
            && Fields.CreateLonnDropdown(fieldInfoEntry, def ?? "", x => {
                try {
                    return (true, Convert.ToInt32(x, CultureInfo.InvariantCulture));
                } catch {
                    return (false, 0);
                }
            }) is {} dropdown) {
            return dropdown;
        }
        
        var min = fieldInfoEntry.Int("minimumValue", int.MinValue);
        var max = fieldInfoEntry.Int("maximumValue", int.MaxValue);

        var field = Fields.Int(Convert.ToInt32(def, CultureInfo.InvariantCulture))
            .WithMin(min)
            .WithMax(max);

        return field;
    }

    int IFieldConvertible<int>.ConvertMapDataValue(object value) => Convert.ToInt32(value, CultureInfo.InvariantCulture);

    float IFieldConvertible<float>.ConvertMapDataValue(object value) => Convert.ToSingle(value, CultureInfo.InvariantCulture);
}