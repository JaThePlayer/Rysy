using KeraLua;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.Mods;

namespace Rysy.LuaSupport;

public class LuaCtx {
    public Lua Lua { get; private set; } = new();

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
        RYSY = {} -- Set up a global RYSY variable, so that plugins know they're running in Rysy if needed.
        _RYSY_entities = {}

        _MAP_VIEWER = {
            name = "rysy",
            version = "0.0.0" -- todo: provide this automatically
        }

        _RYSY_unimplemented = function()
            local info = debug.getinfo(2)
            local caller = info.name
            local src = info.short_src

            local traceback = debug.traceback(string.format("The method '%s->%s' is not implemented in Rysy", src, caller), 3)
            --print(traceback)

            error(traceback)
        end

        math.atan2 = math.atan
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

        // _RYSY_DRAWABLE_getTextureSize(texturePath) -> number, number, number, number, number, number
        // gets the clip rectangle and draw offset for a texture, potentially causing preloading.
        lua.Register("_RYSY_DRAWABLE_getTextureSize", (nint s) => {
            var lua = Lua.FromIntPtr(s);

            var path = lua.FastToString(-1);

            var texture = GFX.Atlas[path];
            var clipRect = texture.ClipRect;

            lua.PushNumber(clipRect.X); 
            lua.PushNumber(clipRect.Y);
            lua.PushNumber(clipRect.Width);
            lua.PushNumber(clipRect.Height);
            lua.PushNumber(texture.DrawOffset.X);
            lua.PushNumber(texture.DrawOffset.Y);

            // x,y,w,h,offX,offY
            return 6;
        });

        // _RYSY_DRAWABLE_getRectangle(drawableSprite) -> number, number, number, number
        // gets the render rectangle for a given sprite
        lua.Register("_RYSY_DRAWABLE_getRectangle", (nint s) => {
            var lua = Lua.FromIntPtr(s);
            var top = lua.GetTop();

            var sprite = LonnDrawables.LuaToSprite(lua, top, Vector2.Zero);
            var rect = sprite.GetRenderRect() ?? new Rectangle(0,0,0,0);

            lua.PushNumber(rect.X);
            lua.PushNumber(rect.Y);
            lua.PushNumber(rect.Width);
            lua.PushNumber(rect.Height);

            // x,y,w,h
            return 4;
        });

        // _RYSY_INTERNAL_getWaterfallHeight(room, x, y) -> number
        // calculates the target height of a waterfall, written in c# for performance.
        lua.Register("_RYSY_INTERNAL_getWaterfallHeight", (nint s) => {
            var lua = Lua.FromIntPtr(s);

            var room = lua.UnboxRoomWrapper(1);
            var x = lua.ToNumber(2);
            var y = lua.ToNumber(3);


            lua.PushNumber(Entities.Waterfall.GetHeight(room, new((float) x, (float) y)));
            return 1;
        });

        lua.Register("_RYSY_INTERNAL_requireFromPlugin", (nint s) => {
            var lua = Lua.FromIntPtr(s);

            var lib = lua.FastToString(1, callMetamethod: false);
            var modName = lua.FastToString(2, callMetamethod: false);
            lua.Pop(2);

            //Console.WriteLine($"requireFromPlugin {lib}, {modName}");

            if (ModRegistry.GetModByName(modName) is not { } mod) {
                lua.Error($"Mod {modName} not loaded!");
                return 1;
            }

            var path = $"Loenn/{lib.Replace('.', '/')}.lua";

            if (mod.Filesystem.TryReadAllText(path) is not { } libString) {

                //lua.Error($"library {path} [{modName}] not found!");
                lua.PushNil();
                return 1;
            }

            //lua.PCallStringThrowIfError(libString, lib, results: 1);
            lua.PushString(libString);
            return 1;
        });

        // _RYSY_MODS_find(string modname) -> ModWrapper - finds a mod by everest yaml name
        lua.Register("_RYSY_MODS_find", (nint s) => {
            var lua = Lua.FromIntPtr(s);

            var modName = lua.FastToString(1, callMetamethod: false);

            var mod = ModRegistry.GetModByName(modName);

            if (mod is null) {
                lua.PushNil();
                return 1;
            }

            lua.PushWrapper(mod);
            return 1;
        });

