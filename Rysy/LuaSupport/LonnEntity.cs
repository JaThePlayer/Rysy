using KeraLua;
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
            "always" => NodeHelper.GetNodeSpritesFor(this),
            "selected" => Array.Empty<ISprite>(),
            var other => Array.Empty<ISprite>(),
        };
    }

    private List<ISprite> SpritesFromLonn(Lua lua, int top) {
        var list = new List<ISprite>();

        switch (lua.PeekTableType(top, "_type")) {
            case LuaType.String:
                // name is provided, so there's 1 placement
                NextSprite(top, list);
                break;
            default:
                lua.IPairs((lua, i, loc) =>
                {
                    NextSprite(loc, list);
                });
                break;
        }

        return list;

        void NextSprite(int top, List<ISprite> addTo) {
            var type = lua.PeekTableStringValue(top, "_type");
            switch (type) {
                case "drawableSprite":
                    addTo.Add(LuaToSprite(lua, top));
                    break;
                case "drawableLine":
                    addTo.Add(LuaToLine(lua, top));
                    break;
                case "_RYSY_fakeTiles":
                    addTo.AddRange(LuaToFakeTiles(lua, top) ?? Array.Empty<ISprite>());
                    break;
                case "drawableRectangle":
                    addTo.Add(LuaToRect(lua, top));
                    break;
                default:
                    Logger.Write("LonnEntity", LogLevel.Warning, $"Unknown Lonn sprite type: {type}: {lua.TableToDictionary(top).ToJson()}");
                    break;
            }
        }
    }

    private static RectangleSprite LuaToRect(Lua lua, int top) {
        var x = lua.PeekTableFloatValue(top, "x") ?? 0f;
        var y = lua.PeekTableFloatValue(top, "y") ?? 0f;
        var w = lua.PeekTableIntValue(top, "width") ?? 8;
        var h = lua.PeekTableIntValue(top, "height") ?? 8;
        var color = lua.PeekTableColorValue(top, "color", Color.White);
        var mode = lua.PeekTableStringValue(top, "mode");
        var rect = new Rectangle((int) x, (int) y, w, h);

        var sprite = mode switch {
            "fill" => ISprite.Rect(rect, color),
            "line" => ISprite.OutlinedRect(rect, Color.Transparent, color),
            "bordered" => ISprite.OutlinedRect(rect, color, lua.PeekTableColorValue(top, "secondaryColor", Color.White)),
            _ => ISprite.Rect(rect, color),
        };
        return sprite;
    }

    private IEnumerable<ISprite>? LuaToFakeTiles(Lua lua, int top) {
        var x = lua.PeekTableFloatValue(top, "x") ?? 0f;
        var y = lua.PeekTableFloatValue(top, "y") ?? 0f;
        var w = lua.PeekTableIntValue(top, "w") ?? 8;
        var h = lua.PeekTableIntValue(top, "h") ?? 8;
        var material = lua.PeekTableStringValue(top, "material") ?? "3";
        var layer = lua.PeekTableStringValue(top, "layer") ?? "tilesFg";

        var sprites = layer switch {
            "tilesFg" => Room.FG.Autotiler?.GetSprites(new(x, y), material[0], w / 8, h / 8),
            "tilesBg" => Room.BG.Autotiler?.GetSprites(new(x, y), material[0], w / 8, h / 8),
            _ => null
        };
        return sprites;
    }

    private static LineSprite LuaToLine(Lua lua, int top) {
        var color = lua.PeekTableColorValue(top, "color", Color.White);
        var offX = lua.PeekTableFloatValue(top, "offsetX") ?? 0;
        var offY = lua.PeekTableFloatValue(top, "offsetY") ?? 0;
        var magnitudeOffset = lua.PeekTableFloatValue(top, "magnitudeOffset") ?? 0;
        var thickness = lua.PeekTableIntValue(top, "thickness") ?? 1;
        var points = lua.PeekTableNumberList(top, "points") ?? new();

        var pointsVec2 = new Vector2[points.Count / 2];
        for (int i = 0; i < points.Count; i += 2) {
            pointsVec2[i / 2] = new(points[i] + offX, points[i + 1] + offY);
        }

        return new LineSprite(pointsVec2) with {
            Color = color,
            Thickness = thickness,
        };
    }

    private Sprite LuaToSprite(Lua lua, int top) {
        var texture = lua.PeekTableStringValue(top, "_RYSY_INTERNAL_texture") ?? throw new Exception("DrawableSprite doesn't have the '_RYSY_INTERNAL_texture' field set!");
        var x = lua.PeekTableFloatValue(top, "x");
        var y = lua.PeekTableFloatValue(top, "y");
        var scaleX = lua.PeekTableFloatValue(top, "scaleX");
        var scaleY = lua.PeekTableFloatValue(top, "scaleY");
        var originX = lua.PeekTableFloatValue(top, "justificationX");
        var originY = lua.PeekTableFloatValue(top, "justificationY");
        var color = lua.PeekTableColorValue(top, "color", Color.White);
        var rotation = lua.PeekTableFloatValue(top, "rotation");
        var depth = lua.PeekTableIntValue(top, "depth");

        var sprite = ISprite.FromTexture(new Vector2(x ?? Pos.X, y ?? Pos.Y), texture) with {
            Scale = new(scaleX ?? 1f, scaleY ?? 1f),
            Origin = new(originX ?? .5f, originY ?? .5f),
            Color = color,
            Rotation = rotation ?? 0f,
            Depth = depth
        };

        if (lua.PeekTableIntValue(top, "_RYSY_quadX") is { } qx) {
            sprite = sprite.CreateSubtexture(
                qx,
                lua.PeekTableIntValue(top, "_RYSY_quadY") ?? 0,
                lua.PeekTableIntValue(top, "_RYSY_quadW") ?? 0,
                lua.PeekTableIntValue(top, "_RYSY_quadH") ?? 0
            );
        }

        return sprite;
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
}

public sealed class LonnEntityPlugin {
    public LuaCtx LuaCtx { get; private set; }
    public int StackLoc { get; private set; }

    public string Name { get; private set; }

    public Func<Room, Entity, int> GetDepth;

    public Func<Room, Entity, string?>? GetTexture;
    public bool HasGetSprite;

    public Func<Room, Entity, Vector2> GetJustification;
    public Func<Room, Entity, Vector2> GetScale;
    public Func<Room, Entity, float> GetRotation;

    public Func<Room, Entity, Color> GetColor;

    public Func<Room, Entity, Color> GetFillColor;
    public Func<Room, Entity, Color> GetBorderColor;

    public Func<Entity, string> GetNodeVisibility;

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

                    lua.PeekTableStringValue(pl.StackLoc, "name");
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
