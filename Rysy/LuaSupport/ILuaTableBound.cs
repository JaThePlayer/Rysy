namespace Rysy.LuaSupport;

/// <summary>
/// Specifies that the given lua wrapper is actually bound to a regular lua table, which will be kept up-to-date externally.
/// </summary>
public interface ILuaTableBound {
    /// <summary>
    /// Called when the wrapper is being pushed to lua.
    /// </summary>
    /// <returns>Reference to the lua table that this wrapper should be pushed as.</returns>
    LuaTableRef OnBind(Lua luaState);
}