using KeraLua;
using LuaNET;
using System.Runtime.InteropServices;
using System.Text;
using LuaType = KeraLua.LuaType;

namespace Rysy.LuaSupport.KeraLuaPolyfill;

using LuaI = LuaNET.Lua;

public static class LuaStateExt {
    extension(Lua lua) {
        public static Lua CreateNew(bool openLibs) {
            var st = LuaI.NewState();
            if (openLibs)
                LuaI.OpenLibs(st);

            return st;
        }
        
        public static Lua FromIntPtr(nint s) {
            return new Lua { pointer = s };
        }

        public Encoding Encoding => Encoding.UTF8;
        
        #region Stack Manip
        public void Pop(int idx) {
            LuaI.Pop(lua, idx);
        }

        /// <summary>
        /// Removes the element at the given valid index, shifting down the elements above this index to fill the gap.
        /// Cannot be called with a pseudo-index, because a pseudo-index is not an actual stack position.
        /// </summary>
        public void Remove(int idx) {
            LuaI.Remove(lua, idx);
        }
        
        public int GetTop() {
            return LuaI.GetTop(lua);
        }

        public LuaType TopType() {
            return (LuaType)LuaI.Type(lua, lua.GetTop());
        }

        public LuaType Type(int idx) {
            return (LuaType)LuaI.Type(lua, idx);
        }
        
        /// <summary>
        /// Moves the top element into the given valid index, shifting up the elements above this index to open space.
        /// Cannot be called with a pseudo-index, because a pseudo-index is not an actual stack position.
        /// </summary>
        public void Insert(int targetIdx) {
            LuaI.Insert(lua, targetIdx);
        }
        #endregion
        
        #region TableMethods
        public LuaType GetTable(int idx) {
            LuaI.GetTable(lua, idx);
            return lua.TopType();
        }

        public LuaType GetField(int idx, string fieldName) {
            LuaI.GetField(lua, idx, fieldName);
            return lua.TopType();
        }
        
        public void SetField(int idx, string fieldName) {
            LuaI.SetField(lua, idx, fieldName);
        }
        
        public bool GetMetaTable(int objIndex) {
            return LuaI.GetMetaTable(lua, objIndex) != 0;
        }

        public LuaType GetMetaField(int idx, string name) {
            return (LuaType)LuaI.GetMetaField(lua, idx, name);
        }
        
        public bool SetMetaTable(int objIndex) {
            return LuaI.SetMetaTable(lua, objIndex) != 0;
        }

        public void Register(string name, KeraLuaStyleLuaFunction f) {
            lua.PushCFunction(f);
            lua.SetGlobal(name);
        }
        
        public void SetGlobal(string fieldName) {
            LuaI.SetGlobal(lua, fieldName);
        }
        
        public void GetGlobal(string fieldName) {
            LuaI.GetGlobal(lua, fieldName);
        }

        public void SetTable(int idx) {
            LuaI.SetTable(lua, idx);
        }

        public LuaType RawGetInteger(int idx, int n) {
            LuaI.RawGetI(lua, idx, n);
            return lua.TopType();
        }

        public bool Next(int idx) {
            return LuaI.Next(lua, idx) != 0;
        }
        #endregion

        public void Call(int args, int results) {
            LuaI.Call(lua, args, results);
        }

        public LuaStatus PCall(int args, int results, int errorFunctionIndex) {
            return (LuaStatus)LuaI.PCall(lua, args, results, errorFunctionIndex);
        }
        
        #region IsMethods

        public bool IsNumber(int idx) {
            return LuaI.IsNumber(lua, idx);
        }
        
        #endregion

        #region ToMethods
        public string ToString(int stackIdx) {
            return LuaI.ToString(lua, stackIdx) ?? "";
        }
        
        public double ToNumber(int stackIdx) {
            return LuaI.ToNumber(lua, stackIdx);
        }
        
        public long ToInteger(int stackIdx) {
            return LuaI.ToInteger(lua, stackIdx);
        }
        
        public long? ToIntegerX(int stackIdx) {
            int isnum = 0;
            var res = LuaI.ToIntegerX(lua, stackIdx, ref isnum);
            if (isnum == 0)
                return null;
            return res;
        }
        
        public bool ToBoolean(int stackIdx) {
            return LuaI.ToBoolean(lua, stackIdx);
        }
        
        public double ToNumberX(int stackIdx) {
            int isnum = 0;
            return LuaI.ToNumberX(lua, stackIdx, ref isnum);
        }
        
        public double ToNumberX(int stackIdx, out bool isNum) {
            int isnum = 0;
            var res = LuaI.ToNumberX(lua, stackIdx, ref isnum);
            isNum = isnum != 0;
            return res;
        }

        public nuint ToUserData(int idx) {
            return (nuint)LuaI.ToUserData(lua, idx);
        }

        public nint ToLString(int idx, out ulong len) {
            return LuaImports.lua_tolstring(lua, idx, out len);
        }
        
        #endregion
        
        #region PushMethods

        public void PushCopy(int idx) {
            LuaI.PushValue(lua, idx);
        }
        
        public void PushNil() {
            LuaI.PushNil(lua);
        }
        
        public void PushBoolean(bool x) {
            LuaI.PushBoolean(lua, x);
        }
        
        public void PushNumber(double x) {
            LuaI.PushNumber(lua, x);
        }
        
        public void PushInteger(long x) {
            LuaI.PushInteger(lua, x);
        }

        public void PushString(string x) {
            LuaI.PushString(lua, x);
        }

        public void PushCFunction(LuaFunction f) {
            LuaI.PushCFunction(lua, f);
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
            return (nuint)LuaI.NewUserData(lua, (ulong) size);
        }

        public void NewTable() {
            LuaI.NewTable(lua);
        }

        public Lua NewThread() {
            return LuaI.NewThread(lua);
        }

        public void CreateTable(int narr, int nrec) {
            LuaI.CreateTable(lua, narr, nrec);
        }
        #endregion
        
        #region CheckMethods
        public string CheckLString(int idx, out ulong len) {
            len = 0;
            return LuaI.CheckLString(lua, 1, ref len);
        }
        
        public long CheckInteger(int idx) {
            return LuaI.CheckInteger(lua, idx);
        }

        public long OptInteger(int nArg, long def) {
            return LuaI.OptInteger(lua, nArg, def);
        }

        public void ArgCheck(bool cond, int argNumber, string msg) {
            LuaI.ArgCheck(lua, cond, argNumber, msg);
        }

        public void CheckStack(int n, string msg) {
            LuaNative.luaL_checkstack(lua, n, msg);
        }
        #endregion
        
        #region LoadingMethods

        public LuaStatus LoadString(string code, string chunkName) {
            return (LuaStatus) LuaI.LoadBuffer(lua, code, (uint)code.Length, chunkName);
        }

        public void DoString(string code) {
            LuaI.DoString(lua, code);
        }
        #endregion
        
        #region DebugMethods
        public void SetHook(LuaHook f, LuaHookMask mask, int count) {
            LuaI.SetHook(lua, f, (int) mask, count);
        }

        public int Error() {
            return LuaI.Error(lua);
        }
        
        public int Error(string msg) {
            return LuaI.Error(lua, msg, "");
        }

        public LuaFunction? AtPanic(LuaFunction f) {
            return LuaI.AtPanic(lua, f);
        }

        public void GarbageCollector(LuaGC what, int n) {
            LuaI.GC(lua, (LuaGCParam)what, n);
        }

        public void Traceback(Lua state, string msg, int level) {
            LuaI.TraceBack(state, state,  msg, level);
        }
        #endregion
    }
}