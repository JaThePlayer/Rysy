using CommunityToolkit.HighPerformance;
using KeraLua;
using Rysy.Extensions;
using Rysy.Mods;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Rysy.LuaSupport;

public sealed class LonnEntityPlugin {
    public ModMeta? ParentMod { get; internal set; }

    public LuaCtx LuaCtx { get; private set; }
    public int StackLoc { get; private set; }

    public string Name { get; private set; }

    public Func<ILuaWrapper, Entity, int> GetDepth;

    public Func<ILuaWrapper, Entity, string?>? GetTexture;
    public bool HasGetSprite;

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

    public bool HasSelectionFunction;

    public List<LonnPlacement> Placements { get; set; } = new();
    public FieldList? FieldList;

    public LuaStackHolder? StackHolder { get; private set; }

    private object LOCK = new();

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
            def: "always",
            funcGetter: static (lua, top) => lua.FastToString(top)
        )!;

        
        plugin.GetRectangle = NullConstOrGetter(plugin, "rectangle",
            def: null,
            funcGetter: static (lua, top) => lua.ToRectangle(top)
        );

        plugin.HasGetSprite = lua.PeekTableType(top, "sprite") is LuaType.Function;
        plugin.HasSelectionFunction = lua.PeekTableType(top, "selection") is LuaType.Function;

        if (lua.GetTable(top, "placements") == LuaType.Table) {
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

        if (lua.GetTable(top, "fieldInformation") == LuaType.Table) {
            var fieldInfoLoc = lua.GetTop();
            var dict = lua.TableToDictionary(fieldInfoLoc);
            var mainPlacement = plugin.Placements.FirstOrDefault() ?? new();

            var fieldList = new FieldList();
            foreach (var (key, val) in dict) {
                if (val is not Dictionary<string, object> infoDict)
                    continue;

                IField? field = null;

                var editable = (bool) infoDict.GetValueOrDefault("editable", true);
                var options = infoDict!.GetValueOrDefault("options", null);

                if (options is { } && mainPlacement.Data.TryGetValue(key, out var def)) {
                    switch (def, editable, options) {
                        case (string str, true, List<object> dropdownOptions):
                            if (dropdownOptions.First() is List<object>) {
                                /*
                                 * {text, value},
                                 * {text, value2},
                                 */
                                field = Fields.EditableDropdown(str, dropdownOptions.Cast<List<object>>().ToDictionary(l => l[1], l => l[0].ToString()!));
                            } else {
                                field = Fields.EditableDropdown(str, dropdownOptions.Select(o => o.ToString()!).ToList());
                            }
                            break;
                        case (string str, false, List<object> dropdownOptions):
                            if (dropdownOptions.First() is List<object>) {
                                /*
                                 * {text, value},
                                 * {text, value2},
                                 */
                                field = Fields.Dropdown(str, dropdownOptions.Cast<List<object>>().ToDictionary(l => l[1], l => l[0]!.ToString()!));
                            } else {
                                field = Fields.Dropdown(str, dropdownOptions.Select(o => o.ToString()!).ToList());
                            }


                            break;
                        case (string str, true, Dictionary<string, object> dropdownOptions): {
                            var firstVal = dropdownOptions.FirstOrDefault().Value;

                            field = firstVal switch {
                                string => Fields.EditableDropdown(str, dropdownOptions.ToDictionary(v => (string) v.Value, v => v.Key)),
                            };
                            break;
                        }

                        case (string str, false, Dictionary<string, object> dropdownOptions): {
                            var firstVal = dropdownOptions.FirstOrDefault().Value;

                            field = firstVal switch {
                                string => Fields.Dropdown(str, dropdownOptions.ToDictionary(v => (string) v.Value, v => v.Key)),
                            };
                            break;
                        }

                        default:
                            break;
                    }
                }

                if (field is { }) {
                    fieldList[key] = field;
                }
            }

            plugin.FieldList = fieldList;
        }
        lua.Pop(1);

        lua.GetGlobal("_RYSY_entities");
        var entitiesTableLoc = lua.GetTop();
        lua.PushCopy(top);
        lua.SetField(entitiesTableLoc, plugin.Name);
        lua.Pop(1);

        return plugin;
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

    public class LonnPlacement {
        public string Name;
        public Dictionary<string, object> Data = new();

        internal LonnPlacement() {
            Name = "";
        }

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
