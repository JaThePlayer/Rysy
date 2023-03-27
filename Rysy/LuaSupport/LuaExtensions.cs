﻿using KeraLua;
using Rysy.Extensions;
using Rysy.Helpers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Rysy.LuaSupport;

public static partial class LuaExt {
    /// <summary>
    /// Converts the Lua value at the given index to a C# string
    /// </summary>
    public static unsafe string FastToString(this Lua state, int index, bool callMetamethod = true) {
        UIntPtr num;
        IntPtr source;
        if (callMetamethod) {
            source = luaL_tolstring(state.Handle, index, out num);
            state.Pop(1);
        } else {
            source = lua_tolstring(state.Handle, index, out num);
        }

        if (source == IntPtr.Zero)
            return null!;

        // todo: check if all these casts are needed?
        int length = checked((int) (uint) num);
        if (length == 0)
            return "";

        if (length == 1) {
            var b = ((byte*) source)[0];
            switch (b) {
                case (byte) 'x':
                    return "x";
                case (byte) 'y':
                    return "y";
                default:
                    break;
            }
        }

        var str = state.Encoding.GetString((byte*) source, length);

        return str;
    }


    public static char[] SharedToStringBuffer = new char[4098];

    public static unsafe Span<char> ToStringInto(this Lua state, int index, Span<char> buffer, bool callMetamethod = true) {
        UIntPtr num;
        IntPtr source;
        if (callMetamethod) {
            source = luaL_tolstring(state.Handle, index, out num);
            state.Pop(1);
        } else {
            source = lua_tolstring(state.Handle, index, out num);
        }

        if (source == IntPtr.Zero)
            return Span<char>.Empty;

        // todo: check if all these casts are needed?
        int length = checked((int) (uint) num);
        if (length == 0) {
            return Span<char>.Empty;
        }

        /*
        if (length == 1) {
            var b = ((byte*) source)[0];
            buffer[0] = (char)b;
            return buffer;
        }*/

        var decoded = state.Encoding.GetChars(new Span<byte>((void*) source, length), buffer);

        return buffer[..decoded];
    }

    /// <summary>
    /// Pushes an ASCII string onto the stack
    /// </summary>
    public static void PushASCIIString(this Lua lua, byte[] value) {
        if (value == null) {
            lua.PushNil();
            return;
        }

        lua.PushBuffer(value);
    }

    public static void LoadStringWithSelene(this Lua lua, string str, string? chunkName = null) {
        lua.GetGlobal("selene");
        var seleneLoc = lua.GetTop();
        lua.PushString("parse");
        lua.GetTable(seleneLoc);

        lua.PushString(str);
        lua.Call(1, 1); // call selene.parse(arg)

        var code = lua.FastToString(-1);
        lua.Pop(2);

        var st = lua.LoadString(code, chunkName ?? str);
        if (st != LuaStatus.OK) {
            throw new LuaException(lua);
        }
    }

    /// <summary>
    /// Calls <see cref="LoadStringWithSelene(Lua, string, string?)"/> with <paramref name="code"/> and <paramref name="chunkName"/>, then calls <see cref="PCallThrowIfError(Lua, int, int, int)"/>
    /// </summary>
    /// <param name="lua"></param>
    /// <param name="code"></param>
    /// <param name="arguments"></param>
    /// <param name="results"></param>
    /// <param name="errorFunctionIndex"></param>
    public static void PCallStringThrowIfError(this Lua lua, string code, string? chunkName = null, int arguments = 0, int results = 0, int errorFunctionIndex = 0) {
        lua.LoadStringWithSelene(code, chunkName);
        lua.PCallThrowIfError(arguments, results, errorFunctionIndex);
    }

    /// <summary>
    /// Calls <see cref="Lua.PCall(int, int, int)"/>, throwing a <see cref="LuaException"/> if the call failed.
    /// </summary>
    /// <exception cref="LuaException"></exception>
    public static void PCallThrowIfError(this Lua lua, int arguments = 0, int results = 0, int errorFunctionIndex = 0) {
        var result = lua.PCall(arguments, results, errorFunctionIndex);
        if (result != LuaStatus.OK) {
            var ex = new LuaException(lua);
            lua.Pop(1);
            throw ex;
        }
    }

