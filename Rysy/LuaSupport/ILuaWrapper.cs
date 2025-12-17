using KeraLua;
using System.Text;

namespace Rysy.LuaSupport;

/// <summary>
/// Implements methods that allow lua code to interact with a C# object as if it was a lua object with a different structure.
/// </summary>
public interface ILuaWrapper {
    /// <summary>
    /// Implements the __index metamethod for number-keys. Returns the amount of values pushed to the stack.
    /// </summary>
    public int LuaIndex(Lua lua, long key);

    /// <summary>
    /// Implements the __index metamethod for string-keys. Returns the amount of values pushed to the stack.
    /// </summary>
    public int LuaIndex(Lua lua, ReadOnlySpan<char> key);

    public virtual int LuaIndex(Lua lua, ReadOnlySpan<byte> keyAscii) {
        Span<char> buffer = LuaExt.SharedToStringBuffer.AsSpan();
        var decoded = Encoding.ASCII.GetChars(keyAscii, buffer);
        return LuaIndex(lua, buffer[..decoded]);
    }

    public virtual int LuaIndexNull(Lua lua) {
        lua.PushNil();
        return 1;
    }

    /// <summary>
    /// Implements the __len metamethod. Returns the amount of values pushed to the stack.
    /// </summary>
    public virtual int LuaLen(Lua lua) {
        lua.PushInteger(0);

        return 1;
    }

    public virtual void LuaNewIndex(Lua lua, ReadOnlySpan<char> key, object value) { }
    public virtual void LuaNewIndex(Lua lua, long key, object value) { }

    public int LuaNext(Lua lua, object? key = null) {
        lua.PushNil();
        return 1;
    }
    
    public int LuaNextIPairs(Lua lua, int i) {
        lua.PushNil();
        return 1;
    }
}
