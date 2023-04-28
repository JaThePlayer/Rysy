using ImGuiNET;

namespace Rysy.Gui.FieldTypes;

public record class StringField : Field {
    public string Default { get; set; }

    public bool NullAllowed { get; set; }

    public override object GetDefault() => Default;

    public override void SetDefault(object newDefault)
        => Default = Convert.ToString(newDefault) ?? "";

    public override bool IsValid(object? value) => (value is string || (NullAllowed && value is null)) && base.IsValid(value);

    public override object? RenderGui(string fieldName, object value) {
        var b = (value ?? "").ToString();
        if (ImGui.InputText(fieldName, ref b, 256).WithTooltip(Tooltip))
            return b;

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

    public override Field CreateClone() => this with { };
}