using KeraLua;
using LuaNET.LuaJIT;
using Rysy.LuaSupport;

namespace Rysy.Helpers;

public sealed class EntityList : TypeTrackedList<Entity>, ILuaWrapper {
    private Dictionary<string, List<Entity>> _sidToEntities = new(StringComparer.Ordinal);

    public EntityList() {
        OnChanged += () => {
            _sidToEntities.Clear();
        };
    }

#pragma warning disable CA1002 // Do not expose generic lists - performance is needed here
    public List<Entity> this[string sid] {
#pragma warning restore CA1002
        get {
            if (_sidToEntities.TryGetValue(sid, out var cached))
                return cached;

            var cache = Inner.Where(e => e.Name == sid).ToList();
            _sidToEntities[sid] = cache;

            return cache;
        }
    }

    public int LuaIndex(Lua lua, long key) {
        var i = (int) key - 1;
        var inner = Inner;

        if (i < inner.Count)
            lua.PushWrapper(inner[i]);
        else
            lua.PushNil();

        return 1;
    }

    public int LuaIndex(Lua lua, ReadOnlySpan<char> key) {
        throw new Exception($"Tried to index list with non-number: {key} [{typeof(ReadOnlySpan<char>)}]");
    }

    public int LuaLen(Lua lua) {
        lua.PushInteger(Inner.Count);

        return 1;
    }
    
    public int LuaNextIPairs(Lua lua, int key) {
        key++;
        
        var i = key - 1;
        var inner = Inner;

        if (i < inner.Count) {
            lua.PushInteger(key);
            lua.Push(inner[i]);
            return 2;
        }
        
        lua.PushNil();
        return 1;
    }
}
