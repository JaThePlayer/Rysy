using Rysy.Helpers;

namespace Rysy.Gui.FieldTypes;

public record NullableBoolField : Field, IFieldConvertible<bool?> {
    private static readonly IReadOnlyList<bool?> Values = [ true, false, null ];
    private string _search = "";

    public string TrueName { get; set; } = "rysy.nullableBool.enabled";
    public string FalseName { get; set; } = "rysy.nullableBool.disabled";
    public string NullName { get; set; } = "rysy.nullableBool.default";

    public const string MapDefaultLangKey = "rysy.nullableBool.mapDefault";
    
    public bool? Default { get; set; }

    public NullableBoolField WithTrueName(string trueName) {
        TrueName = trueName;
        return this;
    }
    
    public NullableBoolField WithFalseName(string falseName) {
        FalseName = falseName;
        return this;
    }
    
    public NullableBoolField WithNullName(string nullName) {
        NullName = nullName;
        return this;
    }

    public override object GetDefault() => Default!;
    public override void SetDefault(object? newDefault)
        => Default = ConvertMapDataValue(newDefault);

    public override ValidationResult IsValid(object? value)
        => value switch {
            null or bool => base.IsValid(value),
            _ => ValidationResult.MustBeBool,
        };

    protected override object? DoRenderGui(string fieldName, object? value) {
        var b = ConvertMapDataValue(value);
        if (ImGuiManager.Combo(fieldName, ref b, Values, ToSearchable, ref _search).WithTooltip(Tooltip))
            return b;

        return null;
    }

    private Searchable ToSearchable(bool? arg) {
        return new(arg switch {
            true => TrueName.Translate(),
            false => FalseName.Translate(),
            null => NullName.Translate(),
        });
    }

    public override Field CreateClone() => this with { };

    public bool? ConvertMapDataValue(object? value) => value is null 
        ? null
        : Convert.ToBoolean(value, CultureInfo.InvariantCulture);
}
