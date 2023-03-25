using KeraLua;
using Rysy.Helpers;

namespace Rysy.LuaSupport;

public record class ListWrapper<T>(IList<T> Inner) : ILuaWrapper {
    public int Lua__index(Lua lua, long i) {
        var intI = (int)i;
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
}

public record class EntityListWrapper(TypeTrackedList<Entity> Inner) : ILuaWrapper {
    public int Lua__index(Lua lua, long i) {
        var intI = (int) i;
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
}
