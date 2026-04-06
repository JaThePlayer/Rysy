using KeraLua;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo

namespace Rysy.LuaSupport.KeraLuaPolyfill;

// Based on https://github.com/japajoe/LuaNET/blob/main/src/LuaNative.cs,
// but modified to perform Utf8 string marshaling.
/*
MIT License

Copyright (c) 2022 W.M.R Jap-A-Joe

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */
internal static unsafe partial class LuaImports {
    private const string LibraryName = "lua51";

    public const string LUAJIT_VERSION = "LuaJIT 2.1.0-beta3";
    public const int LUAJIT_VERSION_NUM = 20100;
    public const string LUAJIT_VERSION_SYM = "luaJIT_version_2_1_0_beta3";
    public const string LUAJIT_COPYRIGHT = "Copyright (C) 2005-2022 Mike Pall";
    public const string LUAJIT_URL = "https://luajit.org/";
    public const string LUA_FILEHANDLE = "FILE*";
    public const string LUA_COLIBNAME = "coroutine";
    public const string LUA_MATHLIBNAME = "math";
    public const string LUA_STRLIBNAME = "string";
    public const string LUA_TABLIBNAME = "table";
    public const string IOLIBNAME = "io";
    public const string OSLIBNAME = "os";
    public const string LOADLIBNAME = "package";
    public const string DBLIBNAME = "debug";
    public const string BITLIBNAME = "bit";
    public const string JITLIBNAME = "jit";
    public const string FFILIBNAME = "fii";
    public const string LUA_LDIR = "!\\lua\\";
    public const string LUA_CDIR = "!\\";
    public const string LUA_PATH_DEFAULT = ".\\?.lua;" + LUA_LDIR + "?.lua;" + LUA_LDIR + "?\\init.lua;";
    public const string LUA_CPATH_DEFAULT = ".\\?.dll;" + LUA_CDIR + "?.dll;" + LUA_CDIR + "loadall.dll";
    public const string LUA_PATH = "LUA_PATH";
    public const string LUA_CPATH = "LUA_CPATH";
    public const string LUA_INIT = "LUA_INIT";
    public const string LUA_DIRSEP = "\\";
    public const string LUA_PATHSEP = ";";
    public const string LUA_PATH_MARK = "?";
    public const string LUA_EXECDIR = "!";
    public const string LUA_IGMARK = "-";

    public const string LUA_PATH_CONFIG = LUA_DIRSEP + "\n" + LUA_PATHSEP + "\n" + LUA_PATH_MARK + "\n" +
                                          LUA_EXECDIR + "\n" + LUA_IGMARK + "\n";

    public const string LUA_QS = "'%s'";
    public const string LUA_VERSION = "Lua 5.1";
    public const string LUA_RELEASE = "Lua 5.1.4";
    public const string LUA_COPYRIGHT = "Copyright (C) 1994-2008 Lua.org, PUC-Rio";
    public const string LUA_AUTHORS = "R. Ierusalimschy, L. H. de Figueiredo, W. Celes";
    public const string LUA_SIGNATURE = "\eLua";

    public const int LUAJIT_MODE_MASK = 0x00FF;
    public const int LUAJIT_MODE_ENGINE = 0;
    public const int LUAJIT_MODE_DEBUG = 1;
    public const int LUAJIT_MODE_FUNC = 2;
    public const int LUAJIT_MODE_ALLFUNC = 3;
    public const int LUAJIT_MODE_ALLSUBFUNC = 4;
    public const int LUAJIT_MODE_TRACE = 5;
    public const int LUAJIT_MODE_WRAPCFUNC = 0x10;
    public const int LUAJIT_MODE_MODE_MAX = 0x11;
    public const int LUAJIT_MODE_OFF = 0x0000;
    public const int LUAJIT_MODE_ON = 0x0100;
    public const int LUAJIT_MODE_FLUSH = 0x0200;

