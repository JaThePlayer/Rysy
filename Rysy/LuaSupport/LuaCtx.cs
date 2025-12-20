using Hexa.NET.ImGui;
using KeraLua;
using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Helpers;
using Rysy.Layers;
using Rysy.Mods;
using Rysy.Scenes;
using Rysy.Tools;
using System.Runtime.InteropServices;
using System.Text;

namespace Rysy.LuaSupport;

public class LuaCtx {
    public Lua Lua { get; private set; } = Lua.CreateNew(openLibs: true);
    
    public static bool SeleneLoaded { get; private set; }

    private static readonly string[] RequireSearchPaths = new string[] {
        "?.lua",
        "lonnShims/?.lua"
    };

    public static LuaCtx CreateNew() {
        LuaCtx luaCtx = new();
        var lua = luaCtx.Lua;

        SeleneLoaded = false;

        // loadstring, but calling selene
        lua.Register("loadstring", (nint s) => {
            var lua = Lua.FromIntPtr(s);

            var moduleName = "?";

            if (lua.GetTop() >= 2) {
                moduleName = lua.FastToString(2);
                lua.Pop(1);
            }

            lua.GetGlobal("selene");
            var seleneLoc = lua.GetTop();
            lua.PushString("parse");
            lua.GetTable(seleneLoc);

            lua.PushCopy(1);
            //lua.Rotate(1, 2); // put the string from arg1 to the top of the stack
            var result = lua.PCall(1, 1, 0); // call selene.parse(arg)
            if (result != LuaStatus.OK) {
                var errMsgPos = lua.GetTop();
                
                lua.PushNil();
                lua.PushCopy(errMsgPos);
                lua.PrintStack(lua.GetTop() - 1);
                return 2;
            }

            var st = lua.LoadString(lua.FastToString(-1), moduleName);
            if (st != LuaStatus.OK) {
                var errMsgPos = lua.GetTop();
                
                lua.PushNil();
                lua.PushCopy(errMsgPos);
                return 2;
            }

            return 1;
        });

        lua.Register("_RYSY_INTERNAL_findRequirePath", (nint s) => {
            var lua = Lua.FromIntPtr(s);

            var modName = lua.FastToString(-1).Replace('.', '/');
            var rysyFs = ModRegistry.RysyMod.Filesystem;

            foreach (var searchPath in RequireSearchPaths) {
                var path = CalcPath(searchPath);
                
                if (rysyFs.FileExists(path)) {
                    lua.PushString(path);
                    return 1;
                }
            }

            lua.PushNil();
            lua.PushString(string.Join('\n', RequireSearchPaths.Select(s => $"no file: {CalcPath(s)}")));

            return 2;

            string CalcPath(string searchPath) => searchPath.Replace("?", modName, StringComparison.Ordinal);
        });

        // loads lua from a direct filepath
        lua.Register("_RYSY_INTERNAL_require_file", (nint s) => {
            var lua = Lua.FromIntPtr(s);

            var moduleName = lua.FastToString(-1);
            var path = lua.FastToString(-2);

            lua.Pop(2);
            if (ModRegistry.RysyMod.Filesystem.TryReadAllText(path) is { } txt) {
                lua.PCallStringThrowIfError(txt, path, results: 1);
            } else {
                Logger.Write("Lua", LogLevel.Error, $"Failed to require file from Rysy: {path}");
                //throw new FileNotFoundException(path);
            }

            return 1;
        });
        
        // Rewrite 'require' so that it runs selene and runs through IModFilesystem
        lua.PCallStringThrowIfError("""
        local orig_require = require
        
        local builtins = {
            -- Extensions built into LuaJit
            ["bit"] = true,
            ["ffi"] = true,
            ["string.buffer"] = true,
        }
        
        function require(modname)
            if builtins[modname] then
                return orig_require(modname)
            end

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
        """u8, "new_require");

        lua.PCallStringThrowIfError("""
        local orig_ipairs = ipairs
        local orig_pairs = pairs
        
        function ipairs(t)
            local mt = getmetatable(t)
            if mt then
                if mt.__ipairs then
                    return mt.__ipairs(t)
                end
            end
            return orig_ipairs(t)
        end
        
        function pairs(t)
            local mt = getmetatable(t)
            if mt then
                if mt.__pairs then
                    return mt.__pairs(t)
                end
            end
            return orig_pairs(t)
        end
        """, "fix_luajit_ipairs");

        // Load selene
        lua.PCallStringThrowIfError("""
            local selene = require("lua.selene")
            selene.load(nil, true)
            _G.selene = selene
        """u8, "selene_loader");

        SeleneLoaded = true;

        lua.PCallStringThrowIfError("""
        RYSY = {} -- Set up a global RYSY variable, so that plugins know they're running in Rysy if needed.
        _RYSY_entities = {}
        _RYSY_styles = {}

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
        """u8, "setup_globals");

        lua.PCallStringThrowIfError("""
        -- Required to make LuaRef work, allows holding strong references to lua objects from C#.
        local refs = {}
        
        function __rysy_mkr(f)
            local i = 0
            while refs[i] do
                i = i + 1
            end
            
            refs[i] = f
            _G["__rysy_ref" .. i] = f
            
            return i
        end
        
        function __rysy_gcr(id)
            refs[id] = nil
            _G["__rysy_ref" .. id] = nil
        end
        """u8, "setup_lua_ref_glue");
        
        lua.PCallStringThrowIfError("""
        local orig_ipairs = ipairs
        
        function ipairs(t)
            local mt = getmetatable(t)
            if mt then
                if mt.__ipairs then
                    return mt.__ipairs(t)
                end
            end
            return orig_ipairs(t)
        end
        """, "fix_luajit_ipairs");

        Utf8Lib.Register(lua);

        Utf8Lib.Register(lua);

        lua.Register("_RYSY_DRAWABLE_fixPath", (nint s) => {
            var lua = Lua.FromIntPtr(s);

            lua.PushString(LonnDrawables.SanitizeLonnTexturePath(lua.FastToString(1)));
            
            return 1;
        });

        if (ModRegistry.RysyMod.Filesystem.TryReadAllText("lua/funpack.lua") is {} funpack)
            lua.PCallStringThrowIfError(funpack, "funpack");

        // _RYSY_DRAWABLE_getTextureSize(texturePath, atlaspath) -> number, number, number, number, number, number
        // gets the clip rectangle and draw offset for a texture, potentially causing preloading.
        lua.Register("_RYSY_DRAWABLE_getTextureSize", (nint s) => {
            var lua = Lua.FromIntPtr(s);

            var path = lua.FastToString(1);
            var atlasName = lua.FastToString(2);

            var texture = Gfx.Atlas[path];
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

            var sprite = LonnDrawables.LuaToSprite(lua, top);
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
            var shouldRegisterWatcher = lua.ToBoolean(3);
            lua.Pop(3);

            //Console.WriteLine($"requireFromPlugin {lib}, {modName}");

            if (ModRegistry.GetModByName(modName) is not { } mod) {
                Logger.Write("Lua.mods.requireFromPlugin", LogLevel.Warning, $"Attempted to load library '{lib}' from missing mod {modName}");
                lua.PushNil();
                return 1;
            }

            var path = $"Loenn/{lib.Replace('.', '/')}.lua";

            if (mod.Filesystem.TryReadAllText(path) is not { } libString) {
                Logger.Write("Lua.mods.requireFromPlugin", LogLevel.Warning, $"Library '{lib}' [{modName}] not found!");
                lua.PushNil();
                return 1;
            }
            
            // Notify lua when the library updates, so that it can hot-reload it.
            if (shouldRegisterWatcher) {
                mod.Filesystem.RegisterFilewatch(path, new WatchedAsset {
                    OnChanged = (p) => {
                        lua.SetCurrentModName(mod);
                        lua.GetGlobal("_RYSY_clear_requireFromPlugin_cache");
                        lua.PushString(lib);
                        lua.PushString(modName);
                        lua.Call(2, 0);
                    
                        if (RysyState.Scene is EditorScene editorScene)
                            editorScene.Map?.Rooms.ForEach(r => r.ClearRenderCacheAggressively());
                    }
                });
            }

            lua.PushString(libString);
            return 1;
        });

        lua.Register("_RYSY_INTERNAL_hotReloadPlugin", (nint s) => {
            var lua = Lua.FromIntPtr(s);

            var lib = lua.FastToString(1, callMetamethod: false);
            var modName = lua.FastToString(2, callMetamethod: false);
            var type = lua.FastToString(3, callMetamethod: false);
            lua.Pop(3);

            if (ModRegistry.GetModByName(modName) is not { } mod) {
                lua.Error($"Mod {modName} not loaded!");
                return 0;
            }

            switch (type) {
                case "entity":
                    EntityRegistry.LoadLuaPluginFromModFile(mod, lib, trigger:  false);
                    break;
                case "trigger":
                    EntityRegistry.LoadLuaPluginFromModFile(mod, lib, trigger: true);
                    break;
                case "style":
                    EntityRegistry.LoadLuaEffectPlugin(mod, lib);
                    break;
                case "field":
                    EntityRegistry.LoadLuaFieldTypePlugin(mod, lib);
                    break;
            }
            
            return 0;
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

            lua.PushWrapper(mod.EverestYaml.Find(y => y.Name == modName)!);
            lua.PushWrapper(new ListWrapper<EverestModuleMetadata>(mod.EverestYaml));
            return 2;
        });
        
        lua.Register("_RYSY_MODS_getModNameFromPath", (s) => {
            var lua = Lua.FromIntPtr(s);

            var path = lua.FastToString(1, callMetamethod: false);

            ModMeta? mod = null;

            if (path.StartsWith('$')) {
                var firstSlash = path.IndexOf('/', StringComparison.Ordinal);
                if (firstSlash == -1)
                    firstSlash = path.Length;
                var name = path[1..firstSlash];
                mod = ModRegistry.GetModByName(name);
            }
            else if (path.StartsWith("@ModsCommon@/", StringComparison.Ordinal)) {
                var vPath = path["@ModsCommon@/".Length..];
                mod = ModRegistry.Filesystem.FindFirstModContaining(vPath);
            } else {
                mod = ModRegistry.Filesystem.FindFirstModContaining(path);
            }
            
            if (mod is null) {
                lua.PushNil();
                return 1;
            }

            lua.PushString(mod.Name);
            return 1;
        });
        

        // _RYSY_DRAWABLE_exists(string texturepath, string atlasName) -> bool - checks if a texture exists
        lua.Register("_RYSY_DRAWABLE_exists", static (nint s) => {
            var lua = Lua.FromIntPtr(s);

            var texture = lua.FastToString(1, callMetamethod: false);
            var atlas = lua.FastToString(2, callMetamethod: false);
            
            // TODO: handle different atlases

            lua.PushBoolean(Gfx.Atlas.TryGetWithoutTryingFrames(texture, out _));

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

            /*
            lua.GetInfo("Sn", 2);
            var debugInfoLoc = lua.GetTop();

            var lineNumber = lua.PeekTableIntValue(debugInfoLoc, "linedefined") ?? -1;
            var source = lua.PeekTableStringValue(debugInfoLoc, "short_src") ?? "";
            var funcName = lua.PeekTableStringValue(debugInfoLoc, "name") ?? "";

            lua.Pop(1);

            Logger.Write("Lua", logLevel, message, callerMethod: funcName, callerFile: source, lineNumber: lineNumber);*/

            Logger.Write("Lua", logLevel, message);

            return 0;
        });

        lua.Register("_RYSY_fake_tiles_get", static (nint s) => {
            var lua = Lua.FromIntPtr(s);
            var layer = lua.FastToString(1);
            lua.PushWrapper(new LuaTilesetsDictionaryWrapper(layer == "tilesFg" ? TileLayer.Fg : TileLayer.Bg));

            return 1;
        });
        
        //_RYSY_fakeTilesTileMaterialForLayer(string layer) -> string
        lua.Register("_RYSY_fakeTilesTileMaterialForLayer", static (nint s) => {
            var lua = Lua.FromIntPtr(s);
            var layer = lua.FastToString(1);

            var rysyLayerName = layer switch {
                "tilesFg" => EditorLayers.Fg.Name,
                "tilesBg" => EditorLayers.Bg.Name,
                _ => null,
            };

            if (rysyLayerName is null
                || RysyState.Scene is not EditorScene { ToolHandler: { } toolHandler } 
                || toolHandler.GetTool<TileTool>() is not {} tileTool
                || Persistence.Instance?.Get(tileTool.GetPersistenceMaterialKeyForLayer(rysyLayerName), (object) null!)?.ToString() is not {} mat) {
                lua.PushNil();
                return 1;
            }
            
            lua.PushString(mat);
            return 1;
        });

        lua.Register("_RYSY_INTERNAL_getModSetting", static (nint s) => {
            var lua = Lua.FromIntPtr(s);
            var modName = lua.FastToString(1);
            var settingName = lua.FastToString(2);

            var mod = ModRegistry.GetModByName(modName);

            if (mod is not { Settings: { } settings }) {
                lua.PushNil();
                return 1;
            }

            if (settings.OtherValues.TryGetValue(settingName, out var value)) {
                lua.Push(value);
                return 1;
            }

            var bindings = LonnBindingHelper.GetAllBindings(settings.GetType());
            if (bindings.TryGetValue(settingName, out var prop)) {
                lua.Push(prop.GetValue(settings));
                return 1;
            }

            lua.PushNil();
            return 1;
        });

        lua.Register("_RYSY_INTERNAL_setModSetting", static (nint s) => {
            var lua = Lua.FromIntPtr(s);
            var modName = lua.FastToString(1);
            var settingName = lua.FastToString(2);
            var value = lua.ToCSharp(3);

            var mod = ModRegistry.GetModByName(modName);

            if (mod is not { Settings: { } settings }) {
                return 0;
            }

            var bindings = LonnBindingHelper.GetAllBindings(settings.GetType());
            if (bindings.TryGetValue(settingName, out var prop)) {
                try {
                    prop.SetValue(settings, value);
                    settings.Save();
                    return 0;
                } catch (Exception e) {
                    lua.PushString(e.ToString());
                    return 1;
                }
            }

            // todo: handle tables being passed here - they need to get a metatable (or we handle that in lua...)
            settings.OtherValues[settingName] = value;
            settings.Save();

            return 0;
        });

        //(sid, value) -> void
        lua.Register("_RYSY_INTERNAL_addPlacement", static (nint s) => {
            var lua = Lua.FromIntPtr(s);
            var sid = lua.FastToString(1);
            var placement = new LonnPlacement(lua, 2);
            var trigger = false;

            if (EntityRegistry.GetTypeForSid(sid, RegisteredEntityType.Entity) is { } t) {
                trigger = t.IsSubclassOf(typeof(Trigger));
            }

            if (EntityRegistry.GetInfo(sid, RegisteredEntityType.Entity) is {} info)
                EntityRegistry.RegisterLuaPlacements(info, trigger, [placement]);

            return 0;
        });
        
        //(rectangles, checkX, checkY, checkWidth, checkHeight) -> bool
        lua.Register("_RYSY_CONNECTED_ENTITIES_hasAdjacent", static (nint s) => {
            var lua = Lua.FromIntPtr(s);

            List<Rectangle> rectangles;
            if (lua.GetTable(1, "_rysy_rect"u8) != LuaType.Nil) {
                rectangles = lua.UnboxWrapper<ListWrapper<Rectangle>>(lua.GetTop()).Inner;
                lua.Pop(1);
            } else {
                lua.Pop(1);
                
                rectangles = new List<Rectangle>();
                lua.IPairs((lua, index, loc) => {
                    rectangles.Add(lua.ToRectangle(loc));
                }, tableStackLoc: 1);
                
                lua.PushString("_rysy_rect"u8);
                lua.PushWrapper(new ListWrapper<Rectangle>(rectangles));
                lua.SetTable(1);
            }
            
            var checkX = (int)lua.ToFloat(2);
            var checkY = (int)lua.ToFloat(3);
            var checkWidth = (int)lua.ToFloat(4);
            var checkHeight = (int)lua.ToFloat(5);
            var checkRect = new Rectangle(checkX, checkY, checkWidth, checkHeight);

            foreach (var r in CollectionsMarshal.AsSpan(rectangles)) {
                if (r.Intersects(checkRect)) {
                    lua.PushBoolean(true);
                    return 1;
                }
            }
            
            lua.PushBoolean(false);

            return 1;
        });
        
        lua.Register("_RYSY_INTERNAL_makeWrapperShallowCopy", static (nint s) => {
            var lua = Lua.FromIntPtr(s);
            var wrapper = lua.UnboxWrapper(1);

            lua.PushWrapper(new ShallowCopyWrapper(wrapper));
            return 1;
        });
        
        lua.Register("_RYSY_triggers_getDrawableDisplayTextForSid", static (nint s) => {
            var lua = Lua.FromIntPtr(s);
            var sid = lua.FastToString(1);

            lua.PushString(Trigger.GetDefaultTextForSid(sid));
            return 1;
        });
        
        lua.Register("_RYSY_loaded_state_getSelectedRoom", static (nint s) => {
            var lua = Lua.FromIntPtr(s);

            lua.Push((object?)EditorState.CurrentRoom ?? false);
            return 1;
        });
        
        lua.Register("_RYSY_loaded_state_getMap", static (nint s) => {
            var lua = Lua.FromIntPtr(s);

            lua.Push(EditorState.Map);
            return 1;
        });
        
        lua.Register("_RYSY_loaded_state_getRoomByName", static (nint s) => {
            var lua = Lua.FromIntPtr(s);
            var name = lua.FastToString(1);

            if (EditorState.Map?.TryGetRoomByName(name) is { } room) {
                lua.Push(room);
                lua.PushInteger(EditorState.Map.Rooms.IndexOf(room) + 1);
            } else {
                lua.PushNil();
                lua.PushNil();
            }
            return 2;
        });
        
        // (data, texture)
        lua.Register("_RYSY_DRAWABLE_makeFromEntity", static (nint s) => {
            var lua = Lua.FromIntPtr(s);
            var data = lua.GetTop() - 1;

            if (!lua.IsWrapper(data) || lua.UnboxWrapper(data) is not LonnEntity entity || !entity.CanMakeLonnDrawableTemplate())
                return 0;
            
            lua.CreateTable(0, 10);
            var output = lua.GetTop();
            
            lua.PushString("_type"u8);
            lua.PushString("drawableSprite"u8);
            lua.SetTable(output);
            
            lua.PushString("x"u8);
            lua.PushNumber(entity.X);
            lua.SetTable(output);
            lua.PushString("y"u8);
            lua.PushNumber(entity.Y);
            lua.SetTable(output);
            
            lua.PushString("justificationX"u8);
            lua.PushNumber(0.5);
            lua.SetTable(output);
            lua.PushString("justificationY"u8);
            lua.PushNumber(0.5);
            lua.SetTable(output);
            
            lua.PushString("scaleX"u8);
            lua.PushNumber(1);
            lua.SetTable(output);
            lua.PushString("scaleY"u8);
            lua.PushNumber(1);
            lua.SetTable(output);
            
            lua.PushString("renderOffsetX"u8);
            lua.PushNumber(0);
            lua.SetTable(output);
            lua.PushString("renderOffsetY"u8);
            lua.PushNumber(0);
            lua.SetTable(output);
            
            lua.PushString("rotation"u8);
            lua.PushNumber(0);
            lua.SetTable(output);

            if (entity.EntityData.TryGetValue("depth", out var d) && d is IConvertible) {
                lua.PushString("depth"u8);
                lua.PushNumber(Convert.ToDouble(d, CultureInfo.InvariantCulture));
                lua.SetTable(output);
            }

            return 1;
        });
        
        lua.Register("_RYSY_lang_get", static s => {
            var lua = Lua.FromIntPtr(s);
            var name = lua.FastToString(1);

            lua.PushString(name.Translate());
            return 1;
        });

        lua.Register("_RYSY_ENTITIES_getAllRegisteredNames", static s => {
            var lua = Lua.FromIntPtr(s);
            var entities = EntityRegistry.Registered;

            lua.CreateTable(entities.Count, 0);
            var tablePos = lua.GetTop();
            long i = 0;
            foreach (var (sid, _) in entities) {
                lua.PushInteger(i);
                lua.PushString(sid);
                lua.SetTable(tablePos);

                i++;
            }
            
            return 1;
        });
        
        lua.PCallStringThrowIfError("""
            local orig_table_shallowcopy = table.shallowcopy
            table.shallowcopy = function(tbl, ...)
                local mt = getmetatable(tbl)
                if mt and rawget(mt, "_RWR") then
                    return _RYSY_INTERNAL_makeWrapperShallowCopy(tbl)
                end
                
                return orig_table_shallowcopy(tbl, ...)
            end
            """u8, "fix_shallow_copy");

        RegisterApiFuncs(lua);

        var orig = lua.AtPanic(AtLuaPanic);

        return luaCtx;
    }

