using Rysy.Gui.FieldTypes;

namespace Rysy;

public interface IField {
    public object GetDefault();

    public object? RenderGui(string fieldName, object value);
}

public sealed class FieldList : Dictionary<string, IField> { }
