using Rysy.Extensions;
using Rysy.Helpers;

namespace Rysy.Gui.FieldTypes.Modded;

internal sealed record FrostHelperAttachGroup : DropdownField<int>, ILonnField {
    public static string Name => "FrostHelper.attachGroup";

    public FrostHelperAttachGroup(string def) {
        Default = int.TryParse(def, CultureInfo.InvariantCulture, out var i) ? i : 0;
        Editable = true;
        Values = _ => EditorState.CurrentRoom is { } room
            ? room.Entities.Concat(room.Triggers)
                .Where(e => e.Has("attachGroup"))
                .Select(e => e.Int("attachGroup"))
                .SafeToDictionary(e => (e, e.ToString()))
            : [];
    }
    
    public static Field Create(object? def, IUntypedData fieldInfoEntry) => new FrostHelperAttachGroup(def?.ToString() ?? "");
}