    private static void RegisterApiFuncs(Lua lua) {
        lua.GetGlobal("RYSY");
        var rysyTableLoc = lua.GetTop();

        // (room, string) -> table
        RegisterApi("entitiesWithSID", static lua => {

            var room = lua.UnboxRoomWrapper(1);
            var sid = lua.FastToString(2);

            var filtered = room.Entities[sid];

            lua.PushWrapper(new WrapperListWrapper<Entity>(filtered));

            return 1;
        });

        // (room, string, entity, int) -> table
        RegisterApi("entitiesWithSIDWithinRangeUntilThis", static lua => {
            var room = lua.UnboxRoomWrapper(1);
            var sid = lua.FastToString(2);
            var entity = lua.UnboxWrapper<Entity>(3);

            var pos = entity.Pos;
            var maxDistanceSquared = MathF.Pow((float)lua.ToNumber(4), 2);

            var enumerator = room.Entities[sid].GetEnumerator();
            long i = 0;

            lua.PushAndPinFunction(lua => {
                start:
                if (enumerator.MoveNext()) {
                    i++;
                    var e = enumerator.Current;
                    if (e == entity) {
                        return 2;
                    }

                    if (Vector2.DistanceSquared(pos, e.Pos) >= maxDistanceSquared)
                        goto start;

                    lua.PushInteger(i);
                    lua.PushWrapper(e);
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

    private static int AtLuaPanic(Lua s) {
        throw new LuaException(s);
    }
}
