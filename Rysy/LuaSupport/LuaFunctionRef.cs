using KeraLua;
using System.Text.Json.Serialization;

namespace Rysy.LuaSupport;

/// <summary>
/// Provides a way to access a lua closure via c#.
/// Keeps the closure alive on the Lua side until <see cref="LuaExt.ClearLuaResources"/> gets called after this object gets GC'd.
/// </summary>
public class LuaFunctionRef : LuaRef {
    protected internal LuaFunctionRef(Lua lua, long id) : base(lua, id)
    {
    }

    public new static LuaFunctionRef MakeFrom(Lua lua, int loc) {
        var r = LuaRef.MakeFrom(lua, loc);
        
        return r as LuaFunctionRef ?? throw new Exception("Tried to create LuaFunctionRef from a non-function value.");
    }

    public static LuaFunctionRef MakeFromString(Lua lua, string code) {
        lua.PCallStringThrowIfError(code, results: 1);
        var ret = MakeFrom(lua, lua.GetTop());
        lua.Pop(1);

        return ret;
    }

    public void InvokeVoid(params Span<object?> args) => InvokeVoid(lua: null, args);
    
    public void InvokeVoid(Lua? lua = null, params Span<object?> args) {
        lua ??= Lua;
        
        PushToStack(lua);
        foreach (var val in args) {
            lua.Push(val);
        }
        lua.PCallThrowIfError(arguments: args.Length, results: 0);
    }
    
    public object? Invoke(params Span<object?> args) => Invoke(lua: null, args);
    
    public object? Invoke(Lua? lua = null, params Span<object?> args) {
        lua ??= Lua;
        
        PushToStack(lua);
        foreach (var val in args) {
            lua.Push(val);
        }
        lua.PCallThrowIfError(arguments: args.Length, results: 1);

        var ret = lua.ToCSharp(lua.GetTop(), makeLuaFuncRefs: true);
        lua.Pop(1);

        return ret;
    }
}