using KeraLua;
using Rysy.Extensions;
using Rysy.Gui.FieldTypes;
using Rysy.Mods;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime;

namespace Rysy.LuaSupport;

public sealed class LonnEntityPlugin {
    public ModMeta? ParentMod { get; internal set; }

    public LuaCtx LuaCtx { get; private set; }
    public int StackLoc { get; private set; }

    public string Name { get; private set; }

    public Func<ILuaWrapper, Entity, int> GetDepth;

    public Func<ILuaWrapper, Entity, string?>? GetTexture;
    public bool HasGetSprite;
    public bool HasGetNodeSprite;

    public Func<ILuaWrapper, Entity, Vector2> GetJustification;
    public Func<ILuaWrapper, Entity, Vector2> GetScale { get; set; }
    public Func<ILuaWrapper, Entity, float> GetRotation;

    public Func<ILuaWrapper, Entity, Color> GetColor;
    public Func<ILuaWrapper, Entity, Color> GetFillColor;
    public Func<ILuaWrapper, Entity, Color> GetBorderColor;

    public Func<Entity, string> GetNodeVisibility;

    public Func<ILuaWrapper, Entity, Range> GetNodeLimits;
    public Func<ILuaWrapper, Entity, Point>? GetMinimumSize;
    public Func<ILuaWrapper, Entity, Point> GetMaximumSize;

    public Func<ILuaWrapper, Entity, Rectangle>? GetRectangle;

    public Func<Entity, List<string>?> GetAssociatedMods;

    //flip(room, entity, horizontal, vertical) -> success?
    public Func<ILuaWrapper, ILuaWrapper, bool, bool, bool>? Flip;
    // rotate(room, entity, direction) -> success?
    public Func<ILuaWrapper, ILuaWrapper, int, bool>? Rotate;

    public bool HasSelectionFunction;

    public List<LonnPlacement> Placements { get; set; } = new();
    public Func<FieldList>? FieldList { get; set; }

    public LuaStackHolder? StackHolder { get; private set; }

    private object LOCK = new();

    public record struct LuaStackHolder(LonnEntityPlugin Plugin, int Amt) : IDisposable {
        public void Dispose() {
            if (Plugin is null)
                return;

            /*
            var lua = Plugin.LuaCtx.Lua;

            if (lua.GetTop() < Amt) {
                //Console.WriteLine("SKIPPING");
                Logger.Write("LonnEntity", LogLevel.Warning, $"Lua stack shrunk after using {Plugin.Name}! Previous: {Plugin.StackLoc}, now: {lua.GetTop()}.");
                lua.PrintStack();
                lua.Pop(lua.GetTop());
                Console.WriteLine(new StackTrace());
                //Logger.Write("LonnEntity", LogLevel.Warning, $"Top element on stack: {lua.ToCSharp(lua.GetTop()).ToJson()}");
                return;
            }

            if (Plugin.StackLoc < lua.GetTop()) {
                Logger.Write("LonnEntity", LogLevel.Warning, $"Lua stack grew after using {Plugin.Name}! Previous: {Plugin.StackLoc}, now: {lua.GetTop()}.");
                lua.PrintStack();
                //Logger.Write("LonnEntity", LogLevel.Warning, $"Top element on stack: {lua.TableToDictionary(lua.GetTop()).ToJson()}");
                Logger.Write("LonnEntity", LogLevel.Warning, $"Top element on stack: {lua.ToCSharp(lua.GetTop()).ToJson()}");
                Console.WriteLine(new StackTrace());
            }
            lua.Pop(Amt);

            Plugin.StackHolder = null;*/
            var lua = Plugin.LuaCtx.Lua;
            lua.Pop(lua.GetTop());
        }
    }

