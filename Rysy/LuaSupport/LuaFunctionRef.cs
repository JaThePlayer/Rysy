using KeraLua;

namespace Rysy.LuaSupport;

/// <summary>
/// Provides a way to access a lua closure via c#.
/// Keeps the closure alive on the Lua side until <see cref="LuaExt.ClearLuaResources"/> gets called after this object gets GC'd.
/// </summary>
public class LuaFunctionRef {
    private long _id;
    public readonly Lua Lua;

    private LuaFunctionRef(Lua lua, long id) {
        Lua = lua;
        _id = id;
    }

    public static LuaFunctionRef MakeFrom(Lua lua, int loc) {
        lua.GetGlobalASCII("__rysy_make_luaFuncRef"u8);
        lua.PushCopy(loc);
        lua.Call(1, 1);
        var id = lua.ToInteger(lua.GetTop());
        lua.Pop(1);

        return new(lua, id);
    }

    public void PushToStack(Lua? lua = null) {
        lua ??= Lua;
        lua.GetGlobalASCII("__rysy_get_luaFuncRef"u8);
        lua.PushInteger(_id);
        lua.Call(1, 1);
    }

    ~LuaFunctionRef() {
        // Clear the reference on the lua side
        var lua = Lua;
        var id = _id;
        LuaExt.RegisterLuaCleanupAction(() => {
            lua.GetGlobalASCII("__rysy_gc_luaFuncRef"u8);
            lua.PushInteger(id);
            lua.Call(1, 0);
        });
    }
}