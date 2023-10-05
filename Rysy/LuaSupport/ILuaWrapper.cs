using KeraLua;

namespace Rysy.LuaSupport;

/// <summary>
/// Implements methods that allow lua code to interact with a C# object as if it was a lua object with a different structure.
/// </summary>
public interface ILuaWrapper {
    /// <summary>
    /// Implements the __index metamethod for number-keys. Returns the amount of values pushed to the stack.
    /// </summary>
    public int Lua__index(Lua lua, long key);

    /// <summary>
    /// Implements the __index metamethod for string-keys. Returns the amount of values pushed to the stack.
    /// </summary>
    public int Lua__index(Lua lua, ReadOnlySpan<char> key);

    /// <summary>
    /// Implements the __len metamethod. Returns the amount of values pushed to the stack.
    /// </summary>
    public virtual int Lua__len(Lua lua) {
        lua.PushInteger(0);

        return 1;
    }

    public virtual void Lua__newindex(Lua lua, ReadOnlySpan<char> key, object value) { }
    public virtual void Lua__newindex(Lua lua, long key, object value) { }
}
