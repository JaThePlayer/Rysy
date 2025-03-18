using KeraLua;
using System.Text.Json.Serialization;

namespace Rysy.LuaSupport;

/// <summary>
/// Provides a way to access a lua value via c#.
/// Keeps the value alive on the Lua side until <see cref="LuaExt.ClearLuaResources"/> gets called after this object gets GC'd.
/// </summary>
public class LuaRef {
    private readonly long Id;
    
    [JsonIgnore]
    public readonly Lua Lua;
    
    protected internal LuaRef(Lua lua, long id) {
        Lua = lua;
        Id = id;
    }
    
    public static LuaRef MakeFrom(Lua lua, int loc) {
        var type = lua.Type(loc);
        lua.GetGlobal("__rysy_make_luaFuncRef"u8);
        lua.PushCopy(loc);
        lua.Call(1, 1);
        var id = lua.ToInteger(lua.GetTop());
        lua.Pop(1);

        return type switch {
            LuaType.Function => new LuaFunctionRef(lua, id),
            LuaType.Table => new LuaTableRef(lua, id),
            _ => new LuaRef(lua, id),
        };
    }

    public void PushToStack(Lua? lua = null) {
        lua ??= Lua;
        lua.GetGlobal("__rysy_get_luaFuncRef"u8);
        lua.PushInteger(Id);
        lua.Call(1, 1);
    }
    
    ~LuaRef() {
        // Clear the reference on the lua side
        var lua = Lua;
        var id = Id;
        LuaExt.RegisterLuaCleanupAction(() => {
            lua.GetGlobal("__rysy_gc_luaFuncRef"u8);
            lua.PushInteger(id);
            lua.Call(1, 0);
        });
    }
}