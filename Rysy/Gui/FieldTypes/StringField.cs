using ImGuiNET;

namespace Rysy.Gui.FieldTypes;

public record class StringField : Field, IFieldConvertible<string> {
    public string Default { get; set; }

    public bool NullAllowed { get; set; }
    public bool EmptyIsNull { get; set; }

    public override object GetDefault() => Default;

    public override void SetDefault(object newDefault)
        => Default = Convert.ToString(newDefault, CultureInfo.InvariantCulture) ?? "";

    private string? RealValue(string from)
        => (EmptyIsNull && string.IsNullOrWhiteSpace(from)) ? null : from;

    public override bool IsValid(object? value) {
        if (value is string s) {
            string? str = RealValue(s);

            return (str is string || (NullAllowed && str is null)) && base.IsValid(value);
        }

        return (NullAllowed && value is null) && base.IsValid(value);
    }

    public override object? RenderGui(string fieldName, object value) {
        var b = (value ?? "").ToString();
        if (ImGui.InputText(fieldName, ref b, 256).WithTooltip(Tooltip)) {
            if (RealValue(b) is { } ret)
                return ret;

            return new FieldNullReturn();
        }

        return null;
    }

    /// <summary>
    /// Allows null to be considered a valid value for this field.
    /// </summary>
    /// <returns>this</returns>
    public StringField AllowNull() {
        NullAllowed = true;

        return this;
    }

    public StringField ConvertEmptyToNull() {
        EmptyIsNull = true;

        return this;
    }

    /// <summary>
    /// Adds a validator to this field, which disallows saving the property if it returns false
    /// </summary>
    public StringField WithValidator(Func<string?, bool> validator) {
        Validator += (v) => validator(v?.ToString());

        return this;
    }

    public override Field CreateClone() => this with { };

    public string ConvertMapDataValue(object value) => RealValue((value ?? "").ToString()!)!;
}