    public const int LUAI_MAXSTACK = 65500;
    public const int LUAI_MAXCSTACK = 8000;
    public const int LUAI_GCPAUSE = 200;
    public const int LUAI_GCMUL = 200;
    public const int LUA_MAXCAPTURES = 32;
    public const int LUA_IDSIZE = 60;
    public const int LUAL_BUFFERSIZE = 512;
    public const int LUA_VERSION_NUM = 501;
    public const int LUA_MULTRET = -1;
    public const int LUA_REGISTRYINDEX = -10000;
    public const int LUA_ENVIRONINDEX = -10001;
    public const int LUA_GLOBALSINDEX = -10002;

    public const int LUA_OK = 0;
    public const int LUA_YIELD = 1;
    public const int LUA_ERRRUN = 2;
    public const int LUA_ERRSYNTAX = 3;
    public const int LUA_ERRMEM = 4;
    public const int LUA_ERRERR = 5;
    public const int LUA_TNONE = -1;

    public const int LUA_TNIL = 0;
    public const int LUA_TBOOLEAN = 1;
    public const int LUA_TLIGHTUSERDATA = 2;
    public const int LUA_TNUMBER = 3;
    public const int LUA_TSTRING = 4;
    public const int LUA_TTABLE = 5;
    public const int LUA_TFUNCTION = 6;
    public const int LUA_TUSERDATA = 7;
    public const int LUA_TTHREAD = 8;
    public const int LUA_MINSTACK = 20;

    public const int LUA_GCSTOP = 0;
    public const int LUA_GCRESTART = 1;
    public const int LUA_GCCOLLECT = 2;
    public const int LUA_GCCOUNT = 3;
    public const int LUA_GCCOUNTB = 4;
    public const int LUA_GCSTEP = 5;
    public const int LUA_GCSETPAUSE = 6;
    public const int LUA_GCSETSTEPMUL = 7;
    public const int LUA_GCISRUNNING = 9;

    public const int LUA_HOOKCALL = 0;
    public const int LUA_HOOKRET = 1;
    public const int LUA_HOOKLINE = 2;
    public const int LUA_HOOKCOUNT = 3;
    public const int LUA_HOOKTAILRET = 4;

    public const int LUA_MASKCALL = 1 << LUA_HOOKCALL;
    public const int LUA_MASKRET = 1 << LUA_HOOKRET;
    public const int LUA_MASKLINE = 1 << LUA_HOOKLINE;
    public const int LUA_MASKCOUNT = 1 << LUA_HOOKCOUNT;

    public const int LUA_NOREF = -2;
    public const int LUA_REFNIL = -1;

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_getfield(Lua lua, int idx, byte* k);

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_pushlstring(Lua lua, byte* s, ulong len);

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaL_newmetatable(Lua lua, byte* name);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial LuaStatus luaL_loadbufferx(Lua lua, byte* buff, nuint sz, string? name, string? mode);

