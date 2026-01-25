using KeraLua;

namespace Rysy.LuaSupport.KeraLuaPolyfill;

public static class LuaStateExtUtf8 {
    extension(Lua lua) {
        public void GetGlobal(ReadOnlySpan<byte> fieldName) {
            unsafe {
                fixed(byte* x = fieldName)
                    LuaImports.lua_getfield(lua, -10002, x);
            }
        }
        
        public void PushBuffer(ReadOnlySpan<byte> buffer) {
            unsafe {
                fixed(byte* x = buffer)
                    LuaImports.lua_pushlstring(lua, x, (ulong)buffer.Length);
            }
        }
        
        public unsafe bool NewMetatable(ReadOnlySpan<byte> utf8Name) {
            fixed (byte* ptr = utf8Name) {
                return LuaImports.luaL_newmetatable(lua, ptr) != 0;
            }
        }
    
        public unsafe void GetMetatable(ReadOnlySpan<byte> utf8Name) {
            fixed (byte* ptr = utf8Name) {
                LuaImports.lua_getfield(lua, (int)LuaRegistry.Index, ptr);
            }
        }

        public unsafe LuaType GetFieldRva(int tableStackIdx, ReadOnlySpan<byte> fieldName) {
            fixed (byte* ptr = fieldName) {
                LuaImports.lua_getfield(lua, tableStackIdx, ptr);
                
                return lua.TopType();
            }
        }
    }
}