    /*
    public LuaStackHolder? PushToStack() {
        var lua = LuaCtx.Lua;

        SetModNameInLua(lua);

        lua.GetGlobal("_RYSY_entities");
        var entitiesTableLoc = lua.GetTop();

        lua.PushString(Name);
        lua.GetTable(entitiesTableLoc);

        StackLoc = lua.GetTop();

        holder = new(this, 2);
        StackHolder = holder;

        return holder;
    }*/

    public T PushToStack<T>(Func<LonnEntityPlugin, T> cb) {
        lock (LOCK) {
            var lua = LuaCtx.Lua;
            SetModNameInLua(lua);

            // push the handler
            lua.GetGlobal("_RYSY_entities");
            var entitiesTableLoc = lua.GetTop();
            lua.PushString(Name);
            lua.GetTable(entitiesTableLoc);

            StackLoc = lua.GetTop();

            using var holder = new LuaStackHolder(this, 2);

            return cb(this);
        }
    }

    private void SetModNameInLua(Lua lua) {
        var modName = ParentMod?.Name ?? string.Empty;
        lua.PushString(modName);
        lua.SetGlobal("_RYSY_CURRENT_MOD");
    }

    public static LonnEntityPlugin Default(LuaCtx ctx, string sid) {
        ctx.Lua.DoString($$"""
            return {
                name = "{{sid}}",
                depth = 0
            }
            """);

        var pl = FromCtx(ctx);

        ctx.Lua.Pop(1);

        return pl[0];
    }

    public static List<LonnEntityPlugin> FromCtx(LuaCtx ctx) {
        var lua = ctx.Lua;
        var top = lua.GetTop();

        var plugins = new List<LonnEntityPlugin>();

        if (lua.Type(top) != LuaType.Table) {
            return new();
        }

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
            funcGetter: static (lua, top) => (int) lua.ToInteger(top)
        );

        plugin.GetRotation = NullConstOrGetter(plugin, "rotation",
            def: 0f,
            funcGetter: static (lua, top) => (float) lua.ToNumber(top)
        );

        plugin.GetTexture = NullConstOrGetter(plugin, "texture",
            def: (string?) null,
            funcGetter: static (lua, top) => lua.FastToString(top)
        );

        plugin.GetJustification = NullConstOrGetter(plugin, "justification",
            def: new Vector2(0.5f),
            funcGetter: static (lua, top) => lua.ToVector2(top),
            funcResults: 2
        );

        plugin.GetNodeLimits = NullConstOrGetter(plugin, "nodeLimits",
            def: 0..0,
            funcGetter: static (lua, top) => lua.ToRangeNegativeIsFromEnd(top),
            funcResults: 2
        );

        plugin.GetMinimumSize = NullConstOrGetter(plugin, "minimumSize",
            def: null,
            funcGetter: static (lua, top) => lua.ToVector2(top).ToPoint(),
            funcResults: 2
        );

        plugin.GetMaximumSize = NullConstOrGetter(plugin, "maximumSize",
            def: new Point(int.MaxValue, int.MaxValue),
            funcGetter: static (lua, top) => lua.ToVector2(top).ToPoint(),
            funcResults: 2
        );

        plugin.GetScale = NullConstOrGetter(plugin, "scale",
            def: Vector2.One,
            funcGetter: static (lua, top) => lua.ToVector2(top),
            funcResults: 2
        );

        plugin.GetColor = NullConstOrGetter(plugin, "color",
            def: Color.White,
            funcGetter: static (lua, top) => lua.ToColor(top, Color.White)
        );

        plugin.GetFillColor = NullConstOrGetter(plugin, "fillColor",
            def: plugin.GetColor,
            funcGetter: static (lua, top) => lua.ToColor(top, Color.White)
        )!;

        plugin.GetBorderColor = NullConstOrGetter(plugin, "borderColor",
            def: plugin.GetColor,
            funcGetter: static (lua, top) => lua.ToColor(top, Color.White)
        )!;

        plugin.GetNodeVisibility = NullConstOrGetter_Entity(plugin, "nodeVisibility",
            def: "selected",
            funcGetter: static (lua, top) => lua.FastToString(top)
        )!;


