using Rysy.Gui.Windows;

namespace Rysy.Gui.FieldTypes;

public sealed record ConditionalField(Func<FormContext, Field> FieldGetter) : Field {
    public override object GetDefault() => FieldGetter(Context).GetDefault();

    public override void SetDefault(object newDefault) => FieldGetter(Context).SetDefault(newDefault);

    public override object? RenderGui(string fieldName, object value) => FieldGetter(Context).RenderGui(fieldName, value);

    public override Field CreateClone() 
        => new ConditionalField(FieldGetter);
}