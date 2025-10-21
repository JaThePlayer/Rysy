using Hexa.NET.ImGui;

namespace Rysy.Gui.FieldTypes;

public record class CharField : Field, IFieldConvertible<char> {

    public char Default { get; set; }

    public override ValidationResult IsValid(object? value)
        => value is char ? base.IsValid(value) : ValidationResult.MustBeChar;

    public override object GetDefault() => Default;

    public override void SetDefault(object newDefault)
        => Default = ConvertMapDataValue(newDefault);


    public override object? RenderGui(string fieldName, object value) {
        var b = Convert.ToChar(value, CultureInfo.InvariantCulture).ToString();
        if (ImGui.InputText(fieldName, ref b, 1).WithTooltip(Tooltip))
            return b.Length > 0 ? b[0] : null;

        return null;
    }

    public override Field CreateClone() => this with { };

    public char ConvertMapDataValue(object value) => Convert.ToChar(value, CultureInfo.InvariantCulture);
}