        plugin.GetRectangle = NullConstOrGetter(plugin, "rectangle",
            def: null,
            funcGetter: static (lua, top) => lua.ToRectangle(top)
        );

        plugin.GetAssociatedMods = NullConstOrGetter_Entity(plugin, "associatedMods",
            def: (List<string>)null!,
            funcGetter: (lua, top) => lua.ToList<string>(top));

        if (lua.PeekTableType(top, "flip") is LuaType.Function) {
            plugin.Flip = (room, entity, horizontal, vertical) => {
                return plugin.PushToStack((pl) => {
                    var lua = pl.LuaCtx.Lua;

                    lua.GetTable(pl.StackLoc, "flip");
                    return lua.PCallFunction((lua, pos) => lua.ToBoolean(pos), results: 1, room, entity, horizontal, vertical);
                });
            };
        }

        if (lua.PeekTableType(top, "rotate") is LuaType.Function) {
            plugin.Rotate = (room, entity, dir) => {
                return plugin.PushToStack((pl) => {
                    var lua = pl.LuaCtx.Lua;

                    lua.GetTable(pl.StackLoc, "rotate");
                    return lua.PCallFunction((lua, pos) => lua.ToBoolean(pos), results: 1, room, entity, dir);
                });
            };
        }

        plugin.HasGetSprite = lua.PeekTableType(top, "sprite") is LuaType.Function;
        plugin.HasGetNodeSprite = lua.PeekTableType(top, "nodeSprite") is LuaType.Function;
        plugin.HasSelectionFunction = lua.PeekTableType(top, "selection") is LuaType.Function;

        LonnPlacement? defaultPlacement = null;

        if (lua.GetTable(top, "placements") == LuaType.Table) {
            var placement1loc = lua.GetTop();

            switch (lua.PeekTableType(placement1loc, "name")) {
                case LuaType.String:
                    // name is provided, so there's 1 placement
                    plugin.Placements.Add(new(lua));
                    break;
                default:
                    if (lua.GetTable(placement1loc, "default") == LuaType.Table) {
                        //plugin.Placements.Add(new(lua));
                        defaultPlacement = new(lua);
                    }
                    lua.Pop(1);

                    lua.IPairs((lua, i, loc) => {
                        plugin.Placements.Add(new(lua));
                    });
                    break;
            }
        }
        lua.Pop(1);

        switch (lua.GetTable(top, "fieldInformation")) {
            case LuaType.Table:
                var fieldInfoLoc = lua.GetTop();
                var dict = lua.TableToDictionary(fieldInfoLoc);
                var mainPlacement = defaultPlacement ?? plugin.Placements.FirstOrDefault() ?? new();

                plugin.FieldList = () => LonnFieldIntoToFieldList(dict, mainPlacement);
                break;
            case LuaType.Function:
                plugin.FieldList = () => {
                    return plugin.PushToStack((plugin) => {
                        var type = lua.GetTable(plugin.StackLoc, "fieldInformation");

                        if (type != LuaType.Function) {
                            lua.Pop(1);
                            return new();
                        }

                        var fields = lua.PCallFunction((lua, i) => {
                            var dict = lua.TableToDictionary(i);
                            var mainPlacement = defaultPlacement ?? plugin.Placements.FirstOrDefault() ?? new();

                            return LonnFieldIntoToFieldList(dict, mainPlacement);
                        }) ?? new();

                        return fields;
                    });
                };
                break;
            default:
                if (defaultPlacement is { }) {
                    plugin.FieldList = () => LonnFieldIntoToFieldList(new(), defaultPlacement);
                } else {
                    plugin.FieldList = () => new();
                }
                break;
        }
        lua.Pop(1);

