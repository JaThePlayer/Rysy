using KeraLua;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Rysy.LuaSupport;

public class LonnEntity : Entity, ICustomNodeHandler {
    [JsonIgnore]
    public LonnEntityPlugin Plugin;

    [JsonIgnore]
    public override int Depth => Plugin.GetDepth(Room, this);

    public IEnumerable<ISprite> GetNodeSprites() {
        using var stackHolder = Plugin.PushToStack();

        var visibility = Plugin.GetNodeVisibility(this);

        return visibility switch {
            "always" => NodeHelper.GetGuessedNodeSpritesFor(this),
            "selected" => Array.Empty<ISprite>(),
            var other => Array.Empty<ISprite>(),
        };
    }

    public override IEnumerable<ISprite> GetSprites() {
        try {
            using var stackHolder = Plugin.PushToStack();

            var spr = _GetSprites();
            //stackHolder?.Dispose();

            return spr;
        } catch (LuaException ex) {
            Logger.Error(ex, $"Erroring entity definition for {Plugin.Name} at {this.ToJson()}");
            return Array.Empty<ISprite>();
        }
    }

    public override ISelectionCollider GetMainSelection() {
        //selection(room, entity):rectangle, table of rectangles
        if (Plugin.HasSelectionFunction) {
            Rectangle rectangle;
            var lua = Plugin.LuaCtx.Lua;
            using (var stackHolder = Plugin.PushToStack()) {
                var type = lua.GetTable(Plugin.StackLoc, "selection");

                if (type != LuaType.Function) {
                    lua.Pop(1);
                    return base.GetMainSelection();
                }

                rectangle = lua.PCallFunction(Room, this, (lua, top) => {
                    return lua.ToRectangle(top);
                });
            }

            return ISelectionCollider.FromRect(rectangle);
        }

        return base.GetMainSelection();
    }

    public override Range NodeLimits {
        get {
            var limits = Plugin.GetNodeLimits(Room, this);

            return new(limits.X.AtLeast(0), limits.Y == -1 ? Index.End : limits.Y);
        }
    }

    public override Point MinimumSize => Plugin.GetMinimumSize(Room, this);

    #region Sprites
    private List<ISprite> SpritesFromLonn(Lua lua, int top) {
        var list = new List<ISprite>();

        switch (lua.PeekTableType(top, "_type")) {
            case LuaType.String:
                // name is provided, so there's 1 placement
                NextSprite(top, list);
                break;
            default:
                var prevTop = lua.GetTop();
                lua.IPairs((lua, i, loc) => {
                    NextSprite(loc, list);
                });
                break;
        }

        return list;

        void NextSprite(int top, List<ISprite> addTo) {
            var type = lua.PeekTableStringValue(top, "_type");
            switch (type) {
                case "drawableSprite":
                    addTo.Add(LonnDrawables.LuaToSprite(lua, top, Pos));
                    break;
                case "drawableLine":
                    addTo.Add(LonnDrawables.LuaToLine(lua, top));
                    break;
                case "_RYSY_fakeTiles":
                    addTo.AddRange(LonnDrawables.LuaToFakeTiles(lua, top, Room) ?? Array.Empty<ISprite>());
                    break;
                case "drawableRectangle":
                    addTo.Add(LonnDrawables.LuaToRect(lua, top));
                    break;
                case "drawableNinePatch":
                    addTo.Add(LonnDrawables.LuaToNineSlice(lua, top));
                    break;
                default:
                    Logger.Write("LonnEntity", LogLevel.Warning, $"Unknown Lonn sprite type: {type}: {lua.TableToDictionary(top).ToJson()}");
                    break;
            }
        }
    }