    [LibraryImport(LibraryName)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_pushcclosure(Lua lua, IntPtr fn, int n);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Lua lua_newstate(LuaAlloc f, void* ud);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_close(Lua l);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Lua lua_newthread(Lua l);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial LuaFunction lua_atpanic(Lua l, LuaFunction panicf);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_gettop(Lua l);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_settop(Lua l, int idx);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_pushvalue(Lua l, int idx);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_remove(Lua l, int idx);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_insert(Lua l, int idx);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_replace(Lua l, int idx);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_checkstack(Lua l, int sz);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_xmove(Lua from, Lua to, int n);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_isnumber(Lua l, int idx);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_isstring(Lua l, int idx);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_iscfunction(Lua l, int idx);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_isuserdata(Lua l, int idx);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_type(Lua l, int idx);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* lua_typename(Lua l, int tp);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_equal(Lua l, int idx1, int idx2);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_rawequal(Lua l, int idx1, int idx2);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_lessthan(Lua l, int idx1, int idx2);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial double lua_tonumber(Lua l, int idx);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial long lua_tointeger(Lua l, int idx);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_toboolean(Lua l, int idx);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* lua_tolstring(Lua l, int idx, ref long len);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial ulong lua_objlen(Lua l, int idx);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial LuaFunction lua_tocfunction(Lua l, int idx);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void* lua_touserdata(Lua l, int idx);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Lua lua_tothread(Lua l, int idx);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void* lua_topointer(Lua l, int idx);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_pushnil(Lua l);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_pushnumber(Lua l, double n);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_pushinteger(Lua l, long n);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_pushlstring(Lua l, string s, long len);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_pushstring(Lua l, string s);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_pushcclosure(Lua l, LuaFunction fn, int n);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_pushboolean(Lua l, int b);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_pushlightuserdata(Lua l, void* p);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_pushthread(Lua l);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_gettable(Lua l, int idx);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_getfield(Lua l, int idx, string k);

    public static int lua_getfield_with_type(Lua l, int idx, string k) {
        lua_getfield(l, idx, k);
        return lua_type(l, -1);
    }

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_rawget(Lua l, int idx);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_rawgeti(Lua l, int idx, int n);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_createtable(Lua l, int narr, int nrec);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void* lua_newuserdata(Lua l, ulong sz);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_getmetatable(Lua l, int objindex);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_getfenv(Lua l, int idx);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_settable(Lua l, int idx);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_setfield(Lua l, int idx, string k);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_rawset(Lua l, int idx);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_rawseti(Lua l, int idx, int n);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_setmetatable(Lua l, int objindex);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_setfenv(Lua l, int idx);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_call(Lua l, int nargs, int nresults);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_pcall(Lua l, int nargs, int nresults, int errfunc);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_cpcall(Lua l, LuaFunction func, void* ud);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_load(Lua l, LuaReader reader, void* dt, string chunkname);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_dump(Lua l, LuaWriter writer, void* data);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_yield(Lua l, int nresults);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_resume(Lua l, int narg);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_status(Lua l);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_gc(Lua l, int what, int data);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_error(Lua l);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_next(Lua l, int idx);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_concat(Lua l, int n);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial LuaAlloc lua_getallocf(Lua l, ref void* ud);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_setallocf(Lua l, LuaAlloc f, void* ud);

    public static void lua_pop(Lua l, int n) {
        lua_settop(l, -n - 1);
    }

    public static void lua_newtable(Lua l) {
        lua_createtable(l, 0, 0);
    }

    public static void lua_register(Lua l, string n, LuaFunction f) {
        lua_pushcfunction(l, f);
        lua_setglobal(l, n);
    }

    public static void lua_pushcfunction(Lua l, LuaFunction f) {
        lua_pushcclosure(l, f, 0);
    }

    public static ulong lua_strlen(Lua l, int i) {
        return lua_objlen(l, i);
    }

    public static bool lua_isfunction(Lua l, int n) {
        return lua_type(l, n) == LUA_TFUNCTION;
    }

    public static bool lua_istable(Lua l, int n) {
        return lua_type(l, n) == LUA_TTABLE;
    }

    public static bool lua_islightuserdata(Lua l, int n) {
        return lua_type(l, n) == LUA_TLIGHTUSERDATA;
    }

    public static bool lua_isnil(Lua l, int n) {
        return lua_type(l, n) == LUA_TNIL;
    }

    public static bool lua_isboolean(Lua l, int n) {
        return lua_type(l, n) == LUA_TBOOLEAN;
    }

    public static bool lua_isthread(Lua l, int n) {
        return lua_type(l, n) == LUA_TTHREAD;
    }

    public static bool lua_isnone(Lua l, int n) {
        return lua_type(l, n) == LUA_TNONE;
    }

    public static bool lua_isnoneornil(Lua l, int n) {
        return lua_type(l, n) <= 0;
    }

    public static void lua_pushliteral(Lua l, string s) {
        lua_pushlstring(l, s, s.Length);
    }

    public static void lua_setglobal(Lua l, string s) {
        lua_setfield(l, LUA_GLOBALSINDEX, s);
    }

    public static void lua_getglobal(Lua l, string s) {
        lua_getfield(l, LUA_GLOBALSINDEX, s);
    }

    public static int lua_getglobal_with_type(Lua l, string s) {
        lua_getfield(l, LUA_GLOBALSINDEX, s);
        return lua_type(l, -1);
    }

    public static Lua lua_open() {
        return luaL_newstate();
    }

    public static void lua_getregistry(Lua l) {
        lua_pushvalue(l, LUA_REGISTRYINDEX);
    }

    public static int lua_getgccount(Lua l) {
        return lua_gc(l, LUA_GCCOUNT, 0);
    }

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_setlevel(Lua from, Lua to);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_getstack(Lua l, int level, LuaDebug ar);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_getinfo(Lua l, string what, LuaDebug ar);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* lua_getlocal(Lua l, LuaDebug ar, int n);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* lua_setlocal(Lua l, LuaDebug ar, int n);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* lua_getupvalue(Lua l, int funcindex, int n);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* lua_setupvalue(Lua l, int funcindex, int n);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_sethook(Lua l, LuaHook func, int mask, int count);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial LuaHook lua_gethook(Lua l);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_gethookmask(Lua l);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_gethookcount(Lua l);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void* lua_upvalueid(Lua l, int idx, int n);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_upvaluejoin(Lua l, int idx1, int n1, int idx2, int n2);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_loadx(Lua l, LuaReader reader, void* dt, string chunkname, string mode);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial double* lua_version(Lua l);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void lua_copy(Lua l, int fromidx, int toidx);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial double lua_tonumberx(Lua l, int idx, ref int isnum);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial long lua_tointegerx(Lua l, int idx, ref int isnum);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int lua_isyieldable(Lua l);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void luaL_openlib(Lua l, string libname, LuaLReg lreg, int nup);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void luaL_register(Lua l, string libname, LuaLReg lreg);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaL_getmetafield(Lua l, int obj, string e);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaL_callmeta(Lua l, int obj, string e);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaL_typerror(Lua l, int narg, string tname);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaL_argerror(Lua l, int numarg, string extramsg);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* luaL_checklstring(Lua l, int numArg, ref long len);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* luaL_optlstring(Lua l, int numArg, string def, ref long len);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial double luaL_checknumber(Lua l, int numArg);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial double luaL_optnumber(Lua l, int nArg, double def);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial long luaL_checkinteger(Lua l, int numArg);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial long luaL_optinteger(Lua l, int nArg, long def);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void luaL_checkstack(Lua l, int sz, string msg);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void luaL_checktype(Lua l, int narg, int t);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void luaL_checkany(Lua l, int narg);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaL_newmetatable(Lua l, string tname);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void* luaL_checkudata(Lua l, int ud, string tname);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void luaL_where(Lua l, int lvl);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaL_error(Lua l, string fmt, string args);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaL_checkoption(Lua l, int narg, string def, string[] lst);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaL_ref(Lua l, int t);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void luaL_unref(Lua l, int t, int _ref);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaL_loadfile(Lua l, string filename);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaL_loadbuffer(Lua l, string buff, long sz, string name);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaL_loadstring(Lua l, string s);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial Lua luaL_newstate();

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* luaL_gsub(Lua l, string s, string p, string r);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* luaL_findtable(Lua l, int idx, string fname, int szhint);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaL_fileresult(Lua l, int stat, string fname);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaL_execresult(Lua l, int stat);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaL_loadfilex(Lua l, string filename, string mode);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaL_loadbufferx(Lua l, string buff, long sz, string name, string mode);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void luaL_traceback(Lua l, Lua l1, string msg, int level);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void luaL_setfuncs(Lua l, LuaLReg[] lreg, int nup);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void luaL_pushmodule(Lua l, string modename, int sizehint);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void* luaL_testudata(Lua l, int ud, string tname);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void luaL_setmetatable(Lua l, string tname);

    public static void luaL_argcheck(Lua l, bool cond, int numarg, string extramsg) {
        if (!cond)
            luaL_argerror(l, numarg, extramsg);
    }

    public static byte* luaL_checkstring(Lua l, int n) {
        long temp = 0;
        return luaL_checklstring(l, n, ref temp);
    }

    public static byte* luaL_optstring(Lua l, int n, string d) {
        long temp = 0;
        return luaL_optlstring(l, n, d, ref temp);
    }

    public static int luaL_checkint(Lua l, int n) {
        return (int) luaL_checkinteger(l, n);
    }

    public static int luaL_optint(Lua l, int n, long d) {
        return (int) luaL_optinteger(l, n, d);
    }

    public static long luaL_checklong(Lua l, int n) {
        return luaL_checkinteger(l, n);
    }

    public static long luaL_optlong(Lua l, int n, long d) {
        return luaL_optinteger(l, n, d);
    }

    public static byte* luaL_typename(Lua l, int i) {
        return lua_typename(l, lua_type(l, i));
    }

    public static int luaL_dofile(Lua l, string fn) {
        int status = luaL_loadfile(l, fn);
        if (status > 0)
            return status;
        return lua_pcall(l, 0, LUA_MULTRET, 0);
    }

    public static int luaL_dostring(Lua l, string s) {
        int status = luaL_loadstring(l, s);
        if (status > 0)
            return status;
        return lua_pcall(l, 0, LUA_MULTRET, 0);
    }

    public static void luaL_getmetatable(Lua l, string n) {
        lua_getfield(l, LUA_REGISTRYINDEX, n);
    }

    public static T luaL_opt<T>(Lua l, LuaLFunction<T> f, int n, T d) {
        return lua_isnoneornil(l, n) ? d : f(l, n);
    }

    public static void luaL_newlibtable(Lua l, LuaLReg[] regs) {
        lua_createtable(l, 0, regs.Length - 1);
    }

    public static void luaL_newlib(Lua l, LuaLReg[] regs) {
        luaL_newlibtable(l, regs);
        luaL_setfuncs(l, regs, 0);
    }

    public static void luaL_addchar(LuaLBuffer b, byte c) {
        if (b.p >= (void*)(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(&b.buffer[0]).Length + LUAL_BUFFERSIZE))
            luaL_prepbuffer(b);
        *b.p = c;
        b.p += 1;
    }

    public static void luaL_putchar(LuaLBuffer b, byte c) {
        luaL_addchar(b, c);
    }

    public static void luaL_addsize(LuaLBuffer b, int n) {
        b.p += n;
    }

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void luaL_buffinit(Lua l, LuaLBuffer b);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial byte* luaL_prepbuffer(LuaLBuffer b);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void luaL_addlstring(LuaLBuffer b, string s, long l);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void luaL_addstring(LuaLBuffer b, string s);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void luaL_addvalue(LuaLBuffer b);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void luaL_pushresult(LuaLBuffer b);

    /// <summary>
    ///     COMPAT53_API
    /// </summary>
    public static void luaL_requiref(Lua l, string modname, LuaFunction openf, int glb) {
        luaL_checkstack(l, 3, "not enough stack slots available");

        luaL_getsubtable(l, LUA_REGISTRYINDEX, "_LOADED");

        if (lua_getfield_with_type(l, -1, modname) == LUA_TNIL) {
            lua_pop(l, 1);
            lua_pushcfunction(l, openf);
            lua_pushstring(l, modname);
            lua_call(l, 1, 1);
            lua_pushvalue(l, -1);
            lua_setfield(l, -3, modname);
        }

        if (glb > 0) {
            lua_pushvalue(l, -1);
            lua_setglobal(l, modname);
        }

        lua_replace(l, -2);
    }

    /// <summary>
    ///     COMPAT53_API
    /// </summary>
    public static int luaL_getsubtable(Lua l, int i, string name) {
        int abs_i = lua_absindex(l, i);
        luaL_checkstack(l, 3, "not enough stack slots");
        lua_pushstring(l, name);
        lua_gettable(l, abs_i);

        if (lua_istable(l, -1))
            return 1;

        lua_pop(l, 1);
        lua_newtable(l);
        lua_pushstring(l, name);
        lua_pushvalue(l, -2);
        lua_settable(l, abs_i);
        return 0;
    }

    /// <summary>
    ///     COMPAT53_API
    /// </summary>
    public static int lua_absindex(Lua l, int i) {
        if (i is < 0 and > LUA_REGISTRYINDEX)
            i += lua_gettop(l) + 1;
        return i;
    }

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaopen_base(Lua l);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaopen_math(Lua l);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaopen_string(Lua l);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaopen_table(Lua l);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaopen_io(Lua l);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaopen_os(Lua l);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaopen_package(Lua l);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaopen_debug(Lua l);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaopen_bit(Lua l);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaopen_jit(Lua l);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaopen_ffi(Lua l);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial int luaopen_string_buffer(Lua l);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    public static partial void luaL_openlibs(Lua l);
}