    public static void PrintStack(this Lua state,
        [CallerMemberName] string callerMethod = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0
    ) {
        Logger.Write("Lua", LogLevel.Info, "Stack:", callerMethod, callerFile, lineNumber);
        for (int i = 1; i <= state.GetTop(); i++) {
            Console.WriteLine($"[{i}]: {state.FastToString(i)}");
        }
    }

    /// <summary>
    /// Peeks the type of the value at t[key], where t is the table at <paramref name="tableStackIndex"/>
    /// </summary>
    public static LuaType PeekTableType(this Lua lua, int tableStackIndex, string key) {
        lua.PushString(key);
        var type = lua.GetTable(tableStackIndex);
        lua.Pop(1);

        return type;
    }

    /// <summary>
    /// Peeks the string value at t[key], where t is the table at <paramref name="tableStackIndex"/>
    /// </summary>
    public static string? PeekTableStringValue(this Lua lua, int tableStackIndex, string key) {
        lua.PushString(key);
        var type = lua.GetTable(tableStackIndex);
        string? ret = null;
        if (type == LuaType.String) {
            ret = lua.FastToString(lua.GetTop());
        }
        lua.Pop(1);

        return ret;
    }

    /// <summary>
    /// Peeks the number value at t[key], where t is the table at <paramref name="tableStackIndex"/>
    /// </summary>
    public static double? PeekTableNumberValue(this Lua lua, int tableStackIndex, string key) {
        lua.PushString(key);
        var type = lua.GetTable(tableStackIndex);
        double? ret = null;
        if (type == LuaType.Number) {
            ret = lua.ToNumberX(lua.GetTop());
        }
        lua.Pop(1);

        return ret;
    }

    public static float? PeekTableFloatValue(this Lua lua, int tableStackIndex, string key) {
        return PeekTableNumberValue(lua, tableStackIndex, key) is double d ? (float) d : null;
    }

    /// <summary>
    /// Peeks the number value at t[key], where t is the table at <paramref name="tableStackIndex"/>
    /// </summary>
    public static double? PeekTableNumberValue(this Lua lua, int tableStackIndex, int key) {
        lua.PushInteger(key);
        var type = lua.GetTable(tableStackIndex);
        double? ret = null;
        if (type == LuaType.Number) {
            ret = lua.ToNumberX(lua.GetTop());
        }
        lua.Pop(1);

        return ret;
    }

    /// <summary>
    /// Peeks the int value at t[key], where t is the table at <paramref name="tableStackIndex"/>.
    /// </summary>
    public static int? PeekTableIntValue(this Lua lua, int tableStackIndex, string key) {
        lua.PushString(key);
        var type = lua.GetTable(tableStackIndex);
        long? ret = null;
        if (type is LuaType.Number) {
            ret = lua.ToIntegerX(lua.GetTop());
        }
        lua.Pop(1);

        return ret is { } r ? (int)r : null;
    }

    public static Vector2 PeekTableVector2Value(this Lua lua, int tableStackIndex, string key) {
        lua.PushString(key);
        var type = lua.GetTable(tableStackIndex);
        Vector2 ret = default;
        if (type is LuaType.Table) {
            ret = lua.ToVector2(lua.GetTop());
        }
        lua.Pop(1);

        return ret;
    }

    /// <summary>
    /// Peeks a value at <paramref name="tableStackIndex"/>[<paramref name="key"/>], converting it to a range using <see cref="ToRangeNegativeIsFromEnd(Lua, int)"/>
    /// </summary>
    public static Range PeekTableRangeValue(this Lua lua, int tableStackIndex, string key) {
        lua.PushString(key);
        var type = lua.GetTable(tableStackIndex);
        Range ret = default;
        if (type is LuaType.Table) {
            ret = lua.ToRangeNegativeIsFromEnd(lua.GetTop());
        }
        lua.Pop(1);

        return ret;
    }

    public static Color PeekTableColorValue(this Lua lua, int tableStackIndex, string key, Color def) {
        lua.PushString(key);
        var type = lua.GetTable(tableStackIndex);
        Color ret = def;
        if (type is LuaType.Table or LuaType.String) {
            ret = lua.ToColor(lua.GetTop());
        }
        lua.Pop(1);

        return ret;
    }

