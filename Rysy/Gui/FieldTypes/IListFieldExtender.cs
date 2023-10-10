namespace Rysy.Gui.FieldTypes;

/// <summary>
/// When implemented on a class extending Field, this interface allows adding functionality when the field is used within a list field.
/// </summary>
public interface IListFieldExtender {
    /// <summary>
    /// Renders the GUI after all of the elements of the list.
    /// </summary>
    public void RenderPostListElementsGui(ListFieldContext ctx);

}

public class ListFieldContext {
    public IReadOnlyList<string> Values => ValuesArray;
    public readonly ListField ListField;

    internal string[] ValuesArray;

    public bool Changed { get; private set; }

    internal ListFieldContext(ListField field, string[] values) {
        ListField = field;
        ValuesArray = values;
    }

    public void SetValue(int index, string value) {
        if (index >= ValuesArray.Length) {
            Array.Resize(ref ValuesArray, index);
            Changed = true;
        }

        ref var prev = ref ValuesArray[index];
        if (prev != value) {
            prev = value;
            Changed = true;
        }
    }
}
