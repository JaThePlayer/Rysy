using KeraLua;

namespace Rysy.LuaSupport;

public interface ILuaDictionaryWrapper {
    public Dictionary<string, object> Dictionary { get; }
}

public class DictionaryWrapper(Dictionary<string, object> dict) : ILuaWrapper, ILuaDictionaryWrapper {
    private readonly Dictionary<string, object>.AlternateLookup<ReadOnlySpan<char>> _alternateLookup = dict.GetAlternateLookup<ReadOnlySpan<char>>();

    public Dictionary<string, object> Dictionary => dict;
    
    public int LuaIndex(Lua lua, long key) {
        return 0;
    }

    public virtual int LuaIndex(Lua lua, ReadOnlySpan<char> key) {
        if (_alternateLookup.TryGetValue(key, out var result)) {
            lua.Push(result);
            return 1;
        }

        return 0;
    }

    public virtual void LuaNewIndex(Lua lua, ReadOnlySpan<char> key, object value) {
        _alternateLookup[key] = value;
    }
}