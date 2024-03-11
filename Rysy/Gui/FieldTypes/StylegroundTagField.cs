namespace Rysy.Gui.FieldTypes;

public record StylegroundTagField : DropdownField<string> {
    public StylegroundTagField(string def, bool editable) {
        Values = (ctx) => {
            if (EditorState.Map is not { } map) {
                return new Dictionary<string, string>();
            }

            return map.Style.AllTags().ToDictionary(x => x, x => x);
        };

        Default = def;
        Editable = editable;
    }
}