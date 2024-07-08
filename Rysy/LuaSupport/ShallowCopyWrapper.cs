using KeraLua;

namespace Rysy.LuaSupport;

/// <summary>
/// Wrapper type used when table.shallowcopy is used on a lua wrapper.
/// Allows mutating it properly, even if the source wrapper is immutable.
/// </summary>
public sealed class ShallowCopyWrapper(ILuaWrapper wrapper) : ILuaWrapper {
    private readonly Dictionary<object, object> _newValues = new();
    
    public int LuaIndex(Lua lua, long key) {
        if (_newValues.Count > 0 &&_newValues.TryGetValue(key, out var newVal)) {
            lua.Push(newVal);
            return 1;
        }

        return wrapper.LuaIndex(lua, key);
    }

    public int LuaIndex(Lua lua, ReadOnlySpan<char> key) {
        if (_newValues.Count > 0 && _newValues.TryGetValue(key.ToString(), out var newVal)) {
            lua.Push(newVal);
            return 1;
        }

        return wrapper.LuaIndex(lua, key);
    }

    public void LuaNewIndex(Lua lua, ReadOnlySpan<char> key, object value) {
        _newValues[key.ToString()] = value;
    }

    public void LuaNewIndex(Lua lua, long key, object value) {
        _newValues[key] = value;
    }
}