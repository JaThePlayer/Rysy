using KeraLua;
using Rysy.Graphics;

namespace Rysy.LuaSupport;

public class LuaCtx {
    public Lua Lua { get; set; } = new();

    private static readonly string[] RequireSearchPaths = new string[] {
        "?.lua",
        "Assets/lonnShims/?.lua"
    };

    public static LuaCtx CreateNew() {
        LuaCtx luaCtx = new();
        var lua = luaCtx.Lua;

        // loadstring, but calling selene
        lua.Register("loadstring", (nint s) => {
            var lua = Lua.FromIntPtr(s);

            lua.GetGlobal("selene");
            var seleneLoc = lua.GetTop();
            lua.PushString("parse");
            lua.GetTable(seleneLoc);

            lua.Rotate(1, 2); // put the string from arg1 to the top of the stack
            lua.Call(1, 1); // call selene.parse(arg)

            var st = lua.LoadString(lua.FastToString(-1));
            if (st != LuaStatus.OK) {
                throw new LuaException(lua);
            }

            return 1;
        });

        lua.Register("_RYSY_INTERNAL_findRequirePath", (nint s) => {
            var lua = Lua.FromIntPtr(s);

            var modName = lua.FastToString(-1).Replace('.', '/');

            foreach (var searchPath in RequireSearchPaths) {
                var path = CalcPath(searchPath);

                if (File.Exists(path)) {
                    lua.PushString(path);
                    return 1;
                }

            }

            lua.PushNil();
            lua.PushString(string.Join('\n', RequireSearchPaths.Select(s => $"no file: {CalcPath(s)}")));

            return 2;

            string CalcPath(string searchPath) => searchPath.Replace("?", modName);
        });

        // loads lua from a direct filepath
        lua.Register("_RYSY_INTERNAL_require_file", (nint s) => {
            var lua = Lua.FromIntPtr(s);

            var moduleName = lua.FastToString(-1);
            var path = lua.FastToString(-2);
            var txt = File.ReadAllText(path);
            lua.Pop(2);
            lua.PCallStringThrowIfError(txt, path, results: 1);

            return 1;
        });

        // Load selene
        lua.LoadString("""
            local selene = require("Assets.lua.selene")
            selene.load(nil, true)
            _G.selene = selene
        """, "selene_loader");
        lua.PCallThrowIfError(arguments: 0, results: 0, errorFunctionIndex: 0);

        // Rewrite 'require' so that it runs selene
        lua.PCallStringThrowIfError("""
        function require(modname)
            local alreadyLoaded = package.loaded[modname]

            if alreadyLoaded then
                return alreadyLoaded
            end

            local path, attemptedPaths = _RYSY_INTERNAL_findRequirePath(modname)
            if not path then
                error("module '" .. modname .. "' not found:\n" .. attemptedPaths)
            end

            local ret = _RYSY_INTERNAL_require_file(path, modname)
            package.loaded[modname] = ret

            return ret
        end
        """, "new_require");

        lua.PCallStringThrowIfError("""
        RYSY = true -- Set up a global RYSY variable set to true, so that plugins know they're running in Rysy if needed.
        _RYSY_entities = {}

        _RYSY_unimplemented = function()
            local info = debug.getinfo(2)
            local caller = info.name
            local src = info.short_src

            local traceback = debug.traceback(string.format("The method '%s->%s' is not implemented in Rysy", src, caller), 3)
            --print(traceback)

            error(traceback)
        end
        """, "setup_globals");

        lua.PCallStringThrowIfError(File.ReadAllText("Assets/lua/funpack.lua"), "funpack");

        lua.Register("_RYSY_bit_lshift", (nint s) => {
            var lua = Lua.FromIntPtr(s);

            var x = (int) lua.ToNumber(-2);
            var n = (int) lua.ToNumber(-1);
            lua.Pop(2);

            lua.PushNumber(x << n);

            return 1;
        });

        lua.Register("_RYSY_DRAWABLE_getTextureSize", (nint s) => {
            var lua = Lua.FromIntPtr(s);

            var path = lua.FastToString(-1);

            var texture = GFX.Atlas[path];
            var clipRect = texture.ClipRect;

            lua.PushNumber(clipRect.X); 
            lua.PushNumber(clipRect.Y);
            lua.PushNumber(clipRect.Width);
            lua.PushNumber(clipRect.Height);

            // x,y,w,h
            return 4;
        });

        var orig = lua.AtPanic(AtLuaPanic);

        return luaCtx;
    }

    private static int AtLuaPanic(nint s) {
        var lua = Lua.FromIntPtr(s);
        throw new LuaException(lua);
    }
}
