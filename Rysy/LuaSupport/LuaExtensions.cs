using KeraLua;
using Rysy.Extensions;
using Rysy.Helpers;
using Rysy.Mods;
using System;
using System.Collections.Generic;
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


    internal static char[] SharedToStringBuffer = new char[4098];

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

        var decoded = state.Encoding.GetChars(new Span<byte>((void*) source, length), buffer);

        return buffer[..decoded];
    }

    public static unsafe Span<byte> ToStringIntoASCII(this Lua state, int index, bool callMetamethod = true) {
        UIntPtr num;
        IntPtr source;
        if (callMetamethod) {
            source = luaL_tolstring(state.Handle, index, out num);
            state.Pop(1);
        } else {
            source = lua_tolstring(state.Handle, index, out num);
        }

        if (source == IntPtr.Zero)
            return Span<byte>.Empty;

        // todo: check if all these casts are needed?
        int length = checked((int) (uint) num);
        if (length == 0) {
            return Span<byte>.Empty;
        }

        // let's hope we can trust the lua gc to not clear up the string,
        // considering strings are always interned, this should be safe?
        return new Span<byte>((void*) source, length);
    }

    /// <summary>
    /// Pushes an utf8 string onto the stack
    /// </summary>
    public static void PushUtf8String(this Lua lua, byte[] value) {
        lua.PushBuffer(value);
    }

    /// <summary>
    /// Pushes an utf8 string stored in RVA onto the stack
    /// </summary>
    /// <param name="lua"></param>
    /// <param name="value"></param>
    public static unsafe void PushString(this Lua lua, ReadOnlySpan<byte> value) {
        fixed (byte* ptr = &value[0])
            lua_pushlstring(lua.Handle, ptr, (nuint)value.Length);
    }
    
    
    [DllImport("lua54", CallingConvention = CallingConvention.Cdecl)]
    internal static extern unsafe nint lua_pushlstring(nint luaState, byte* s, UIntPtr len);

    public static void LoadStringWithSelene(this Lua lua, string str, string? chunkName = null) {
        string code;
        if (LuaCtx.SeleneLoaded) {
            lua.GetGlobal("selene");
            var seleneLoc = lua.GetTop();
            lua.PushString("parse");
            lua.GetTable(seleneLoc);

            lua.PushString(str);
            lua.Call(1, 1); // call selene.parse(arg)

            code = lua.FastToString(-1);
            lua.Pop(2);
        } else {
            code = str;
        }

        var st = lua.LoadString(code, chunkName ?? str);
        if (st != LuaStatus.OK) {
            throw new LuaException(lua);
        }
    }

    /// <summary>
    /// Calls <see cref="LoadStringWithSelene(Lua, string, string?)"/> with <paramref name="code"/> and <paramref name="chunkName"/>, then calls <see cref="PCallThrowIfError(Lua, int, int, int)"/>
    /// </summary>
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

    public static void PrintStack(this Lua state, int startI = 1,
        [CallerMemberName] string callerMethod = "",
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int lineNumber = 0
    ) {
        Logger.Write("Lua", LogLevel.Info, "Stack:", callerMethod, callerFile, lineNumber);
        for (int i = startI; i <= state.GetTop(); i++) {
            Console.WriteLine($"[{i}]: {state.FastToString(i)}");
        }
    }

    /// <summary>
    /// Peeks the type of the value at t[key], where t is the table at <paramref name="tableStackIndex"/>
    /// </summary>
    public static LuaType PeekTableType(this Lua lua, int tableStackIndex, ReadOnlySpan<byte> key) {
        var type = lua.GetFieldRva(tableStackIndex, key);
        lua.Pop(1);

        return type;
    }
    
    /// <summary>
    /// Peeks whether there's a value at t[key], where t is the table at <paramref name="tableStackIndex"/>
    /// </summary>
    public static bool PeekTableHasKey(this Lua lua, int tableStackIndex, ReadOnlySpan<byte> key) {
        var type = lua.GetFieldRva(tableStackIndex, key);
        lua.Pop(1);

        return type is not LuaType.Nil and not LuaType.None;
    }

    /// <summary>
    /// Peeks the string value at t[key], where t is the table at <paramref name="tableStackIndex"/>
    /// </summary>
    public static string? PeekTableStringValue(this Lua lua, int tableStackIndex, ReadOnlySpan<byte> key) {
        var type = lua.GetFieldRva(tableStackIndex, key);
        string? ret = null;
        if (type == LuaType.String) {
            ret = lua.FastToString(lua.GetTop());
        }
        lua.Pop(1);

        return ret;
    }
    
    /// <summary>
    /// Peeks the enum value at t[key], where t is the table at <paramref name="tableStackIndex"/>
    /// </summary>
    public static TEnum PeekTableEnumValue<TEnum>(this Lua lua, int tableStackIndex, ReadOnlySpan<byte> key, TEnum def)
    where TEnum : struct, Enum {
        var str = lua.PeekTableStringValue(tableStackIndex, key);
        if (str is null)
            return def;

        return Enum.TryParse<TEnum>(str, ignoreCase: true, out var result) ? result : def;
    }
    
    /// <summary>
    /// Peeks the function value at t[key], where t is the table at <paramref name="tableStackIndex"/>
    /// </summary>
    public static LuaFunctionRef? PeekTableFunctionValue(this Lua lua, int tableStackIndex, ReadOnlySpan<byte> key) {
        var type = lua.GetFieldRva(tableStackIndex, key);
        LuaFunctionRef? ret = null;
        if (type == LuaType.Function) {
            ret = LuaFunctionRef.MakeFrom(lua, lua.GetTop());
        }
        lua.Pop(1);

        return ret;
    }

    public static bool TryPeekTableStringValueToSpanInSharedBuffer(this Lua lua, int tableStackIndex, ReadOnlySpan<byte> keyASCII, out Span<char> chars) {
        var type = lua.GetFieldRva(tableStackIndex, keyASCII);
        if (type == LuaType.String) {
            chars = lua.ToStringInto(lua.GetTop(), SharedToStringBuffer);
            lua.Pop(1);
            return true;
        }
        lua.Pop(1);

        chars = default;
        return false;
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

    /// <summary>
    /// Peeks the number value at t[key], where t is the table at <paramref name="tableStackIndex"/>
    /// </summary>
    public static double? PeekTableNumberValue(this Lua lua, int tableStackIndex, ReadOnlySpan<byte> key) {
        var type = lua.GetFieldRva(tableStackIndex, key);
        double? ret = null;
        if (type == LuaType.Number) {
            ret = lua.ToNumber(lua.GetTop());
        }
        lua.Pop(1);

        return ret;
    }

    public static float? PeekTableFloatValue(this Lua lua, int tableStackIndex, ReadOnlySpan<byte> key) {
        var type = lua.GetFieldRva(tableStackIndex, key);
        float? ret = null;
        if (type == LuaType.Number) {
            ret = (float)lua.ToNumber(lua.GetTop());
        }
        lua.Pop(1);

        return ret;
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
    /// Peeks the value at t[key], where t is the table at <paramref name="tableStackIndex"/>, then converts it into a C# value.
    /// </summary>
    public static object? PeekTableCSharpValue(this Lua lua, int tableStackIndex, string key) {
        lua.PushString(key);
        var type = lua.GetTable(tableStackIndex);
        var ret = lua.ToCSharp(lua.GetTop(), makeLuaFuncRefs: true);
        lua.Pop(1);

        return ret;
    }
    
    /// <summary>
    /// Peeks the int value at t[key], where t is the table at <paramref name="tableStackIndex"/>.
    /// </summary>
    public static int? PeekTableIntValue(this Lua lua, int tableStackIndex, ReadOnlySpan<byte> keyRva) {
        var type = lua.GetFieldRva(tableStackIndex, keyRva);
        long? ret = null;
        if (type is LuaType.Number) {
            ret = (int)lua.ToNumber(lua.GetTop());
        }
        lua.Pop(1);

        return ret is { } r ? (int)r : null;
    }
    
    /// <summary>
    /// Peeks the bool value at t[key], where t is the table at <paramref name="tableStackIndex"/>.
    /// </summary>
    public static bool? PeekTableBoolValue(this Lua lua, int tableStackIndex, ReadOnlySpan<byte> key) {
        var type = lua.GetFieldRva(tableStackIndex, key);
        bool? ret = null;
        if (type is LuaType.Boolean) {
            ret = lua.ToBoolean(lua.GetTop());
        }
        lua.Pop(1);

        return ret;
    }

    public static Vector2 PeekTableVector2Value(this Lua lua, int tableStackIndex, ReadOnlySpan<byte> key) {
        var type = lua.GetFieldRva(tableStackIndex, key);
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
    public static Range PeekTableRangeValue(this Lua lua, int tableStackIndex, ReadOnlySpan<byte> key) {
        var type = lua.GetFieldRva(tableStackIndex, key);
        Range ret = default;
        if (type is LuaType.Table) {
            ret = lua.ToRangeNegativeIsFromEnd(lua.GetTop());
        }
        lua.Pop(1);

        return ret;
    }

    public static Color PeekTableColorValue(this Lua lua, int tableStackIndex, ReadOnlySpan<byte> key, Color def) {
        var type = lua.GetFieldRva(tableStackIndex, key);
        Color ret = def;
        if (type is LuaType.Table or LuaType.String) {
            ret = lua.ToColor(lua.GetTop(), def);
        }
        lua.Pop(1);

        return ret;
    }

    public static Rectangle PeekTableRectangleValue(this Lua lua, int tableStackIndex, ReadOnlySpan<byte> key, Rectangle def) {
        var type = lua.GetFieldRva(tableStackIndex, key);
        var ret = def;
        if (type is LuaType.Table or LuaType.String) {
            ret = lua.ToRectangle(lua.GetTop());
        }
        lua.Pop(1);

        return ret;
    }

    public static List<float>? PeekTableNumberList(this Lua lua, int tableStackIndex, ReadOnlySpan<byte> key) {
        var type = lua.GetFieldRva(tableStackIndex, key);

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
    
    public static List<T>? PeekTableList<T>(this Lua lua, int tableStackIndex, ReadOnlySpan<byte> key, Func<Lua, int, T> valueGetter) {
        var type = lua.GetFieldRva(tableStackIndex, key);

        if (type != LuaType.Table) {
            return null;
        }

        var list = new List<T>();

        lua.IPairs((lua, index, loc) => {
            list.Add(valueGetter(lua, loc));
        });

        lua.Pop(1);

        return list;
    }
    
    public static T? PeekTableWrapper<T>(this Lua lua, int tableStackIndex, ReadOnlySpan<byte> key) where T : class, ILuaWrapper {
        var type = lua.GetFieldRva(tableStackIndex, key);

        if (type != LuaType.UserData) {
            return null;
        }

        var ret = lua.UnboxWrapper<T>(lua.GetTop());

        lua.Pop(1);

        return ret;
    }

    public static float? ToFloatX(this Lua lua, int index) {
        return lua.ToNumberX(index) is { } d ? (float) d : null;
    }

    public static float ToFloat(this Lua lua, int index) {
        return (float) lua.ToNumber(index);
    }

    /// <summary>
    /// Pushes t[<paramref name="key"/>], where t is on the stack at <paramref name="index"/>
    /// </summary>
    public static LuaType GetTable(this Lua lua, int index, string key) {
        return lua.GetField(index, key);
    }

    /// <summary>
    /// Pushes t[<paramref name="key"/>], where t is on the stack at <paramref name="index"/>
    /// </summary>
    public static LuaType GetTable(this Lua lua, int index, ReadOnlySpan<byte> key) {
        return lua.GetFieldRva(index, key);
    }

    public static void Push(this Lua lua, object? obj) {
        switch (obj) {
            case null:
                lua.PushNil();
                break;
            case bool b:
                lua.PushBoolean(b);
                break;
            case int i:
                lua.PushInteger(i);
                break;
            case float f:
                lua.PushNumber(f);
                break;
            case string str:
                lua.PushString(str);
                break;
            case ILuaWrapper wrapper:
                lua.PushWrapper(wrapper);
                break;
            case byte[] asciiStr:
                lua.PushUtf8String(asciiStr);
                break;
            case long l:
                lua.PushInteger(l);
                break;
            case double d:
                lua.PushNumber(d);
                break;
            case LuaRef r:
                r.PushToStack(lua);
                break;
            case LuaFunction del:
                lua.PushCFunction(del);
                break;
            case List<object> objList:
                lua.PushWrapper(new ListWrapper<object>(objList));
                break;
            case Dictionary<string, object> dict:
                lua.PushWrapper(new DictionaryWrapper(dict));
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

        ClearLuaResources();

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
            lua.Pop(1);
            throw ex;
        }

        ret = retGetter(lua, lua.GetTop());
        lua.Pop(results);

        ClearLuaResources();

        return ret;
    }

    public static TOut? PCallFunction<TOut>(this Lua lua, Func<Lua, int, TOut?> retGetter, int results, params object[] args) {
        TOut? ret;

        foreach (var arg in args) {
            lua.Push(arg);
        }

        var result = lua.PCall(args.Length, results, 0);
        if (result != LuaStatus.OK) {
            var ex = new LuaException(lua);
            lua.Pop(1);
            throw ex;
        }

        ret = retGetter(lua, lua.GetTop());
        lua.Pop(results);

        ClearLuaResources();

        return ret;
    }

    public static TOut? PCallFunction<TOut>(this Lua lua, Func<Lua, int, TOut?> retGetter, int results = 1) {
        TOut? ret;

        var result = lua.PCall(0, results, 0);
        if (result != LuaStatus.OK) {
            var ex = new LuaException(lua);
            lua.Pop(1);
            throw ex;
        }

        ret = retGetter(lua, lua.GetTop());
        lua.Pop(results);

        ClearLuaResources();

        return ret;
    }

    public static TOut? PCallFunction<TArg1, TArg2, TArg3, TArg4, TOut>(this Lua lua, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, Func<Lua, int, TOut?> retGetter, int results = 1)
    {
        TOut? ret;

        lua.Push(arg1);
        lua.Push(arg2);
        lua.Push(arg3);
        lua.Push(arg4);

        var result = lua.PCall(4, results, 0);
        if (result != LuaStatus.OK) {
            var ex = new LuaException(lua);
            lua.Pop(1);
            throw ex;
        }

        ret = retGetter(lua, lua.GetTop());
        lua.Pop(results);

        ClearLuaResources();

        return ret;
    }

    public static TOut? PCallFunction<TArg1, TOut>(this Lua lua, TArg1 arg1, Func<Lua, int, TOut?> retGetter, int results = 1)
where TArg1 : class, ILuaWrapper {
        TOut? ret;

        lua.PushWrapper(arg1);
        var result = lua.PCall(1, results, 0);
        if (result != LuaStatus.OK) {
            var ex = new LuaException(lua);
            lua.Pop(1);
            throw ex;
        }

        ret = retGetter(lua, lua.GetTop());
        lua.Pop(results);

        ClearLuaResources();

        return ret;
    }

    /// <summary>
    /// Converts a lua table at index <paramref name="index"/> on the stack into a C# dictionary
    /// </summary>
    public static Dictionary<string, object> TableToDictionary(this Lua lua, int index, HashSet<string>? keyBlacklist = null, 
        int depth = 0, bool makeLuaFuncRefs = false) {
        var dict = new Dictionary<string, object>();
        var dataStart = index;

        if (lua.IsWrapper(dataStart) && lua.UnboxWrapper(dataStart) is ILuaDictionaryWrapper dictWrapper)
            return dictWrapper.Dictionary;

        lua.PushNil();
        while (lua.Next(dataStart)) {
            var key = lua.FastToString(-2);
            if (keyBlacklist?.Contains(key) ?? false) {
                goto next;
            }

            var value = ToCSharp(lua, lua.GetTop(), depth, makeLuaFuncRefs);
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
    /// <param name="tableStackLoc"></param>
    public static void IPairs(this Lua lua, Action<Lua, int, int> onElement, int tableStackLoc = -1) {
        for (int i = 1; ; i++) {
            var t = lua.RawGetInteger(tableStackLoc, i);
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

    public static Color ToColor(this Lua lua, int index, Color def) {
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
                return def;
        };
    }



    internal static object ToListOrDict(Lua lua, int index, int depth = 0, bool makeLuaFuncRefs = false) {
        List<object> list = new();

        lua.IPairs((lua, index, loc) => {
            list.Add(ToCSharp(lua, loc, depth + 1, makeLuaFuncRefs));
        });

        if (list.Count > 0) {
            return list;
        }

        return lua.TableToDictionary(index, depth: depth + 1, makeLuaFuncRefs: makeLuaFuncRefs);
    }

    public static List<object>? ToList(this Lua lua, int index, int depth = 0) {
        List<object> list = new();

        lua.IPairs((lua, index, loc) => {
            list.Add(ToCSharp(lua, loc, depth + 1));
        });

        return list;
    }

    public static List<T>? ToList<T>(this Lua lua, int index, int depth = 0) {
        List<T> list = new();

        lua.IPairs((lua, index, loc) => {
            var obj = ToCSharp(lua, loc, depth + 1);
            if (obj is T t)
                list.Add(t);
        });

        return list;
    }

    public static object ToCSharp(this Lua s, int index, int depth = 0, bool makeLuaFuncRefs = false) {
        object val = s.Type(index) switch {
            LuaType.Nil or LuaType.None => null!,
            LuaType.Boolean => s.ToBoolean(index),
            LuaType.Number => (float)s.ToNumber(index),
            LuaType.String => s.FastToString(index, false),
            LuaType.Function => makeLuaFuncRefs ? LuaFunctionRef.MakeFrom(s, index) : s.FastToString(index, false),
            LuaType.Table => depth > 10 ? "table" : ToListOrDict(s, index, depth: depth + 1, makeLuaFuncRefs),//"table",
            LuaType.UserData when s.IsWrapper(index) => s.UnboxWrapper(index), 
            _ => throw new LuaException(s, new NotImplementedException($"Can't convert {s.Type(index)} to C# type")),
        };
        return val;
    }

    public static Rectangle ToRectangle(this Lua lua, int index) {
        var x = lua.PeekTableIntValue(index, "x"u8) ?? 0;
        var y = lua.PeekTableIntValue(index, "y"u8) ?? 0;
        var w = lua.PeekTableIntValue(index, "width"u8) ?? 8;
        var h = lua.PeekTableIntValue(index, "height"u8) ?? 8;

        return new Rectangle(x, y, w, h);
    }

    private static nint _nextNeoWrapperId = nint.MinValue;
    internal static readonly Dictionary<nint, ILuaWrapper> NeoWrappers = new();

    /// <summary>
    /// Type used as a userdata on the lua side to represent C# wrappers.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct NeoWrapper {
        /// <summary>
        /// Id used to index into NeoWrappers.
        /// </summary>
        public nint Id;
    }

    private static unsafe int GCNeoWrapper(nint s) {
        var lua = Lua.FromIntPtr(s);

        var wrapper = (NeoWrapper*)lua.ToUserData(1);
        NeoWrappers.Remove(wrapper->Id);
        // Console.WriteLine($"[{NeoWrappers.Count}] GC: {lua.UnboxWrapper(1)}");

        return 0;
    }
    
    private static int IndexNeoWrapper(nint s) {
        var lua = Lua.FromIntPtr(s);

        var obj = lua.UnboxWrapper(1);
        
        var top = lua.GetTop();
        var t = lua.Type(top);

        int ret;
        switch (t) {
            case LuaType.Number:
                ret = obj.LuaIndex(lua, lua.ToInteger(top));
                break;
            case LuaType.String:
                //Span<char> buffer = SharedToStringBuffer.AsSpan();
                //var str = lua.ToStringInto(top, buffer, callMetamethod: false);
                //ret = obj.LuaIndex(lua, str);

                ret = obj.LuaIndex(lua, lua.ToStringIntoASCII(top, callMetamethod: false));
                break;
            case LuaType.Nil:
                ret = obj.LuaIndexNull(lua);
                break;
            default:
                throw new NotImplementedException($"Can't index LuaWrapper with {lua.FastToString(top)} [type: {t}].");
        }

        return ret;
    }
    
    /// <summary>
    /// Pushes a Wrapper object, which implements various metamethods on the C# side to communicate between Lua and C# easily.
    /// </summary>
    public static unsafe void PushWrapper(this Lua state, ILuaWrapper wrapper) {
        var userdata = (NeoWrapper*)state.NewUserData(sizeof(NeoWrapper));
        userdata->Id = _nextNeoWrapperId;
        NeoWrappers[_nextNeoWrapperId] = wrapper;
        // Prepare next id
        while (NeoWrappers.ContainsKey(++_nextNeoWrapperId)) {}
        
        var handlePos = state.GetTop();
        
        if (state.NewMetaTable("C#Wrapper")) {
            int metatableStackLoc = state.GetTop();
            
            state.PushString("__index"u8);
            state.PushCFunction(IndexNeoWrapper);
            state.SetTable(metatableStackLoc);
            
            state.PushString("__gc"u8);
            state.PushCFunction(GCNeoWrapper);
            state.SetTable(metatableStackLoc);

            state.PushString("__newindex"u8);
            state.PushCFunction(static (nint s) => {
                var lua = Lua.FromIntPtr(s);

                var wrapper = lua.UnboxWrapper(1) ?? throw new LuaException(lua, $"Tried to index null wrapper");
                var value = lua.ToCSharp(3, makeLuaFuncRefs: true);
                const int keyPos = 2;
                //var top = lua.GetTop();

                switch (lua.Type(keyPos)) {
                    case LuaType.Number:
                        wrapper.LuaNewIndex(lua, lua.ToInteger(keyPos), value);
                        break;
                    case LuaType.String:
                        Span<char> buffer = SharedToStringBuffer.AsSpan();
                        var str = lua.ToStringInto(keyPos, buffer, callMetamethod: false);
                        wrapper.LuaNewIndex(lua, str, value);
                        break;
                    default:
                        throw new NotImplementedException($"Can't newindex LuaWrapper with {lua.FastToString(keyPos)} [type: {lua.Type(keyPos)}].");
                }
                return 0;
            });
            state.SetTable(metatableStackLoc);

            // # operator
            state.PushString("__len"u8);
            state.PushCFunction(static (nint s) => {
                var lua = Lua.FromIntPtr(s);

                var wrapper = lua.UnboxWrapper(1);

                return wrapper.LuaLen(lua);
            });
            state.SetTable(metatableStackLoc);

            // equality operator
            state.PushString("__eq"u8);
            state.PushCFunction(static (nint s) => {
                var lua = Lua.FromIntPtr(s);

                if (lua.IsWrapper(2)) {
                    var a = lua.UnboxWrapper(1);
                    var b = lua.UnboxWrapper(2);
                    lua.PushBoolean(ReferenceEquals(a, b));
                } else {
                    lua.PushBoolean(false);
                }

                return 1;
            });
            state.SetTable(metatableStackLoc);
            
            // tostring
            state.PushString("__tostring"u8);
            state.PushCFunction(static (nint s) => {
                var lua = Lua.FromIntPtr(s);
                var wrapper = lua.UnboxWrapper(1);
                
                lua.PushString(wrapper.ToString());
                return 1;
            });
            state.SetTable(metatableStackLoc);
        }
        
        state.SetMetaTable(handlePos);
    }


    public static unsafe bool IsWrapper(this Lua lua, int loc) {
        var data = (NeoWrapper*)lua.ToUserData(loc);

        return data != null && NeoWrappers.ContainsKey(data->Id);
    }

    public static unsafe ILuaWrapper UnboxWrapper(this Lua lua, int loc){
        var data = (NeoWrapper*)lua.ToUserData(loc);
        if (data is null)
            throw new Exception("Can't unbox wrapper, as it isn't one!");
        return NeoWrappers[data->Id];
    }

    public static T UnboxWrapper<T>(this Lua lua, int loc) where T : ILuaWrapper {
        return (T)UnboxWrapper(lua, loc);
    }

    public static Room UnboxRoomWrapper(this Lua lua, int loc) {
        var roomWrapper = lua.UnboxWrapper(loc);
        var room = roomWrapper switch {
            Room r => r,
            RoomLuaWrapper wr => wr.GetRoom(),
            _ => throw new Exception($"Can't convert {roomWrapper} to a Room!")
        };

        return room;
    }

    private static readonly List<GCHandle> LuaUsedHandles = new();
    private static readonly List<Action> LuaCleanupActions = new();
    private static readonly object LuaResourceLock = new();
    
    public static void ClearLuaResources() {
        lock (LuaResourceLock) {
            foreach (var handler in LuaUsedHandles) {
                handler.Free();
            }

            foreach (var act in LuaCleanupActions) {
                act();
            }
        
            LuaCleanupActions.Clear();
            LuaUsedHandles.Clear();
        }
    }

    public static void RegisterLuaCleanupAction(Action cb) {
        lock (LuaResourceLock)
            LuaCleanupActions.Add(cb);
    }

    public static void PushAndPinFunction(this Lua lua, LuaFunction func) {
        var pin = GCHandle.Alloc(func);
        LuaUsedHandles.Add(pin);

        lua.PushCFunction(func);
    }

    public static void SetCurrentModName(this Lua lua, ModMeta? mod) {
        var modName = mod?.Name ?? string.Empty;
        lua.PushString(modName);
        lua.SetGlobal("_RYSY_CURRENT_MOD");
    }

    public static unsafe LuaType GetGlobal(this Lua lua, ReadOnlySpan<byte> asciiName) {
        fixed (byte* ptr = &asciiName[0]) {
            return (LuaType)lua_getglobal(lua.Handle, ptr);
        }
    }

    public static unsafe bool NewMetatable(this Lua lua, ReadOnlySpan<byte> asciiName) {
        fixed (byte* ptr = &asciiName[0]) {
            return luaL_newmetatable(lua.Handle, ptr) != 0;
        }
    }
    
    public static unsafe void GetMetatable(this Lua lua, ReadOnlySpan<byte> utf8Name) {
        fixed (byte* ptr = &utf8Name[0]) {
            _ = lua_getfield(lua.Handle, (int)LuaRegistry.Index, ptr);
        }
    }

    public static long ToIntegerSafe(Lua lua, int index) {
        return lua.ToIntegerX(index) ?? throw new LuaException(lua, new InvalidCastException($"Can't convert lua {lua.Type(index)} [{lua.ToString(index)}] to c# integer"));
    }

    public static unsafe LuaType GetFieldRva(this Lua lua, int tableStackIdx, ReadOnlySpan<byte> fieldName) {
        fixed (byte* ptr = &fieldName[0])
            return (LuaType)lua_getfield(lua.Handle, tableStackIdx, ptr);
    }


    [LibraryImport("lua54")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial IntPtr luaL_tolstring(IntPtr luaState, int index, out UIntPtr len);

    [LibraryImport("lua54")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial IntPtr lua_tolstring(IntPtr luaState, int index, out UIntPtr strLen);

    [LibraryImport("lua54")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static unsafe partial int lua_getglobal(nint luaState, byte* name);

    [LibraryImport("lua54")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static unsafe partial int luaL_newmetatable(nint luaState, byte* name);
    
    [LibraryImport("lua54")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static unsafe partial int lua_getfield(nint luaState, int index, byte* name);
    
    
}
