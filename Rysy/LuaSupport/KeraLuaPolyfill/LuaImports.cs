using KeraLua;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Rysy.LuaSupport.KeraLuaPolyfill;

internal static unsafe partial class LuaImports {
    private const string LibraryName = "lua51";
    
    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_getfield(Lua lua, int idx, byte* k);
    
    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_pushlstring(Lua lua, byte* s, ulong len);
    
    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaL_newmetatable(Lua lua, byte* name);
    
    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial nint lua_tolstring(Lua lua, int idx, out ulong len);
    
    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial LuaStatus luaL_loadbufferx(Lua lua, byte* buff, nuint sz, string? name, string? mode);
    
    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_pushcclosure(Lua lua, IntPtr fn, int n);
}