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
    
    public string? Default { get; set; }

    /// <summary>
    /// Divides the number displayed in the gui by this number.
    /// </summary>
    public int DisplayScale { get; set; } = 1;

    public override object GetDefault() => (object?)ParseInput(Default) ?? Default!;
    public override void SetDefault(object newDefault)
        => Default = newDefault.ToStringInvariant();

    private int? ParseInput(object? value) {
        return value is null or "" && NullAllowed ? null : value.CoerceToInt();
    }
    
    public override ValidationResult IsValid(object? value) {
        if ((NullAllowed && value is null or "")) {
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
            ValidationMessage.TooSmallRecommended(v / DisplayScale, RecommendedMin / DisplayScale),
            ValidationMessage.TooLargeRecommended(v / DisplayScale, RecommendedMax / DisplayScale),
            ValidationMessage.NotRecommendedMultiple(v / DisplayScale, RecommendedStep)
        );
    }

    public override object? RenderGui(string fieldName, object value) {
        var v = ParseInput(value);
        var bStr = v is {} ? (v.Value / DisplayScale).ToStringInvariant() : value.ToStringInvariant();
        
        if (ImGuiManager.InputInt(fieldName, ref bStr, Tooltip)) {
            if (int.TryParse(bStr, CultureInfo.InvariantCulture, out var bRet))
                return bRet * DisplayScale;
            
            return null;
        }

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
                    return (true, x.CoerceToInt());
                } catch {
                    return (false, 0);
                }
            }) is {} dropdown) {
            return dropdown;
        }
        
        var min = fieldInfoEntry.Int("minimumValue", int.MinValue);
        var max = fieldInfoEntry.Int("maximumValue", int.MaxValue);

        var field = Fields.Int(def.CoerceToInt())
            .WithMin(min)
            .WithMax(max);

        return field;
    }

    int IFieldConvertible<int>.ConvertMapDataValue(object value) => value.CoerceToInt();

    float IFieldConvertible<float>.ConvertMapDataValue(object value) => value.CoerceToFloat();
}