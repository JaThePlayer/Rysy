using KeraLua;
using Rysy.Gui;

namespace Rysy.LuaSupport;

public sealed class LuaLangString : ILuaWrapper {
    public string LangKey { get; set; } = "";

    public LuaLangString() {
        
    }

    public LuaLangString(string str) {
        LangKey = str;
    }
    
    public int LuaIndex(Lua lua, long key) {
        return 0;
    }

    public int LuaIndex(Lua lua, ReadOnlySpan<char> key) {
        switch (key) {
            case "_type":
                lua.PushString("__RYSY_lang"u8);
                return 1;
        }
        
        return 0;
    }

    public override string ToString() {
        return LangKey.Translate();
    }
}

public sealed class LuaDelayedString(string current) : ILuaWrapper {
    public string Value { get; set; } = current;

    public int LuaIndex(Lua lua, long key) {
        return 0;
    }

    public int LuaIndex(Lua lua, ReadOnlySpan<char> key) {
        switch (key) {
            case "_type":
                lua.PushString("__RYSY_lang"u8);
                return 1;
        }
        
        return 0;
    }

    public override string ToString() {
        return Value;
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