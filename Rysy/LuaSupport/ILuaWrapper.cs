using KeraLua;

namespace Rysy.LuaSupport;

/// <summary>
/// Implements methods that allow lua code to interact with a C# object as if it was a lua object with a different structure.
/// </summary>
public interface ILuaWrapper {
    /// <summary>
    /// Implements the __index metamethod. Returns the amount of values pushed to the stack.
    /// </summary>
    public int Lua__index(Lua lua, long key);

    public int Lua__index(Lua lua, ReadOnlySpan<char> key);
    //public int Lua__index(Lua lua, object key);
}