        // _RYSY_DRAWABLE_exists(string texturepath) -> bool - checks if a texture exists
        lua.Register("_RYSY_DRAWABLE_exists", static (nint s) => {
            var lua = Lua.FromIntPtr(s);

            var texture = lua.FastToString(1, callMetamethod: false);

            lua.PushBoolean(GFX.Atlas.Exists(texture));

            return 1;
        });

        // _RYSY_log(status, message) -> nothing - implements logging.log
        lua.Register("_RYSY_log", static (nint s) => {
            var lua = Lua.FromIntPtr(s);
            var status = lua.FastToString(1);
            var message = lua.FastToString(2);

            var logLevel = status switch {
                "DEBUG" => LogLevel.Debug,
                "INFO" => LogLevel.Info,
                "WARNING" => LogLevel.Warning,
                "ERROR" => LogLevel.Error,
                _ => LogLevel.Debug,
            };

            lua.GetInfo("Sn", 2);
            var debugInfoLoc = lua.GetTop();

            var lineNumber = lua.PeekTableIntValue(debugInfoLoc, "linedefined") ?? -1;
            var source = lua.PeekTableStringValue(debugInfoLoc, "short_src") ?? "";
            var funcName = lua.PeekTableStringValue(debugInfoLoc, "name") ?? "";

            lua.Pop(1);

            Logger.Write("[Lua]", logLevel, message, callerMethod: funcName, callerFile: source, lineNumber: lineNumber);

            return 0;
        });

        lua.Register("_RYSY_fake_tiles_get", static (nint s) => {
            var lua = Lua.FromIntPtr(s);
            var layer = lua.FastToString(1);

            if (EditorState.Map is not { } map) {
                lua.CreateTable(0, 0);
                return 1;
            }

            /*  return {
		            dirt = "c",
		            snow = "3",
	            } */
            var autotiler = layer == "tilesFg" ? map.FGAutotiler : map.BGAutotiler;

            var tiles = autotiler.Tilesets.Select(t => (t.Key, autotiler.GetTilesetDisplayName(t.Key)));

            lua.CreateTable(0, autotiler.Tilesets.Count);
            var tablePos = lua.GetTop();
            foreach (var item in tiles) {
                lua.PushString(item.Key.ToString());
                lua.SetField(tablePos, item.Item2);
            }

            return 1;
        });

        RegisterAPIFuncs(lua);

        var orig = lua.AtPanic(AtLuaPanic);

        return luaCtx;
    }

    private static void RegisterAPIFuncs(Lua lua) {
        lua.GetGlobal("RYSY");
        var rysyTableLoc = lua.GetTop();

        // (room, string) -> table
        RegisterApi("entitiesWithSID", static (nint s) => {
            var lua = Lua.FromIntPtr(s);

            var room = lua.UnboxRoomWrapper(1);
            var sid = lua.FastToString(2);

            var filtered = room.Entities[sid];

            lua.PushWrapper(new WrapperListWrapper<Entity>(filtered));

            return 1;
        });

        // (room, string, entity, int) -> table
        RegisterApi("entitiesWithSIDWithinRange", static (nint s) => {
            var lua = Lua.FromIntPtr(s);

            var room = lua.UnboxRoomWrapper(1);
            var sid = lua.FastToString(2);
            var entity = lua.UnboxWrapper<Entity>(3);

            var pos = entity.Pos;
            var maxDistanceSquared = MathF.Pow((float)lua.ToNumber(4), 2);

            var enumerator = room.Entities[sid].GetEnumerator();
            long i = 0;

            lua.PushAndPinFunction((nint s) => {
                start:
                if (enumerator.MoveNext()) {
                    i++;
                    var e = enumerator.Current;
                    if (Vector2.DistanceSquared(pos, e.Pos) >= maxDistanceSquared)
                        goto start;

                    lua.PushInteger(i);
                    lua.PushWrapper(e);
                } else {
                    lua.PushNil();
                    lua.PushNil();
                }


                return 2;
            });

            return 1;
        });

        lua.Pop(1);

        void RegisterApi(string name, LuaFunction cb) {
            lua.PushCFunction(cb);
            lua.SetField(rysyTableLoc, name);
        }
    }

    private static int AtLuaPanic(nint s) {
        var lua = Lua.FromIntPtr(s);
        throw new LuaException(lua);
    }
}
