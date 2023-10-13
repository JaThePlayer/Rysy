namespace Rysy.Helpers;

public sealed class EntityList : TypeTrackedList<Entity> {
    private Dictionary<string, List<Entity>> SIDToEntities = new(StringComparer.Ordinal);

    public EntityList() : base() {
        OnChanged += () => {
            SIDToEntities.Clear();
        };
    }

#pragma warning disable CA1002 // Do not expose generic lists - performance is needed here
    public List<Entity> this[string sid] {
#pragma warning restore CA1002
        get {
            if (SIDToEntities.TryGetValue(sid, out var cached))
                return cached;

            var cache = Inner.Where(e => e.Name == sid).ToList();
            SIDToEntities[sid] = cache;

            return cache;
        }
    }
}
