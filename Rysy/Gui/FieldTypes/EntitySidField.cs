using Rysy.Helpers;

namespace Rysy.Gui.FieldTypes;

public record EntitySidField : DropdownField<string> {
    private static Cache<Dictionary<string, string>> CreateCache(RegisteredEntityType types) {
        return EntityRegistry.Registered.CreateCache(x =>
            x.Where(y => (y.Value.Type & types) != 0)
            .ToDictionary(y => y.Key, y => y.Key));
    }

    private static readonly Dictionary<RegisteredEntityType, Cache<Dictionary<string, string>>> Caches = new();
    
    public EntitySidField(string @default, RegisteredEntityType validTypes) {
        if (!Caches.TryGetValue(validTypes, out var values)) {
            Caches[validTypes] = values = CreateCache(validTypes);
        }

        Default = @default;
        Values = _ => values.Value;
        Editable = false;
    }
}