    private List<ISprite> _GetSprites() {
        if (Plugin.HasGetSprite) {
            var lua = Plugin.LuaCtx.Lua;

            var type = lua.GetTable(Plugin.StackLoc, "sprite");

            if (type != LuaType.Function) {
                lua.Pop(1);
                return new();
            }

            var sprites = lua.PCallFunction(Room, this, (lua, i) => SpritesFromLonn(lua, i));

            return sprites!;
        }


        if (Plugin.GetTexture is { } getTexture && getTexture(Room, this) is { } texturePath) {
            return new() { ISprite.FromTexture(Pos, texturePath) with {
                Origin = Plugin.GetJustification(Room, this),
                Color = Plugin.GetColor(Room, this),
                Scale = Plugin.GetScale(Room, this),
                Rotation = Plugin.GetRotation(Room, this),
            }};
        } else {
            return new() { ISprite.OutlinedRect(Rectangle, Plugin.GetFillColor(Room, this), Plugin.GetBorderColor(Room, this)) };
        }
    }

    #endregion
}

public sealed class LonnEntityPlugin {
    public LuaCtx LuaCtx { get; private set; }
    public int StackLoc { get; private set; }

    public string Name { get; private set; }

    public Func<Room, Entity, int> GetDepth;

    public Func<Room, Entity, string?>? GetTexture;
    public bool HasGetSprite;

    public Func<Room, Entity, Vector2> GetJustification;
    public Func<Room, Entity, Vector2> GetScale { get; set; }
    public Func<Room, Entity, float> GetRotation;

    public Func<Room, Entity, Color> GetColor;

    public Func<Room, Entity, Color> GetFillColor;
    public Func<Room, Entity, Color> GetBorderColor;

    public Func<Entity, string> GetNodeVisibility;

    public Func<Room, Entity, Point> GetNodeLimits;
    public Func<Room, Entity, Point> GetMinimumSize;
    public Func<Room, Entity, Point> GetMaximumSize;

    public bool HasSelectionFunction;

    public List<LonnPlacement> Placements = new();

    public LuaStackHolder? StackHolder { get; private set; }

    public record struct LuaStackHolder(LonnEntityPlugin Plugin, int Amt) : IDisposable {
        public void Dispose() {
            if (Plugin is null)
                return;

            var lua = Plugin.LuaCtx.Lua;

            if (lua.GetTop() < Amt) {
                Console.WriteLine("SKIPPING");
                return;
            }

            if (Plugin.StackLoc < lua.GetTop()) {
                Logger.Write("LonnEntity", LogLevel.Warning, $"Lua stack grew after using {Plugin.Name}! Previous: {Plugin.StackLoc}, now: {lua.GetTop()}.");
                lua.PrintStack();
                Logger.Write("LonnEntity", LogLevel.Warning, $"Top element on stack: {lua.TableToDictionary(lua.GetTop()).ToJson()}");
            }
            lua.Pop(Amt);

            Plugin.StackHolder = null;
        }
    }

    public LuaStackHolder? PushToStack() {
        var lua = LuaCtx.Lua;
        if (StackHolder is { } holder) {
            //lua.PrintStack();
            //StackLoc.LogAsJson();
            //return null;
        }


        lua.GetGlobal("_RYSY_entities");
        var entitiesTableLoc = lua.GetTop();

        lua.PushString(Name);
        lua.GetTable(entitiesTableLoc);

        StackLoc = lua.GetTop();

        holder = new(this, 2);
        StackHolder = holder;

        return holder;
    }

    public static List<LonnEntityPlugin> FromCtx(LuaCtx ctx) {
        var lua = ctx.Lua;
        var top = lua.GetTop();

        var plugins = new List<LonnEntityPlugin>();

        var entityName = lua.PeekTableStringValue(top, "name");
        if (entityName is { }) {
            plugins.Add(FromLocation(ctx, lua, top));
        } else {
            lua.IPairs((lua, i, loc) => {
                plugins.Add(FromLocation(ctx, lua, loc));
            });
        }

        return plugins;
    }

