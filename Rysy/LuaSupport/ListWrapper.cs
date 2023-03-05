using KeraLua;

namespace Rysy.LuaSupport;

public record class ListWrapper<T>(IList<T> Inner) : ILuaWrapper {
    public int Lua__index(Lua lua, object key) {
        if (key is long i) {
            var item = Inner.ElementAtOrDefault((int) i - 1);

            lua.Push(item);
        } else {
            throw new Exception($"Tried to index list with non-number: {key} [{key.GetType()}]");
        }

        return 1;
    }
}
