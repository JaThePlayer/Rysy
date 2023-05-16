using KeraLua;
using Rysy.Helpers;

namespace Rysy.LuaSupport;

public record class ListWrapper<T>(IList<T> Inner) : ILuaWrapper {
    public int Lua__index(Lua lua, long i) {
        var intI = (int)i - 1;
        var inner = Inner;

        if (intI < inner.Count)
            lua.Push(inner[intI]);
        else
            lua.PushNil();

        return 1;
    }

    public int Lua__index(Lua lua, ReadOnlySpan<char> key) {
        throw new Exception($"Tried to index list with non-number: {key} [{typeof(ReadOnlySpan<char>)}]");
    }

    public int Lua__len(Lua lua) {
        lua.PushInteger(Inner.Count);

        return 1;
    }
}

public record class WrapperListWrapper<T>(List<T> Inner) : ILuaWrapper
    where T : ILuaWrapper {
    public int Lua__index(Lua lua, long i) {
        var intI = (int) i - 1;
        var inner = Inner;

        if (intI < inner.Count)
            lua.PushWrapper(inner[intI]);
        else
            lua.PushNil();

        return 1;
    }

    public int Lua__index(Lua lua, ReadOnlySpan<char> key) {
        throw new Exception($"Tried to index list with non-number: {key} [{typeof(ReadOnlySpan<char>)}]");
    }

    public int Lua__len(Lua lua) {
        lua.PushInteger(Inner.Count);

        return 1;
    }
}

public record class EntityListWrapper(TypeTrackedList<Entity> Inner) : ILuaWrapper {
    public int Lua__index(Lua lua, long i) {
        var intI = (int) i - 1;
        var inner = Inner;

        if (intI < inner.Count)
            lua.PushWrapper(inner[intI]);
        else
            lua.PushNil();

        return 1;
    }

    public int Lua__index(Lua lua, ReadOnlySpan<char> key) {
        throw new Exception($"Tried to index list with non-number: {key} [{typeof(ReadOnlySpan<char>)}]");
    }

    public int Lua__len(Lua lua) {
        lua.PushInteger(Inner.Count);

        return 1;
    }
}
