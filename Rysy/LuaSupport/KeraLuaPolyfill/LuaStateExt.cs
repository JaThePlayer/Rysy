using KeraLua;
using Rysy.Helpers;
using System.Runtime.InteropServices;
using System.Text;
using LuaType = KeraLua.LuaType;

namespace Rysy.LuaSupport.KeraLuaPolyfill;

public static class LuaStateExt {
    private static readonly Interpolator Interpolator = new Interpolator();

    private static unsafe string Utf8ToString(byte* ptr, long len) {
        return Encoding.UTF8.GetString(ptr, checked((int)len));
    }
    
    extension(Lua lua) {
        public static Lua CreateNew(bool openLibs) {
            var st = LuaImports.luaL_newstate();
            if (openLibs)
                LuaImports.luaL_openlibs(st);

            return st;
        }
        
        public static Lua FromIntPtr(nint s) {
            return new Lua(s);
        }

        public Encoding Encoding => Encoding.UTF8;
        
        #region Stack Manip
        public void Pop(int idx) {
            LuaImports.lua_pop(lua, idx);
        }

        /// <summary>
        /// Removes the element at the given valid index, shifting down the elements above this index to fill the gap.
        /// Cannot be called with a pseudo-index, because a pseudo-index is not an actual stack position.
        /// </summary>
        public void Remove(int idx) {
            LuaImports.lua_remove(lua, idx);
        }
        
        public int GetTop() {
            return LuaImports.lua_gettop(lua);
        }

        public LuaType TopType() {
            return (LuaType)LuaImports.lua_type(lua, lua.GetTop());
        }

        public LuaType Type(int idx) {
            return (LuaType)LuaImports.lua_type(lua, idx);
        }
        
        /// <summary>
        /// Moves the top element into the given valid index, shifting up the elements above this index to open space.
        /// Cannot be called with a pseudo-index, because a pseudo-index is not an actual stack position.
        /// </summary>
        public void Insert(int targetIdx) {
            LuaImports.lua_insert(lua, targetIdx);
        }
        #endregion
        
        #region TableMethods
        public LuaType GetTable(int idx) {
            LuaImports.lua_gettable(lua, idx);
            return lua.TopType();
        }

        public LuaType GetField(int idx, string fieldName) {
            LuaImports.lua_getfield(lua, idx, fieldName);
            return lua.TopType();
        }
        
        public void SetField(int idx, string fieldName) {
            LuaImports.lua_setfield(lua, idx, fieldName);
        }
        
        public bool GetMetaTable(int objIndex) {
            return LuaImports.lua_getmetatable(lua, objIndex) != 0;
        }

        public LuaType GetMetaField(int idx, string name) {
            return (LuaType)LuaImports.luaL_getmetafield(lua, idx, name);
        }
        
        public bool SetMetaTable(int objIndex) {
            return LuaImports.lua_setmetatable(lua, objIndex) != 0;
        }

        public void Register(string name, KeraLuaStyleLuaFunction f) {
            lua.PushCFunction(f);
            lua.SetGlobal(name);
        }
        
        public void SetGlobal(string fieldName) {
            LuaImports.lua_setglobal(lua, fieldName);
        }
        
        public void GetGlobal(string fieldName) {
            LuaImports.lua_getglobal(lua, fieldName);
        }

        public void SetTable(int idx) {
            LuaImports.lua_settable(lua, idx);
        }

        public LuaType RawGetInteger(int idx, int n) {
            LuaImports.lua_rawgeti(lua, idx, n);
            return lua.TopType();
        }

        public bool Next(int idx) {
            return LuaImports.lua_next(lua, idx) != 0;
        }
        #endregion

        public void Call(int args, int results) {
            LuaImports.lua_call(lua, args, results);
        }

        public LuaStatus PCall(int args, int results, int errorFunctionIndex) {
            return (LuaStatus)LuaImports.lua_pcall(lua, args, results, errorFunctionIndex);
        }
        
        #region IsMethods

        public bool IsNumber(int idx) {
            return LuaImports.lua_isnumber(lua, idx) != 0;
        }
        
        #endregion

        #region ToMethods
        public string ToString(int stackIdx) {
            return lua.FastToString(stackIdx);
        }
        
        public double ToNumber(int stackIdx) {
            return LuaImports.lua_tonumber(lua, stackIdx);
        }
        
        public long ToInteger(int stackIdx) {
            return LuaImports.lua_tointeger(lua, stackIdx);
        }
        