        switch (lua.GetTable(top, "fieldOrder")) {
            case LuaType.Table: {
                var order = lua.ToList(lua.GetTop())?.OfType<string>().ToList();
                if (order is { }) {
                    var origFieldListGetter = plugin.FieldList;
                    plugin.FieldList = () => origFieldListGetter().Ordered(order);
                }
            }
            break;
            case LuaType.Function: {
                Console.WriteLine($"Field Order is a function: {plugin.Name}");
                var origFieldListGetter = plugin.FieldList;

                plugin.FieldList = () => {
                    var fields = origFieldListGetter();

                    fields.Ordered((entity) => {
                        return plugin.PushToStack((plugin) => {
                            var type = lua.GetTable(plugin.StackLoc, "fieldOrder");

                            if (type != LuaType.Function) {
                                lua.Pop(1);
                                return new();
                            }

                            var order = lua.PCallFunction(entity, (lua, i) => {
                                return lua.ToList(i)?.OfType<string>().ToList();
                            }) ?? new();
                            lua.Pop(1);

                            return order;
                        });
                    });

                    return fields;
                };
            }
            break;
            default:
                break;
        }
        lua.Pop(1);

        lua.GetGlobal("_RYSY_entities");
        var entitiesTableLoc = lua.GetTop();
        lua.PushCopy(top);
        lua.SetField(entitiesTableLoc, plugin.Name);
        lua.Pop(1);