    public static List<float>? PeekTableNumberList(this Lua lua, int tableStackIndex, string key) {
        lua.PushString(key);
        var type = lua.GetTable(tableStackIndex);

        if (type != LuaType.Table) {
            return null;
        }

        var list = new List<float>();

        lua.IPairs((lua, index, loc) => {
            list.Add((float)lua.ToNumber(loc));
        });

        lua.Pop(1);

        return list;
    }

    /// <summary>
    /// Pushes t[<paramref name="key"/>], where t is on the stack at <paramref name="index"/>
    /// </summary>
    public static LuaType GetTable(this Lua lua, int index, string key) {
        //lua.PushString(key);
        //return lua.GetTable(index);
        return lua.GetField(index, key);
    }

    /// <summary>
    /// Pushes t[<paramref name="key"/>], where t is on the stack at <paramref name="index"/>
    /// </summary>
    public static LuaType GetTable(this Lua lua, int index, byte[] keyASCII) {
        lua.PushASCIIString(keyASCII);
        return lua.GetTable(index);
    }

    public static void Push(this Lua lua, object? obj) {
        switch (obj) {
            case null:
                lua.PushNil();
                break;
            case short s:
                lua.PushInteger(s);
                break;
            case ushort s:
                lua.PushInteger(s);
                break;
            case uint u:
                lua.PushInteger(u);
                break;
            case int i:
                lua.PushInteger(i);
                break;
            case ulong ul:
                lua.PushInteger((long) ul);
                break;
            case long l:
                lua.PushInteger(l);
                break;
            case float f:
                lua.PushNumber(f);
                break;
            case double d:
                lua.PushNumber(d);
                break;
            case string str:
                lua.PushString(str);
                break;
            case bool b:
                lua.PushBoolean(b);
                break;
            case ILuaWrapper wrapper:
                lua.PushWrapper(wrapper);
                break;
            default:
                throw new Exception($"Can't push {obj} [{obj.GetType()}] to Lua");
        }
    }

    /// <summary>
    /// Calls the function on top of the stack with the given arguments, returning the value returned by that function. The return value is popped from the stack.
    /// </summary>
    public static TOut? CallFunction<TArg1, TArg2, TOut>(this Lua lua, TArg1 arg1, TArg2 arg2, Func<Lua, int, TOut?> retGetter, int results = 1)
    where TArg1 : class, ILuaWrapper
    where TArg2 : class, ILuaWrapper {
        TOut? ret;

        lua.PushWrapper(arg1);
        lua.PushWrapper(arg2);

        lua.Call(2, results);
        ret = retGetter(lua, lua.GetTop());
        lua.Pop(results);

        ClearWrappers();

        return ret;
    }

    public static TOut? PCallFunction<TArg1, TArg2, TOut>(this Lua lua, TArg1 arg1, TArg2 arg2, Func<Lua, int, TOut?> retGetter, int results = 1)
where TArg1 : class, ILuaWrapper
where TArg2 : class, ILuaWrapper {
        TOut? ret;

        lua.PushWrapper(arg1);
        lua.PushWrapper(arg2);

        var result = lua.PCall(2, results, 0);
        if (result != LuaStatus.OK) {
            var ex = new LuaException(lua);
            lua.Pop(results);
            throw ex;
        }

        ret = retGetter(lua, lua.GetTop());
        lua.Pop(results);

        ClearWrappers();

        return ret;
    }

    public static TOut? PCallFunction<TArg1, TOut>(this Lua lua, TArg1 arg1, Func<Lua, int, TOut?> retGetter, int results = 1)
where TArg1 : class, ILuaWrapper {
        TOut? ret;

        lua.PushWrapper(arg1);

        var result = lua.PCall(1, results, 0);
        if (result != LuaStatus.OK) {
            var ex = new LuaException(lua);
            lua.Pop(results);
            throw ex;
        }

        ret = retGetter(lua, lua.GetTop());
        lua.Pop(results);

        ClearWrappers();

        return ret;
    }

    /// <summary>
    /// Converts a lua table at index <paramref name="index"/> on the stack into a C# dictionary
    /// </summary>
    /// <param name="lua"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static Dictionary<string, object> TableToDictionary(this Lua lua, int index, HashSet<string>? keyBlacklist = null, int depth = 0) {
        var dict = new Dictionary<string, object>();
        var dataStart = index;

