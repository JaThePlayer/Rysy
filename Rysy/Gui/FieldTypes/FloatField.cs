using Hexa.NET.ImGui;
using Rysy.Helpers;

namespace Rysy.Gui.FieldTypes;

public sealed record class FloatField : Field, IFieldConvertible<int>, IFieldConvertible<float>, ILonnField {
    public string? Default { get; set; }

    public float Min { get; set; } = float.MinValue;
    public float Max { get; set; } = float.MaxValue;
    
    public float RecommendedMin { get; set; } = float.MinValue;
    public float RecommendedMax { get; set; } = float.MaxValue;

    public bool EmptyAllowed { get; set; }
    
    public override object GetDefault() => (object?)ParseInput(Default) ?? Default!;

    public override void SetDefault(object? newDefault)
        => Default = ParseInput(newDefault)?.ToStringInvariant() ?? (EmptyAllowed ? "" : "0");

    private float? ParseInput(object? value) {
        return value.CoerceToFloatNullable();
    }

    public override ValidationResult IsValid(object? value) {
        if ((EmptyAllowed && value is null or "")) {
            return ValidationResult.Ok;
        }
        
        if (ParseInput(value) is not {} v) {
            return ValidationResult.MustBeNumber;
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
            ValidationMessage.TooLargeRecommended(v, RecommendedMax)
        );
    }

    protected override object? DoRenderGui(string fieldName, object value) {
        var str = value.ToStringInvariant();
        if (ImGuiManager.InputFloat(fieldName, ref str, Tooltip)) {
            if (EmptyAllowed && str.IsNullOrWhitespace())
                return new FieldNullReturn();
            
            if (float.TryParse(str, CultureInfo.InvariantCulture, out var f))
                return f;
        }

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

    public FloatField AllowNull() {
        EmptyAllowed = true;
        return this;
    }
    
    public FloatField WithRecommendedMin(float min) {
        RecommendedMin = min;
        return this;
    }

    public FloatField WithRecommendedMax(float max) {
        RecommendedMax = max;
        return this;
    }

    int IFieldConvertible<int>.ConvertMapDataValue(object value) => value.CoerceToInt();

    float IFieldConvertible<float>.ConvertMapDataValue(object value) => value.CoerceToFloat();

    public static string Name => "number";

    public static Field Create(object? def, IUntypedData fieldInfoEntry) {
        if (fieldInfoEntry.TryGetValue("options", out _) 
            && Fields.CreateLonnDropdown(fieldInfoEntry, def ?? "", x => {
                try {
                    return (true, Convert.ToSingle(x, CultureInfo.InvariantCulture));
                } catch {
                    return (false, 0f);
                }
            }) is {} dropdown) {
            return dropdown;
        }
        
        var min = fieldInfoEntry.Float("minimumValue", float.MinValue);
        var max = fieldInfoEntry.Float("maximumValue", float.MaxValue);

        var field = new FloatField()
            .WithMin(min)
            .WithMax(max);
        
        if (fieldInfoEntry.Bool("allowEmpty")) {
            field = field.AllowNull();
        }
        
        field.SetDefault(def);

        if (fieldInfoEntry.Has("warningBelowValue"))
            field.WithRecommendedMin(fieldInfoEntry.Float("warningBelowValue"));

        if (fieldInfoEntry.Has("warningAboveValue"))
            field.WithRecommendedMin(fieldInfoEntry.Float("warningAboveValue"));


        return field;
    }
}