        public long? ToIntegerX(int stackIdx) {
            int isnum = 0;
            var res = LuaImports.lua_tointegerx(lua, stackIdx, ref isnum);
            if (isnum == 0)
                return null;
            return res;
        }
        
        public bool ToBoolean(int stackIdx) {
            return LuaImports.lua_toboolean(lua, stackIdx) != 0;
        }
        
        public double ToNumberX(int stackIdx) {
            int isnum = 0;
            return LuaImports.lua_tonumberx(lua, stackIdx, ref isnum);
        }
        
        public double ToNumberX(int stackIdx, out bool isNum) {
            int isnum = 0;
            var res = LuaImports.lua_tonumberx(lua, stackIdx, ref isnum);
            isNum = isnum != 0;
            return res;
        }

        public nuint ToUserData(int idx) {
            unsafe
            {
                return (nuint)LuaImports.lua_touserdata(lua, idx);
            }
        }
        
        #endregion
        
        #region PushMethods

        public void PushCopy(int idx) {
            LuaImports.lua_pushvalue(lua, idx);
        }
        
        public void PushNil() {
            LuaImports.lua_pushnil(lua);
        }
        
        public void PushBoolean(bool x) {
            LuaImports.lua_pushboolean(lua, x ? 1 : 0);
        }
        
        public void PushNumber(double x) {
            LuaImports.lua_pushnumber(lua, x);
        }
        
        public void PushInteger(long x) {
            LuaImports.lua_pushinteger(lua, x);
        }

        public void PushString(string x) {
            LuaImports.lua_pushstring(lua, x);
        }

        public void PushCFunction(LuaFunction f) {
            LuaImports.lua_pushcfunction(lua, f);
        }
        
        public void PushCFunction(KeraLuaStyleLuaFunction f) {
            lua_pushcclosure(lua, f, 0);
        }

        private static void lua_pushcclosure(Lua L, KeraLuaStyleLuaFunction? fn, int n)
        {
            LuaImports.lua_pushcclosure(L, fn == null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(fn), n);
        }
        #endregion
        
        #region NewMethods

        public nuint NewUserData(int size) {
            unsafe
            {
                return (nuint)LuaImports.lua_newuserdata(lua, (ulong) size);
            }
        }

        public void NewTable() {
            LuaImports.lua_newtable(lua);
        }

        public Lua NewThread() {
            return LuaImports.lua_newthread(lua);
        }

        public void CreateTable(int narr, int nrec) {
            LuaImports.lua_createtable(lua, narr, nrec);
        }
        #endregion
        
        #region CheckMethods
        public string CheckLString(int idx, out long len) {
            unsafe
            {
                len = 0;
                var ret = LuaImports.luaL_checklstring(lua, 1, ref len);
                return Utf8ToString(ret, len);
            }
        }
        
        public long CheckInteger(int idx) {
            return LuaImports.luaL_checkinteger(lua, idx);
        }

        public long OptInteger(int nArg, long def) {
            return LuaImports.luaL_optinteger(lua, nArg, def);
        }

        public void ArgCheck(bool cond, int argNumber, string msg) {
            LuaImports.luaL_argcheck(lua, cond, argNumber, msg);
        }

        public void CheckStack(int n, string msg) {
            LuaImports.luaL_checkstack(lua, n, msg);
        }
        #endregion
        
        #region LoadingMethods

        public LuaStatus LoadString(string code, string chunkName) {
            unsafe
            {
                var codeUtf8 = Interpolator.Utf8($"{code}");
                fixed (byte* codePtr = &codeUtf8[0])
                    return LuaImports.luaL_loadbufferx(lua, codePtr, (nuint)codeUtf8.Length, chunkName, null);
            }
        }

        public void DoString(string code) {
            LuaImports.luaL_dostring(lua, code);
        }
        #endregion
        
        #region DebugMethods
        public void SetHook(LuaHook f, LuaHookMask mask, int count) {
            LuaImports.lua_sethook(lua, f, (int) mask, count);
        }

        public int Error() {
            return LuaImports.lua_error(lua);
        }
        
        public int Error(string msg) {
            return LuaImports.luaL_error(lua, msg, "");
        }

        public LuaFunction? AtPanic(LuaFunction f) {
            return LuaImports.lua_atpanic(lua, f);
        }

        public void GarbageCollector(LuaGC what, int n) {
            LuaImports.lua_gc(lua, (int)what, n);
        }

        public void Traceback(Lua state, string msg, int level) {
            LuaImports.luaL_traceback(state, state,  msg, level);
        }
        #endregion
    }
}