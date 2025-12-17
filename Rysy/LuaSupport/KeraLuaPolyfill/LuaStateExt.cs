using KeraLua;
using System.Runtime.InteropServices;
using System.Text;

namespace Rysy.LuaSupport.KeraLuaPolyfill;

using LuaI = LuaNET.LuaJIT.Lua;

public static class LuaStateExt {
    extension(Lua lua) {
        public static Lua CreateNew(bool openLibs) {
            var st = LuaI.luaL_newstate();
            if (openLibs)
                LuaI.luaL_openlibs(st);

            return st;
        }
        
        public static Lua FromIntPtr(nint s) {
            unchecked {
                return new Lua { Handle = (nuint)s };
            }
        }

        public Encoding Encoding => Encoding.UTF8;
        
        #region Stack Manip
        public void Pop(int idx) {
            LuaI.lua_pop(lua, idx);
        }

        public void Remove(int idx) {
            LuaI.lua_remove(lua, idx);
        }
        
        public int GetTop() {
            return LuaI.lua_gettop(lua);
        }

        public LuaType TopType() {
            return (LuaType)LuaI.lua_type(lua, lua.GetTop());
        }

        public LuaType Type(int idx) {
            return (LuaType)LuaI.lua_type(lua, idx);
        }
        #endregion
        
        #region TableMethods
        public LuaType GetTable(int idx) {
            LuaI.lua_gettable(lua, idx);
            return lua.TopType();
        }

        public LuaType GetField(int idx, string fieldName) {
            LuaI.lua_getfield(lua, idx, fieldName);
            return lua.TopType();
        }
        
        public void SetField(int idx, string fieldName) {
            LuaI.lua_setfield(lua, idx, fieldName);
        }
        
        public bool GetMetaTable(int objIndex) {
            return LuaI.lua_getmetatable(lua, objIndex) != 0;
        }

        public LuaType GetMetaField(int idx, string name) {
            return (LuaType)LuaI.luaL_getmetafield(lua, idx, name);
        }
        
        public bool SetMetaTable(int objIndex) {
            return LuaI.lua_setmetatable(lua, objIndex) != 0;
        }

        public void Register(string name, KeraLuaStyleLuaFunction f) {
            lua.PushCFunction(f);
            lua.SetGlobal(name);
        }
        
        public void SetGlobal(string fieldName) {
            LuaI.lua_setglobal(lua, fieldName);
        }
        
        public void GetGlobal(string fieldName) {
            LuaI.lua_getglobal(lua, fieldName);
        }

        public void SetTable(int idx) {
            LuaI.lua_settable(lua, idx);
        }

        public LuaType RawGetInteger(int idx, int n) {
            LuaI.lua_rawgeti(lua, idx, n);
            return lua.TopType();
        }

        public bool Next(int idx) {
            return LuaI.lua_next(lua, idx) != 0;
        }
        #endregion

        public void Call(int args, int results) {
            LuaI.lua_call(lua, args, results);
        }

        public LuaStatus PCall(int args, int results, int errorFunctionIndex) {
            return (LuaStatus)LuaI.lua_pcall(lua, args, results, errorFunctionIndex);
        }
        
        #region IsMethods

        public bool IsNumber(int idx) {
            return LuaI.lua_isnumber(lua, idx) != 0;
        }
        
        #endregion

        #region ToMethods
        public string ToString(int stackIdx) {
            return LuaI.lua_tostring(lua, stackIdx) ?? "";
        }
        
        public double ToNumber(int stackIdx) {
            return LuaI.lua_tonumber(lua, stackIdx);
        }
        
        public long ToInteger(int stackIdx) {
            return LuaI.lua_tointeger(lua, stackIdx);
        }
        
        public long? ToIntegerX(int stackIdx) {
            int isnum = 0;
            var res = LuaI.lua_tointegerx(lua, stackIdx, ref isnum);
            if (isnum == 0)
                return null;
            return res;
        }
        
