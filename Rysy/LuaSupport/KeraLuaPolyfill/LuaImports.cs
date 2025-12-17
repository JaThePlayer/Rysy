using System.Runtime.InteropServices;

namespace Rysy.LuaSupport.KeraLuaPolyfill;

internal static unsafe class LuaImports {
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)]
    public static extern void lua_getfield(Lua lua, int idx, byte* k);
    
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)]
    public static extern void lua_pushlstring(Lua lua, byte* s, ulong len);
    
    
    [DllImport("lua51", CallingConvention = CallingConvention.Cdecl)]
    public static extern int luaL_newmetatable(Lua L, byte* tname);
}