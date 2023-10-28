using KeraLua;

namespace Rysy.LuaSupport;

// A wrapper over an entity that allows lua to mutate it, including the _name field, by storing all changes done to it into a temporary dictionary.
public record class CloneEntityWrapper(Entity Entity) : ILuaWrapper {
    public Dictionary<string, object> Changes = new(StringComparer.Ordinal);
    public string? NewSID;

    public int LuaIndex(Lua lua, long key) {
        return Entity.LuaIndex(lua, key);
    }

    public int LuaIndex(Lua lua, ReadOnlySpan<char> key) {
        if (NewSID is { } newSid && key is "_name") {
            lua.PushString(newSid);
            return 1;
        }


        var changes = Changes;
        if (changes.Count > 0 && changes.TryGetValue(key.ToString(), out var changedVal)) {
            lua.Push(changedVal);
            return 1;
        }

        return Entity.LuaIndex(lua, key);
    }

    public void LuaNewIndex(Lua lua, ReadOnlySpan<char> key, object value) {
        switch (key) {
            case "_name":
                NewSID = value.ToString();
                break;
            default:
                Changes[key.ToString()] = value;
                break;
        }
    }

    public Entity CreateMutatedClone() => Entity.CloneWith(pl => {
        pl.SID = NewSID ?? Entity.Name;

        foreach (var (k, v) in Changes) {
            pl[k] = v;
        }
    });
}
