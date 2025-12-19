using KeraLua;

namespace Rysy.LuaSupport;

/// <summary>
/// Wraps a lua error into an Exception, with nice error messages.
/// </summary>
public class LuaException : Exception {
    private string _error;

    private void SetError(Lua state, string msg) {
        state.Traceback(state, msg, 0);
        _error = state.FastToString(state.GetTop());
        state.Pop(1);
    }
    
    public LuaException(Lua state) {
        SetError(state, state.FastToString(state.GetTop()));
    }

    public LuaException(Lua state, Exception inner) : base(null, inner) {
        SetError(state, inner.Message);
    }

    public LuaException(Lua state, string innerMessage) : base(null) {
        SetError(state, innerMessage);
    }

    public override string Message => _error;
}
