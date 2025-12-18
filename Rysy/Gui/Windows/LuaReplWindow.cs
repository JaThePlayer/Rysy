using Hexa.NET.ImGui;
using KeraLua;
using Rysy.LuaSupport;
using System.Text;

namespace Rysy.Gui.Windows;

public class LuaReplWindow() : Window("rysy.luaRepl".Translate(), new GuiSize(120, 20).Calculate()) {
    private string _code = "";
    private string _results = "";

    private Lua? _lua;
    
    public override bool HasBottomBar => true;

    protected override void Render() {
        
        ImGui.Columns(2);
        
        ImGui.InputTextMultiline("##code", ref _code, 
            uint.Max(8192, (uint)_code.Length + 10), ImGui.GetContentRegionAvail(), ImGuiInputTextFlags.None);
        ImGui.NextColumn();
        ImGuiManager.ReadOnlyInputTextMultiline("##results", _results, ImGui.GetContentRegionAvail());
        
        ImGui.Columns();
        
        base.Render();
    }

    public override void RenderBottomBar() {
        if (ImGuiManager.TranslatedButton("rysy.luaRepl.run")) {
            Run();
        }
        
        base.RenderBottomBar();
    }

    private void Run() {
        if (_lua is null) {
            var mainLua = EntityRegistry.LuaCtx.Lua;

            _lua = LuaNET.LuaJIT.Lua.lua_newthread(mainLua);
        }

        var lua = _lua.Value;
        const int results = 1;

        try {
            lua.PCallStringThrowIfError(_code, results: results);
        } catch (Exception ex) {
            _results = ex.ToString();
            return;
        }
        

        StringBuilder res = new();
        for (int i = 1; i <= results; i++) {
            res.AppendLine(lua.Type(i) switch {
                LuaType.None => "<no result>",
                LuaType.Nil => "nil",
                LuaType.Boolean => lua.ToBoolean(i).ToStringInvariant(),
                LuaType.LightUserData => "<lightuserdata>",
                LuaType.Number => lua.ToNumber(i).ToStringInvariant(),
                LuaType.String => lua.ToString(i),
                LuaType.Table => lua.Serialize(i),
                LuaType.Function => lua.ToString(i),
                LuaType.UserData => lua.ToString(i),
                LuaType.Thread => lua.ToString(i),
                _ => "???"
            });
        }

        _results = res.ToString();
        
        lua.Pop(results);
    }
}