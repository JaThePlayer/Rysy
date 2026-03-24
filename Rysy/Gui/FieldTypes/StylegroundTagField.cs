using Rysy.Helpers;

namespace Rysy.Gui.FieldTypes;

public record StylegroundTagField : DropdownField<string> {
    public StylegroundTagField(string def, bool editable) {
        Values = (ctx) => {
            if (ctx.EditorState?.Map is not { } map) {
                return new Dictionary<string, Searchable>();
            }

            return map.Style.AllTags().ToDictionary(x => x, x => new Searchable(x));
        };

        Default = def;
        Editable = editable;
    }
}