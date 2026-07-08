namespace Rysy.Gui.FieldTypes;

public record class PaddingField(string? Text = null, bool DrawSeparator = true) : Field {
    public override Field CreateClone() => this with { };

    public override object GetDefault() => null!;

    protected override object? DoRenderGui(string fieldName, object value) {
        return null;
    }

    public override void SetDefault(object newDefault) {

    }
}