        public bool ToBoolean(int stackIdx) {
            return LuaI.lua_toboolean(lua, stackIdx) != 0;
        }
        
        public double ToNumberX(int stackIdx) {
            int isnum = 0;
            return LuaI.lua_tonumberx(lua, stackIdx, ref isnum);
        }
        
        public double ToNumberX(int stackIdx, out bool isNum) {
            int isnum = 0;
            var res = LuaI.lua_tonumberx(lua, stackIdx, ref isnum);
            isNum = isnum != 0;
            return res;
        }

        public nuint ToUserData(int idx) {
            return LuaI.lua_touserdata(lua, idx);
        }
        
        [DllImport("lua51", EntryPoint = "lua_tolstring", CallingConvention = (CallingConvention) 2)]
        private static extern nint _lua_tolstring(Lua L, int idx, ref ulong len);

        public nint ToLString(int idx, out ulong len) {
            len = 0;
            return _lua_tolstring(lua, idx, ref len);
        }
        
        #endregion
        
        #region PushMethods

        public void PushCopy(int idx) {
            LuaI.lua_pushvalue(lua, idx);
        }
        
        public void PushNil() {
            LuaI.lua_pushnil(lua);
        }
        
        public void PushBoolean(bool x) {
            LuaI.lua_pushboolean(lua, x ? 1 : 0);
        }
        
        public void PushNumber(double x) {
            LuaI.lua_pushnumber(lua, x);
        }
        
        public void PushInteger(long x) {
            LuaI.lua_pushinteger(lua, x);
        }

        public void PushString(string x) {
            LuaI.lua_pushstring(lua, x);
        }

        public void PushCFunction(LuaFunction f) {
            LuaI.lua_pushcfunction(lua, f);
        }
        
        public void PushCFunctionNew(LuaFunction f) {
            LuaI.lua_pushcfunction(lua, f);
        }
        
        public void PushCFunction(KeraLuaStyleLuaFunction f) {
            lua_pushcclosure(lua, f, 0);
        }
        
        [DllImport("lua51", EntryPoint = "lua_pushcclosure", CallingConvention = (CallingConvention) 2)]
        private static extern void _lua_pushcclosure(Lua L, IntPtr fn, int n);

        private static void lua_pushcclosure(Lua L, KeraLuaStyleLuaFunction? fn, int n)
        {
            Lua._lua_pushcclosure(L, fn == null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(fn), n);
        }
        #endregion
        
        #region NewMethods

        public nuint NewUserData(int size) {
            return LuaI.lua_newuserdata(lua, (ulong) size);
        }

        public void NewTable() {
            LuaI.lua_newtable(lua);
        }

        public void CreateTable(int narr, int nrec) {
            LuaI.lua_createtable(lua, narr, nrec);
        }
        #endregion
        
        #region LoadingMethods

        public LuaStatus LoadString(string code, string chunkName) {
            
            return (LuaStatus) LuaI.luaL_loadbuffer(lua, code, (uint)code.Length, chunkName);//LuaI.luaL_loadstring(lua, code);
        }

        public void DoString(string code) {
            LuaI.luaL_dostring(lua, code);
        }
        #endregion
        
        #region DebugMethods
        public void SetHook(LuaI.lua_Hook f, LuaHookMask mask, int count) {
            LuaI.lua_sethook(lua, f, (int) mask, count);
        }

        public void Error() {
            LuaI.lua_error(lua);
        }
        
        public void Error(string msg) {
            LuaI.luaL_error(lua, msg);
        }

        public LuaFunction? AtPanic(LuaFunction f) {
            return LuaI.lua_atpanic(lua, f);
        }

        public void GarbageCollector(LuaGC what, int n) {
            LuaI.lua_gc(lua, (int)what, n);
        }

        public void Traceback(Lua state, string msg, int level) {
            LuaI.luaL_traceback(lua, state,  msg, level);
        }
        #endregion
    }
}