using KeraLua;

namespace Rysy.LuaSupport;

public sealed class LonnFieldType {
    public string Name { get; private set; }
    
    public LuaFunctionRef GetElement { get; private set; }

    public LonnFieldType(LuaCtx ctx) {
        var lua = ctx.Lua;
        var top = lua.GetTop();
        
        if (lua.Type(top) != LuaType.Table) {
            throw new Exception($"File did not return a table");
        }
        
        Name = lua.PeekTableStringValue(top, "fieldType"u8)  ?? throw new Exception("Field type does not have a `fieldType` string value");
        GetElement = lua.PeekTableFunctionValue(top, "getElement"u8) ?? throw new Exception("Field type does not have a `getElement` function");
    }
}