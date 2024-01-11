namespace Rysy.Gui.FieldTypes;

public record class PaddingField(string? Text = null) : Field {
    public override Field CreateClone() => this with { };

    public override object GetDefault() => null!;

    public override object? RenderGui(string fieldName, object value) {
        return null;
    }

    public override void SetDefault(object newDefault) {

    }
}
