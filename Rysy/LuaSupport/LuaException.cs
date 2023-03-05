using KeraLua;

namespace Rysy.LuaSupport;

/// <summary>
/// Wraps a lua error into an Exception, with nice error messages.
/// </summary>
public class LuaException : Exception {
    string error;

    public LuaException(Lua state) {
        //error = state.FastToString(state.GetTop());
        state.Traceback(state, state.FastToString(state.GetTop()), 0);
        error = state.FastToString(state.GetTop());
        state.Pop(1);
    }

    public LuaException(Lua state, Exception inner) : base(null, inner) {
        state.Traceback(state, inner.Message, 0);
        error = state.FastToString(state.GetTop());
        state.Pop(1);
    }

    public LuaException(Lua state, string innerMessage) : base(null) {
        state.Traceback(state, innerMessage, 0);
        error = state.FastToString(state.GetTop());
        state.Pop(1);
    }

    public override string Message => error;
}
