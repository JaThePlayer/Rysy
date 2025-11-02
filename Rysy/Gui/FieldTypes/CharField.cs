using Hexa.NET.ImGui;

namespace Rysy.Gui.FieldTypes;

public record CharField : Field, IFieldConvertible<char> {
    public bool NullCharAllowed { get; set; }
    
    public char Default { get; set; }

    public CharField AllowEmptyAsNullChar() {
        NullCharAllowed = true;

        return this;
    }

    public override ValidationResult IsValid(object? value) {
        if (value is not char c) {
            return ValidationResult.MustBeChar;
        }

        if (!NullCharAllowed && c == '\0') {
            return ValidationResult.MustBeChar;
        }

        return base.IsValid(value);
    }

    public override object GetDefault() => Default;

    public override void SetDefault(object newDefault)
        => Default = ConvertMapDataValue(newDefault);


    public override object? RenderGui(string fieldName, object value) {
        var b = Convert.ToChar(value, CultureInfo.InvariantCulture).ToString();
        if (ImGui.InputText(fieldName, ref b, 2).WithTooltip(Tooltip))
            return b.Length > 0 ? b[0] : '\0';

        return null;
    }

    public override Field CreateClone() => this with { };

    public char ConvertMapDataValue(object value) => Convert.ToChar(value, CultureInfo.InvariantCulture);
}