    private static LonnEntityPlugin FromLocation(LuaCtx ctx, Lua lua, int top) {
        var plugin = new LonnEntityPlugin();
        plugin.LuaCtx = ctx;

        plugin.Name = lua.PeekTableStringValue(top, "name") ?? throw new Exception("Name isn't a string!");

        plugin.GetDepth = NullConstOrGetter(plugin, "depth",
            def: 0,
            constGetter: () => lua.PeekTableIntValue(top, "depth") ?? 0,
            funcGetter: static (lua, top) => (int) lua.ToInteger(top)
        );

        plugin.GetRotation = NullConstOrGetter(plugin, "rotation",
            def: 0f,
            constGetter: () => (float) (lua.PeekTableNumberValue(top, "rotation") ?? 0f),
            funcGetter: static (lua, top) => (float) lua.ToNumber(top)
        );

        plugin.GetTexture = NullConstOrGetter(plugin, "texture",
            def: (string?) null,
            constGetter: () => lua.PeekTableStringValue(top, "texture"),
            funcGetter: static (lua, top) => lua.FastToString(top)
        );

        plugin.GetJustification = NullConstOrGetter(plugin, "justification",
            def: new Vector2(0.5f),
            constGetter: () => lua.PeekTableVector2Value(top, "justification"),
            funcGetter: static (lua, top) => lua.ToVector2(top),
            funcResults: 2
        );

        plugin.GetNodeLimits = NullConstOrGetter(plugin, "nodeLimits",
            def: new Point(0, 0),
            constGetter: () => lua.PeekTableVector2Value(top, "nodeLimits").ToPoint(),
            funcGetter: static (lua, top) => lua.ToVector2(top).ToPoint(),
            funcResults: 2
        );

        plugin.GetMinimumSize = NullConstOrGetter(plugin, "minimumSize",
            def: new Point(8, 8),
            constGetter: () => lua.PeekTableVector2Value(top, "minimumSize").ToPoint(),
            funcGetter: static (lua, top) => lua.ToVector2(top).ToPoint(),
            funcResults: 2
        );

        plugin.GetMaximumSize = NullConstOrGetter(plugin, "maximumSize",
            def: new Point(int.MaxValue, int.MaxValue),
            constGetter: () => lua.PeekTableVector2Value(top, "maximumSize").ToPoint(),
            funcGetter: static (lua, top) => lua.ToVector2(top).ToPoint(),
            funcResults: 2
        );

        plugin.GetScale = NullConstOrGetter(plugin, "scale",
            def: Vector2.One,
            constGetter: () => lua.PeekTableVector2Value(top, "scale"),
            funcGetter: static (lua, top) => lua.ToVector2(top),
            funcResults: 2
        );

        plugin.GetColor = NullConstOrGetter(plugin, "color",
            def: Color.White,
            constGetter: () => lua.PeekTableColorValue(top, "color", Color.White),
            funcGetter: static (lua, top) => lua.ToColor(top)
        );

        plugin.GetFillColor = NullConstOrGetter(plugin, "fillColor",
            def: plugin.GetColor,
            constGetter: () => lua.PeekTableColorValue(top, "fillColor", Color.White),
            funcGetter: static (lua, top) => lua.ToColor(top)
        )!;

        plugin.GetBorderColor = NullConstOrGetter(plugin, "borderColor",
            def: plugin.GetColor,
            constGetter: () => lua.PeekTableColorValue(top, "borderColor", Color.White),
            funcGetter: static (lua, top) => lua.ToColor(top)
        )!;

        plugin.GetNodeVisibility = NullConstOrGetter_Entity(plugin, "nodeVisibility",
            def: "selected",
            constGetter: () => lua.PeekTableStringValue(top, "nodeVisibility") ?? "selected",
            funcGetter: static (lua, top) => lua.FastToString(top)
        )!;

        plugin.HasGetSprite = lua.PeekTableType(top, "sprite") is LuaType.Function;
        plugin.HasSelectionFunction = lua.PeekTableType(top, "selection") is LuaType.Function;

        var placementType = lua.GetTable(top, "placements");
        if (placementType == LuaType.Table) {
            var placement1loc = lua.GetTop();

            switch (lua.PeekTableType(placement1loc, "name")) {
                case LuaType.String:
                    // name is provided, so there's 1 placement
                    plugin.Placements.Add(new(lua));
                    break;
                default:
                    lua.IPairs((lua, i, loc) => {
                        plugin.Placements.Add(new(lua));
                    });
                    break;
            }
        }
        lua.Pop(1);

        lua.GetGlobal("_RYSY_entities");
        var entitiesTableLoc = lua.GetTop();
        lua.PushCopy(top);
        lua.SetField(entitiesTableLoc, plugin.Name);
        lua.Pop(1);

        return plugin;
    }

