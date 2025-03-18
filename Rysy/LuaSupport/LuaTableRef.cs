using KeraLua;
using Rysy.Helpers;
using System.Diagnostics.CodeAnalysis;

namespace Rysy.LuaSupport;

public sealed class LuaTableRef : LuaRef, IUntypedData {
    internal LuaTableRef(Lua lua, long id) : base(lua, id)
    {
    }
    
    public new static LuaTableRef MakeFrom(Lua lua, int loc) {
        var r = LuaRef.MakeFrom(lua, loc);
        
        return r as LuaTableRef ?? throw new Exception("Tried to create LuaFunctionRef from a non-table value.");
    }

    public bool TryGetValue(string key, [NotNullWhen(true)] out object? value) {
        PushToStack();
        value = Lua.PeekTableCSharpValue(Lua.GetTop(), key);
        Lua.Pop(1);
        return value is { };
    }

    public LuaTableRef? GetMetatable() {
        PushToStack();
        if (Lua.GetMetaTable(Lua.GetTop())) {
            var r = MakeFrom(Lua, Lua.GetTop());
            Lua.Pop(2);
            return r;
        }

        Lua.Pop(1);
        return null;
    }

    public object? this[string key] {
        get {
            PushToStack();
            var value = Lua.PeekTableCSharpValue(Lua.GetTop(), key);
            Lua.Pop(1);
            return value;
        }
        set {
            PushToStack();
            var tableLoc = Lua.GetTop();
            Lua.PushString(key);
            Lua.Push(value);
            Lua.SetTable(tableLoc);
            Lua.Pop(1);
        }
    }
}