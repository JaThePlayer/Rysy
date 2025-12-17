using KeraLua;
using Rysy.Helpers;

namespace Rysy.LuaSupport;

public record class ListWrapper<T>(List<T> Inner) : ILuaWrapper {
    public int LuaIndex(Lua lua, long key) {
        var i = (int)key - 1;
        var inner = Inner;

        if (i < inner.Count)
            lua.Push(inner[i]);
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

public record class WrapperListWrapper<T>(List<T> Inner) : ILuaWrapper
    where T : ILuaWrapper {
    public int LuaIndex(Lua lua, long key) {
        var intI = (int) key - 1;
        var inner = Inner;

        if (intI < inner.Count)
            lua.PushWrapper(inner[intI]);
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