    [return: NotNullIfNotNull(nameof(def))]
    private static Func<Room, Entity, T?>? NullConstOrGetter<T>(LonnEntityPlugin pl, string fieldName, 
        T? def, 
        Func<T> constGetter, 
        Func<Lua, int, T> funcGetter,
        int funcResults = 1
    ) {
        var lua = pl.LuaCtx.Lua;
        var top = lua.GetTop();

        switch (lua.PeekTableType(top, fieldName)) {
            case LuaType.None:
            case LuaType.Nil:
                return def is { } ? (r, e) => def : null;
            case LuaType.Function:
                return (r, e) => {
                    using var token = pl.PushToStack();
                    var lua = pl.LuaCtx.Lua;

                    lua.GetTable(pl.StackLoc, fieldName);
                    var ret = lua.PCallFunction(r, e, funcGetter, results: funcResults)!;

                    return ret;
                };
            default:
                var depth = constGetter();
                return (r, e) => depth;
        }
    }

    [return: NotNullIfNotNull(nameof(def))]
    private static Func<Entity, T?>? NullConstOrGetter_Entity<T>(LonnEntityPlugin pl, string fieldName,
        T? def,
        Func<T> constGetter,
        Func<Lua, int, T> funcGetter,
        int funcResults = 1
    ) {
        var lua = pl.LuaCtx.Lua;
        var top = lua.GetTop();

        switch (lua.PeekTableType(top, fieldName)) {
            case LuaType.None:
            case LuaType.Nil:
                return def is { } ? (e) => def : null;
            case LuaType.Function:
                return (e) => {
                    using var token = pl.PushToStack();
                    var lua = pl.LuaCtx.Lua;

                    lua.PeekTableStringValue(pl.StackLoc, "name");
                    lua.GetTable(pl.StackLoc, fieldName);

                    return lua.PCallFunction(e, funcGetter, results: funcResults)!;
                };
            default:
                var depth = constGetter();
                return (e) => depth;
        }
    }

    private static Func<Room, Entity, T?>? NullConstOrGetter<T>(LonnEntityPlugin pl, string fieldName, 
        Func<Room, Entity, T?>? def, 
        Func<T> constGetter, 
        Func<Lua, int, T> funcGetter,
        int funcResults = 1
    ) {
        var lua = pl.LuaCtx.Lua;
        var top = lua.GetTop();

        switch (lua.PeekTableType(top, fieldName)) {
            case LuaType.None:
            case LuaType.Nil:
                return def;
            case LuaType.Function:
                return (r, e) => {
                    var lua = pl.LuaCtx.Lua;
                    lua.GetTable(pl.StackLoc, fieldName);
                    return lua.PCallFunction(r, e, funcGetter, results: funcResults)!;
                };
            default:
                var depth = constGetter();
                return (r, e) => depth;
        }
    }

    public class LonnPlacement {
        public string Name;
        public Dictionary<string, object> Data = new();

        public LonnPlacement(Lua lua) {
            var start = lua.GetTop();

            Name = lua.PeekTableStringValue(start, "name") ?? throw new Exception("Name isn't a string!");


            if (lua.GetTable(start, "data") is LuaType.Table)
                Data = lua.TableToDictionary(lua.GetTop(), DataKeyBlacklist);

            // pop the "data" table
            lua.Pop(1);
        }

        #warning Remove, once placements support nodes and TableToDictionary supports tables in tables...
        private static readonly HashSet<string> DataKeyBlacklist = new() { "nodes" };
    }
}