        lua.PushNil();
        while (lua.Next(dataStart)) {
            var key = lua.FastToString(-2);
            if (keyBlacklist?.Contains(key) ?? false) {
                goto next;
            }

            var value = ToCSharpSimple(lua, lua.GetTop(), depth: depth);
            dict[key] = value;

            next:
            // pop the value, keeping the key
            lua.Pop(1);
        }

        return dict;
    }


    /// <summary>
    /// Enumerates through an int-indexed table, calling <paramref name="onElement"/> for each element with (lua, index, valueLocation)
    /// </summary>
    /// <param name="lua"></param>
    /// <param name="onElement">(lua, index, valueLocation)</param>
    public static void IPairs(this Lua lua, Action<Lua, int, int> onElement) {
        for (int i = 1; ; i++) {
            var t = lua.RawGetInteger(-1, i);
            if (t == LuaType.Nil) {
                lua.Pop(1);
                break;
            }

            onElement(lua, i, lua.GetTop());

            lua.Pop(1);
        }
    }

    public static Vector2 ToVector2(this Lua lua, int index) {
        switch (lua.Type(index)) {
            case LuaType.Table:
                return new((float) lua.PeekTableNumberValue(index, 1)!, (float) lua.PeekTableNumberValue(index, 2)!);
            case LuaType.Number:
                if (lua.IsNumber(index - 1)) {
                    return new((float) lua.ToNumber(index - 1), (float) lua.ToNumber(index));
                } else {
                    return new((float) lua.ToNumber(index));
                }

            default:
                return default;
        };
    }

    public static Point ToPoint(this Lua lua, int index) {
        var vec = lua.ToVector2(index);

        return vec.ToPoint();
    }

    /// <summary>
    /// Turns the lua value on the stack at <paramref name="index"/> to a range.
    /// A negative value for the 2nd number gets converted to <see cref="Index.End"/>
    /// </summary>
    public static Range ToRangeNegativeIsFromEnd(this Lua lua, int index) {
        var point = lua.ToPoint(index);

        return new(point.X.AtLeast(0), point.Y < 0 ? Index.End : point.Y);
    }

    public static Color ToColor(this Lua lua, int index) {
        switch (lua.Type(index)) {
            case LuaType.Table:
                var a = (float) (lua.PeekTableNumberValue(index, 4) ?? 1f);
                return new Color(
                    (float) (lua.PeekTableNumberValue(index, 1) ?? 1f),
                    (float) (lua.PeekTableNumberValue(index, 2) ?? 1f),
                    (float) (lua.PeekTableNumberValue(index, 3) ?? 1f)
                 ) * a;
            case LuaType.String:
                return ColorHelper.RGBA(lua.FastToString(index));
            default:
                return default;
        };
    }

    internal static object ToCSharpSimple(Lua s, int index, int depth = 0) {
        object val = s.Type(index) switch {
            LuaType.Nil => null!,
            LuaType.Boolean => s.ToBoolean(index),
            LuaType.Number => (float)s.ToNumber(index),
            LuaType.String => s.FastToString(index, false),
            LuaType.Function => s.FastToString(index, false),
            LuaType.Table => depth > 5 ? "table" : s.TableToDictionary(index, depth: depth + 1),//"table",
            _ => throw new LuaException(s, new NotImplementedException($"Can't convert {s.Type(index)} to C# type")),
        };
        return val;
    }

    public static Rectangle ToRectangle(this Lua lua, int index) {
        var x = lua.PeekTableIntValue(index, "x") ?? 0;
        var y = lua.PeekTableIntValue(index, "y") ?? 0;
        var w = lua.PeekTableIntValue(index, "width") ?? 8;
        var h = lua.PeekTableIntValue(index, "height") ?? 8;

        return new Rectangle(x, y, w, h);
    }


    private static byte[] WrapperThisASCII = Encoding.ASCII.GetBytes("__this");
    /*

    /// <summary>
    /// Pushes a Wrapper object, which implements various metamethods on the C# side to communicate between Lua<->C# easily.
    /// Make sure to run <see cref="ClearWrappers"/>, otherwise you'll have a memory leak :(
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="state"></param>
    /// <param name="wrapper"></param>
    public static void PushWrapper(this Lua state, ILuaWrapper wrapper) {
        state.CreateTable(0, 0);
        var tablePos = state.GetTop();

        state.PushASCIIString(WrapperThisASCII);
        state.PushObjectStoreHandle(wrapper);
        state.SetTable(tablePos);

        if (state.NewMetaTable("RYSY_INTERNAL_WRAPPER"))
        {
            // Create a metatable that will call the C# methods:
            int metatableIndex = state.GetTop();

            state.PushString("__index");
            state.PushCFunction(static (nint ptr) => {
                var lua = Lua.FromIntPtr(ptr);

                lua.GetTable(1, WrapperThisASCII);
                //lua.RawGetInteger(1, 1);

                var wrapper = lua.ToObject<ILuaWrapper>(lua.GetTop(), false);
                lua.Pop(1);

                var obj = wrapper ?? throw new LuaException(lua, $"Tried to index null wrapper");
                var top = lua.GetTop();
                var t = lua.Type(top);

                var ret = obj.Lua__index(lua, t switch {
                    LuaType.Number => lua.ToInteger(top),
                    LuaType.String => lua.FastToString(top)
                });

                return ret;
            });

            state.SetTable(metatableIndex);

        }

        state.SetMetaTable(tablePos);
    }

    public static T UnboxWrapper<T>(this Lua lua, int loc) where T : ILuaWrapper {
        //lua.RawGetInteger(loc, 1);
        lua.GetTable(loc, WrapperThisASCII);
        var wrapper = lua.ToObject<T>(lua.GetTop(), false);
        lua.Pop(1);

        return wrapper;
    }
        
    #region GCHandleStore
    public static void PushObjectStoreHandle<T>(this Lua lua, T obj) {
        if (obj == null) {
            lua.PushNil();
            return;
        }

        var handle = GCHandle.Alloc(obj);
        WrapperGCHandles.Add(handle);
        lua.PushLightUserData(GCHandle.ToIntPtr(handle));
    }

    private static List<GCHandle> WrapperGCHandles = new();

    /// <summary>
    /// Forcibly clears all GCHandles used for wrappers. Used to avoid memory leaks.
    /// </summary>
    internal static void ClearWrappers() {
        foreach (var item in WrapperGCHandles) {
            item.Free();
        }
        WrapperGCHandles.Clear();
    }
    #endregion
     
     */

    private static int WrapperIDLoc = -1;
    private static List<LuaFunction> WrapperFuncs = new();
    private static List<byte[]> WrapperMetatableNames = new();

    private static byte[] WrapperMarkerNameASCII = Encoding.ASCII.GetBytes("RYSY_wrapper\0");

    public static void PushWrapper(this Lua state, ILuaWrapper wrapper) {
        var newIndex = LuaWrapperList.Count;
        LuaWrapperList.Add(wrapper);

        if (newIndex == WrapperMetatableNames.Count) {
            WrapperMetatableNames.Add(Encoding.ASCII.GetBytes($"_RIW{newIndex}\0"));
        }

        if (state.NewMetatableASCII(WrapperMetatableNames[newIndex])) {
            // Create a metatable that will call the C# methods:
            int metatableIndex = state.GetTop();

            state.PushNumber(newIndex);
            state.RawSetInteger(metatableIndex, WrapperIDLoc);

            state.PushASCIIString(WrapperMarkerNameASCII);
            state.PushBoolean(true);
            state.RawSet(metatableIndex);

            state.PushString("__index");
            if (newIndex == WrapperFuncs.Count) {
                var f = CreateLuaWrapperForIdx(newIndex);
                GCHandle.Alloc(f);
                WrapperFuncs.Add(f);
            }
            state.PushCFunction(WrapperFuncs[newIndex]);
            state.SetTable(metatableIndex);

            // equality operator
            state.PushString("__eq");
            state.PushCFunction(static (nint s) => {
                var lua = Lua.FromIntPtr(s);

                if (lua.IsWrapper(2)) {
                    /*
                    // get the wrapper indexes of both the wrappers
                    // doesn't work because of the lack of wrapper deduplication, would be a bit faster though...
                    lua.RawGetInteger(1, WrapperIDLoc);
                    var aIdx = lua.ToInteger(lua.GetTop());
                    lua.RawGetInteger(2, WrapperIDLoc);
                    var bIdx = lua.ToInteger(lua.GetTop());
                    lua.Pop(2);
                    lua.PushBoolean(aIdx == bIdx);*/
                    var a = lua.UnboxWrapper<ILuaWrapper>(1);
                    var b = lua.UnboxWrapper<ILuaWrapper>(2);
                    lua.PushBoolean(ReferenceEquals(a, b));
                } else {
                    lua.PushBoolean(false);
                }

                return 1;
            });
            state.SetTable(metatableIndex);


            // set the table to be a metatable of itself
            // this way, pushing a wrapper is very cheap, as it doesn't create any tables
            state.PushCopy(metatableIndex);
            state.SetMetaTable(metatableIndex);
        }
    }

    private static LuaFunction CreateLuaWrapperForIdx(int wrapperIndex) {
        return (nint ptr) => {
            var lua = Lua.FromIntPtr(ptr);

            var wrapper = LuaWrapperList[wrapperIndex];//UnboxWrapper<ILuaWrapper>(lua, 1);

            var obj = wrapper ?? throw new LuaException(lua, $"Tried to index null wrapper");
            var top = lua.GetTop();
            var t = lua.Type(top);

            int ret;
            switch (t) {
                case LuaType.Number:
                    ret = obj.Lua__index(lua, lua.ToInteger(top));
                    break;
                case LuaType.String:
                    //ret = obj.Lua__index(lua, lua.FastToString(top));

                    Span<char> buffer = SharedToStringBuffer.AsSpan();
                    var str = lua.ToStringInto(top, buffer, callMetamethod: false);
                    ret = obj.Lua__index(lua, str);
                    break;
                default:
                    throw new NotImplementedException($"Can't index LuaWrapper with {lua.FastToString(top)} [type: {t}].");
            }

            return ret;
        };
    }

    public static bool IsWrapper(this Lua lua, int loc) {
        lua.PushASCIIString(WrapperMarkerNameASCII);
        var t = lua.RawGet(loc);
        lua.Pop(1);

        return t != LuaType.Nil;
    }

    public static T UnboxWrapper<T>(this Lua lua, int loc) where T : ILuaWrapper {
        lua.RawGetInteger(loc, WrapperIDLoc);
        var wrapper = LuaWrapperList[(int) lua.ToInteger(-1)];
        lua.Pop(1);

        return (T)wrapper;
    }

    private static List<ILuaWrapper> LuaWrapperList = new();
    internal static void ClearWrappers() {
        LuaWrapperList.Clear();
    }

    public static unsafe LuaType GetGlobalASCII(this Lua lua, byte[] asciiName) {
        fixed (byte* ptr = &asciiName[0]) {
            return (LuaType)lua_getglobal(lua.Handle, ptr);
        }
    }

    public static unsafe bool NewMetatableASCII(this Lua lua, byte[] asciiName) {
        fixed (byte* ptr = &asciiName[0]) {
            return luaL_newmetatable(lua.Handle, ptr) != 0;
        }
    }

    public static long ToIntegerSafe(Lua lua, int index) {
        return lua.ToIntegerX(index) ?? throw new LuaException(lua, new InvalidCastException($"Can't convert lua {lua.Type(index)} [{lua.ToString(index)}] to c# integer"));
    }

    [LibraryImport("lua54")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    //[DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
    internal static partial IntPtr luaL_tolstring(IntPtr luaState, int index, out UIntPtr len);

    [LibraryImport("lua54")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    //[DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
    internal static partial IntPtr lua_tolstring(IntPtr luaState, int index, out UIntPtr strLen);

    //[LibraryImport("lua54", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(System.Runtime.InteropServices.Marshalling.AnsiStringMarshaller))]
    //[UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    //internal static partial int lua_getglobal(IntPtr luaState, string name);

    [LibraryImport("lua54")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    internal static unsafe partial int lua_getglobal(nint luaState, byte* name);

    [LibraryImport("lua54")]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvCdecl) })]
    internal static unsafe partial int luaL_newmetatable(nint luaState, byte* name);
}