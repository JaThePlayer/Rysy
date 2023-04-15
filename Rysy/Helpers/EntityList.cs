namespace Rysy.Helpers;

public sealed class EntityList : TypeTrackedList<Entity> {
    private Dictionary<string, List<Entity>> SIDToEntities = new(StringComparer.Ordinal);

    public EntityList() : base() {
        OnChanged += () => {
            SIDToEntities.Clear();
        };
    }

    public List<Entity> this[string sid] {
        get {
            if (SIDToEntities.TryGetValue(sid, out var cached))
                return cached;

            var cache = Inner.Where(e => e.Name == sid).ToList();
            SIDToEntities[sid] = cache;

            return cache;
        }
    }
}
