using KeraLua;
using Rysy.Gui;

namespace Rysy.LuaSupport;

public sealed class LuaLangString : ILuaWrapper {
    public string LangKey { get; set; } = "";
    
    public int LuaIndex(Lua lua, long key) {
        return 0;
    }

    public int LuaIndex(Lua lua, ReadOnlySpan<char> key) {
        return 0;
    }

    public override string ToString() {
        return LangKey.Translate();
    }
}

public sealed class LuaTooltip : ILuaWrapper, ITooltip {
    public ITooltip? Tooltip { get; set; }
    
    public int LuaIndex(Lua lua, long key) {
        return 0;
    }

    public int LuaIndex(Lua lua, ReadOnlySpan<char> key) {
        return 0;
    }

    public void RenderImGui() {
        Tooltip?.RenderImGui();
    }

    public bool IsEmpty => Tooltip?.IsEmpty ?? true;
    
    public string? GetRawText() {
        return Tooltip?.GetRawText();
    }

    public override string ToString() {
        return GetRawText() ?? "";
    }
}