        return plugin;
    }

    private static FieldList LonnFieldIntoToFieldList(Dictionary<string, object> dict, LonnPlacement mainPlacement)
        => LonnFieldIntoToFieldList(dict, mainPlacement.Data);

    public static FieldList LonnFieldIntoToFieldList(Dictionary<string, object> dict, Dictionary<string, object> mainPlacement) {
        var fieldList = new FieldList();

        foreach (var (key, val) in dict) {
            if (val is not Dictionary<string, object> infoDict)
                continue;


            var editable = (bool) infoDict.GetValueOrDefault("editable", true);
            var options = infoDict!.GetValueOrDefault("options", null);
            var fieldType = (string?) infoDict!.GetValueOrDefault("fieldType", null);
            var min = infoDict!.GetValueOrDefault("minimumValue", null);
            var max = infoDict!.GetValueOrDefault("maximumValue", null);

            var list = infoDict!.GetValueOrDefault("ext:list##todo", null); // for now, the spec is still undefined for this, but I'll leave in the code for later
            var (listSeparator, listMin, listMax) = list switch {
                true => (",", 1, -1),
                Dictionary<string, object> listInfo => (
                    listInfo.GetValueOrDefault("separator", ",").ToString()!,
                    Convert.ToInt32(listInfo.GetValueOrDefault("minElements", 1), CultureInfo.InvariantCulture),
                    Convert.ToInt32(listInfo.GetValueOrDefault("maxElements", -1), CultureInfo.InvariantCulture)
                ),
                _ => (",", -1, -1),
            };

            var defVal = mainPlacement.GetValueOrDefault(key);
            if (list is { } && defVal is string defAsString)
                defVal = defAsString.Split(listSeparator).FirstOrDefault();

            Field? field = null;
            // ignore fields we can guess - no need to duplicate code for guessed values
            //if (fieldType is { } && fieldType is not "string" and not "number" and not "boolean" and not "anything")
                field = Fields.CreateFromLonn(defVal, fieldType, infoDict);

            if (field is IntField intField) {
                if (min is { })
                    intField.WithMin(Convert.ToInt32(min, CultureInfo.InvariantCulture));
                if (max is { })
                    intField.WithMax(Convert.ToInt32(max, CultureInfo.InvariantCulture));
            }

            if (field is FloatField floatField) {
                if (min is { })
                    floatField.WithMin(Convert.ToSingle(min, CultureInfo.InvariantCulture));
                if (max is { })
                    floatField.WithMax(Convert.ToSingle(max, CultureInfo.InvariantCulture));
            }

            if (options is { }) {
                field = HandleDropdown(editable, options, mainPlacement, key, fieldType) ?? field;
            }

            if (list is { } && field is { }) {
                field = new ListField(field) {
                    Separator = listSeparator,
                    MinElements = listMin,
                    MaxElements = listMax,
                };
            }

            if (field is { }) {
                fieldList[key] = field;
            }
        }

        // take into account properties not defined in fieldInfo but only in the main placement
        foreach (var (key, val) in mainPlacement) {
            if (fieldList.ContainsKey(key))
                continue;

            if (Fields.GuessFromValue(val, fromMapData: true) is { } guessed)
                fieldList[key] = guessed;
        }

        return fieldList;
    }

    private static Field? HandleDropdown(bool editable, object? options, Dictionary<string, object> mainPlacement, string key, string? fieldType) {
        if (options is { } && mainPlacement.TryGetValue(key, out var def)) {
            switch (options) {
                case List<object> dropdownOptions:
                    if (dropdownOptions.First() is List<object>) {
                        // {text, value},
                        // {text, value2},
                        return fieldType switch {
                            "integer" => Fields.Dropdown(Convert.ToInt32(def, CultureInfo.InvariantCulture), dropdownOptions.Cast<List<object>>().SafeToDictionary(l => Convert.ToInt32(l[1], CultureInfo.InvariantCulture), l => l[0].ToString()!), editable),
                            _ => Fields.Dropdown(def, dropdownOptions.Cast<List<object>>().SafeToDictionary(l => l[1], l => l[0].ToString()!), editable)
                        };
                    } else {
                        return fieldType switch {
                            "integer" => Fields.Dropdown(Convert.ToInt32(def, CultureInfo.InvariantCulture), dropdownOptions.Select(o => Convert.ToInt32(o, CultureInfo.InvariantCulture)).ToList(), editable: editable),
                            _ => Fields.Dropdown(def, dropdownOptions.Select(o => o).ToList(), editable: editable),
                        };
                    }
                case Dictionary<string, object> dropdownOptions: {
                    var firstVal = dropdownOptions.FirstOrDefault().Value;

                    return fieldType switch {
                        "integer" => Fields.Dropdown((int)Convert.ToDouble(def, CultureInfo.InvariantCulture), dropdownOptions.SafeToDictionary(v => (int) Convert.ToDouble(v.Value, CultureInfo.InvariantCulture), v => v.Key), editable),
                        _ => Fields.Dropdown(def, dropdownOptions.SafeToDictionary(v => v.Value, v => v.Key), editable),
                    };
                }
                default:
                    break;
            }
        }

        return null;
    }

    private enum LonnRetrievalStrategy {
        Missing,
        Const,
        Function
    }

    private static LonnRetrievalStrategy NullConstOrGetterImpl(LonnEntityPlugin pl, string fieldName) {
        var lua = pl.LuaCtx.Lua;
        var top = lua.GetTop();

        switch (lua.GetField(top, fieldName)) {
            case LuaType.None:
            case LuaType.Nil:
                lua.Pop(1);
                return LonnRetrievalStrategy.Missing;
            case LuaType.Function:
                lua.Pop(1);
                return LonnRetrievalStrategy.Function;
            case LuaType.Table:
                // selene lambdas use tables with a metatable...
                var callType = lua.GetMetaField(lua.GetTop(), "__call");
                if (callType is not LuaType.None and not LuaType.Nil) {
                    lua.Pop(2); // pop the metafield and field
                    return LonnRetrievalStrategy.Function;
                }

                // intentionally leave 1 element on the stack
                return LonnRetrievalStrategy.Const;
            default:
                // intentionally leave 1 element on the stack
                return LonnRetrievalStrategy.Const;
        }
    }

    [return: NotNullIfNotNull(nameof(def))]
    private static Func<ILuaWrapper, Entity, T?>? NullConstOrGetter<T>(LonnEntityPlugin pl, string fieldName,
        T? def,
        Func<Lua, int, T> funcGetter,
        int funcResults = 1
    ) {
        var lua = pl.LuaCtx.Lua;
        var strat = NullConstOrGetterImpl(pl, fieldName);

        switch (strat) {
            case LonnRetrievalStrategy.Const:
                var con = funcGetter(lua, lua.GetTop());
                lua.Pop(1); // pop the field we got from NullConstOrGetterImpl
                return (r, e) => con;
            case LonnRetrievalStrategy.Function:
                return (r, e) => {
                    return pl.PushToStack((pl) => {
                        var lua = pl.LuaCtx.Lua;

                        lua.GetTable(pl.StackLoc, fieldName);
                        return lua.PCallFunction(r, e, funcGetter, results: funcResults)!;
                    });
                };
            default:
                return def is { } ? (r, e) => def : null;
        }
    }

    [return: NotNullIfNotNull(nameof(def))]
    private static Func<Entity, T?>? NullConstOrGetter_Entity<T>(LonnEntityPlugin pl, string fieldName,
        T? def,
        Func<Lua, int, T> funcGetter,
        int funcResults = 1
    ) {
        var lua = pl.LuaCtx.Lua;
        var strat = NullConstOrGetterImpl(pl, fieldName);

        switch (strat) {
            case LonnRetrievalStrategy.Const:
                var con = funcGetter(lua, lua.GetTop());
                lua.Pop(1); // pop the field we got from NullConstOrGetterImpl
                return (r) => con;
            case LonnRetrievalStrategy.Function:
                return (r) => {
                    return pl.PushToStack((pl) => {
                        var lua = pl.LuaCtx.Lua;

                        lua.GetTable(pl.StackLoc, fieldName);
                        return lua.PCallFunction(r, funcGetter, results: funcResults)!;
                    });
                };
            default:
                return def is { } ? (e) => def : null;
        }
    }

    private static Func<ILuaWrapper, Entity, T?>? NullConstOrGetter<T>(LonnEntityPlugin pl, string fieldName,
        Func<ILuaWrapper, Entity, T?>? def,
        Func<Lua, int, T> funcGetter,
        int funcResults = 1
    ) {
        var lua = pl.LuaCtx.Lua;
        var strat = NullConstOrGetterImpl(pl, fieldName);

        switch (strat) {
            case LonnRetrievalStrategy.Const:
                var con = funcGetter(lua, lua.GetTop());
                lua.Pop(1); // pop the field we got from NullConstOrGetterImpl
                return (r, e) => con;
            case LonnRetrievalStrategy.Function:
                return (r, e) => {
                    return pl.PushToStack((pl) => {
                        var lua = pl.LuaCtx.Lua;

                        lua.GetTable(pl.StackLoc, fieldName);
                        return lua.PCallFunction(r, e, funcGetter, results: funcResults)!;
                    });
                };
            default:
                return def;
        }
    }

}
public class LonnPlacement {
    public string Name;
    public Dictionary<string, object> Data = new();
    public List<string>? AssociatedMods;

    internal LonnPlacement() {
        Name = "";
    }

    public LonnPlacement(Lua lua, int? loc = null) {
        var start = loc ?? lua.GetTop();

        Name = lua.PeekTableStringValue(start, "name") ?? "default";


        if (lua.GetTable(start, "data") is LuaType.Table)
            Data = lua.TableToDictionary(lua.GetTop(), DataKeyBlacklist);
        // pop the "data" table
        lua.Pop(1);
        
        if (lua.GetTable(start, "associatedMods") is LuaType.Table) {
            AssociatedMods = lua.ToList<string>(lua.GetTop());
        }
        // pop the "associatedMods" table
        lua.Pop(1);
    }

#warning Remove, once placements support nodes and TableToDictionary supports tables in tables...
    private static readonly HashSet<string> DataKeyBlacklist = new() { "nodes" };
}
