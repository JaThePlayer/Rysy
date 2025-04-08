using KeraLua;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;
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

    public Func<ILuaWrapper, Entity, int> GetDepth { get; private set; }
    public Func<ILuaWrapper, Entity, Node, int, int>? NodeDepth { get; private set; }

    public Func<ILuaWrapper, Entity, string?>? GetTexture;
    public Func<ILuaWrapper, Entity, Node, int, string?>? GetNodeTexture;
    
    public bool HasGetSprite;
    public bool HasGetNodeSprite;

    public Func<ILuaWrapper, Entity, Vector2> GetJustification { get; private set; }
    public Func<ILuaWrapper, Entity, Vector2> GetScale  { get; private set; }
    public Func<ILuaWrapper, Entity, float> GetRotation  { get; private set; }
    
    public Func<ILuaWrapper, Entity, Node, int, Vector2> NodeJustification  { get; private set; }
    public Func<ILuaWrapper, Entity, Node, int, Vector2> NodeScale { get; private set; }
    public Func<ILuaWrapper, Entity, Node, int, float> NodeRotation  { get; private set; }
    
    public Func<ILuaWrapper, Entity, Vector2>? GetOffset { get; set; }
    public Func<ILuaWrapper, Entity, Node, int, Vector2>? NodeOffset { get; set; }

    public Func<ILuaWrapper, Entity, Color> GetColor;
    public Func<ILuaWrapper, Entity, Color> GetFillColor;
    public Func<ILuaWrapper, Entity, Color> GetBorderColor;
    
    public Func<ILuaWrapper, Entity, Node, int, Color> NodeColor { get; private set; }
    public bool BothNodeColors { get; private set; }
    public Func<ILuaWrapper, Entity, Node, int, Color> NodeBorderColor { get; private set; }
    public Func<ILuaWrapper, Entity, Node, int, Color> NodeFillColor { get; private set; }

    public Func<Entity, string> GetNodeVisibility;

    public Func<ILuaWrapper, Entity, Range> GetNodeLimits;
    public Func<ILuaWrapper, Entity, Point>? GetMinimumSize;
    public Func<ILuaWrapper, Entity, Point>? GetWarnBelowSize;
    public Func<ILuaWrapper, Entity, Point>? GetMaximumSize;
    public Func<ILuaWrapper, Entity, Point>? GetWarnAboveSize;

    public Func<ILuaWrapper, Entity, Rectangle>? GetRectangle;

    public Func<Entity, List<string>?> GetAssociatedMods { get; private set; }

    //flip(room, entity, horizontal, vertical) -> success?
    public Func<ILuaWrapper, ILuaWrapper, bool, bool, bool>? Flip;
    // rotate(room, entity, direction) -> success?
    public Func<ILuaWrapper, ILuaWrapper, int, bool>? Rotate;
    
    // move(room, entity, nodeIndex, offsetX, offsetY)
    public Action<ILuaWrapper, ILuaWrapper, int, float, float>? Move;
    
    public Func<Entity, string> NodeLineRenderType { get; private set; }
    
    public Func<Entity, Node, int, Vector2>? NodeLineRenderOffset { get; private set; }
    
    public Func<ILuaWrapper, Entity, Node, int, Rectangle>? NodeRectangle { get; private set; }
    
    //triggerText(room, trigger) -> string
    public Func<ILuaWrapper, Entity, string?>? TriggerText { get; private set; }
    
    //category() -> string
    public Func<ILuaWrapper, Entity, string?>? TriggerCategory { get; private set; }

    public bool HasSelectionFunction;

    public List<LonnPlacement> Placements { get; set; } = new();
    public Func<object, FieldList>? FieldList { get; set; }

    private readonly Lock _lock = new();

    public IEnumerable<ISprite> GetNodePathSprites(Entity entity, Func<Entity, int, Vector2>? defaultOffset = null) {
        var lineType = NodeLineRenderType(entity);

        var offsetFunc = defaultOffset;

        if (NodeLineRenderOffset is { } offset) {
            offsetFunc = (Entity e, int i) => e.Nodes[i] + offset(e, e.Nodes[i], i + 1);
        }

        if (offsetFunc is { } f) {
            return lineType switch {
                "line" => NodePathTypes.Line(entity, f),
                "fan" => NodePathTypes.Fan(entity, f),
                "circle" => NodePathTypes.Circle(entity, f, nodeIsCenter: true),
                "none" => NodePathTypes.None,
                _ => NodePathTypes.Line(entity, f),
            };
        }
        
        return lineType switch {
            "line" => NodePathTypes.Line(entity),
            "fan" => NodePathTypes.Fan(entity),
            "circle" => NodePathTypes.Circle(entity, nodeIsCenter: true),
            "none" => NodePathTypes.None,
            _ => NodePathTypes.Line(entity),
        };
    }

    public ref struct StackHolder : IDisposable {
        private LonnEntityPlugin Plugin { get; }
        
        public int Amt { get; }

#pragma warning disable CA2213 // Scope is not disposed. - What?? it definitely is.
        private Lock.Scope _scope;
#pragma warning restore CA2213
        
        public StackHolder(LonnEntityPlugin plugin, int amt) {
            _scope = plugin._lock.EnterScope();
            
            Plugin = plugin;
            Amt = amt;
            
            var lua = plugin.LuaCtx.Lua;
            plugin.SetModNameInLua(lua);

            // push the handler
            lua.GetGlobal("_RYSY_entities");
            var entitiesTableLoc = lua.GetTop();
            lua.PushString(plugin.Name);
            lua.GetTable(entitiesTableLoc);

            plugin.StackLoc = lua.GetTop();
        }
        
        public void Dispose() {
            var lua = Plugin.LuaCtx.Lua;
            lua.Pop(lua.GetTop());
            _scope.Dispose();
        }
    }

    public StackHolder PushToStack() {
        return new StackHolder(this, 2);
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

        var entityName = lua.PeekTableStringValue(top, "name"u8);
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

        plugin.Name = lua.PeekTableStringValue(top, "name"u8) ?? throw new Exception("Name isn't a string!");

        plugin.GetDepth = NullConstOrGetter(plugin, "depth"u8,
            def: 0,
            funcGetter: static (lua, top) => (int) lua.ToInteger(top)
        );
        
        plugin.NodeDepth = NullConstOrGetter_Noded(plugin, "nodeDepth"u8,
            def: null,
            funcGetter: static (lua, top) => (int) lua.ToInteger(top)
        );

        plugin.GetRotation = NullConstOrGetter(plugin, "rotation"u8,
            def: 0f,
            funcGetter: static (lua, top) => (float) lua.ToNumber(top)
        );
        
        plugin.NodeRotation = NullConstOrGetter_Noded(plugin, "nodeRotation"u8,
            def: 0f,
            funcGetter: static (lua, top) => (float) lua.ToNumber(top)
        );

        plugin.GetTexture = NullConstOrGetter(plugin, "texture"u8,
            def: (string?) null,
            funcGetter: static (lua, top) => lua.FastToString(top)
        );
        
        plugin.TriggerText = NullConstOrGetter(plugin, "triggerText"u8,
            def: (string?) null,
            funcGetter: static (lua, top) => lua.FastToString(top)
        );
        
        plugin.TriggerCategory = NullConstOrGetter(plugin, "category"u8,
            def: (string?) null,
            funcGetter: static (lua, top) => lua.FastToString(top)
        );
        
        
        plugin.GetNodeTexture = NullConstOrGetter_Noded(plugin, "nodeTexture"u8,
            def: plugin.GetTexture is {} ? (r, e, n, i) => plugin.GetTexture(r, e) : null,
            funcGetter: static (lua, top) => lua.FastToString(top)
        );

        plugin.GetJustification = NullConstOrGetter(plugin, "justification"u8,
            def: new Vector2(0.5f),
            funcGetter: static (lua, top) => lua.ToVector2(top),
            funcResults: 2
        );
        
        plugin.NodeJustification = NullConstOrGetter_Noded(plugin, "nodeJustification"u8,
            def: new Vector2(0.5f),
            funcGetter: static (lua, top) => lua.ToVector2(top),
            funcResults: 2
        );

        plugin.GetNodeLimits = NullConstOrGetter(plugin, "nodeLimits"u8,
            def: 0..0,
            funcGetter: static (lua, top) => lua.ToRangeNegativeIsFromEnd(top),
            funcResults: 2
        );

        plugin.GetMinimumSize = NullConstOrGetter(plugin, "minimumSize"u8,
            def: null,
            funcGetter: static (lua, top) => lua.ToVector2(top).ToPoint(),
            funcResults: 2
        );
        
        plugin.GetWarnBelowSize = NullConstOrGetter(plugin, "warnBelowSize"u8,
            def: null,
            funcGetter: static (lua, top) => lua.ToVector2(top).ToPoint(),
            funcResults: 2
        );

        plugin.GetMaximumSize = NullConstOrGetter(plugin, "maximumSize"u8,
            def: null,
            funcGetter: static (lua, top) => lua.ToVector2(top).ToPoint(),
            funcResults: 2
        );
        
        plugin.GetWarnAboveSize = NullConstOrGetter(plugin, "warnAboveSize"u8,
            def: null,
            funcGetter: static (lua, top) => lua.ToVector2(top).ToPoint(),
            funcResults: 2
        );

        plugin.GetScale = NullConstOrGetter(plugin, "scale"u8,
            def: Vector2.One,
            funcGetter: static (lua, top) => lua.ToVector2(top),
            funcResults: 2
        );
        
        plugin.NodeScale = NullConstOrGetter_Noded(plugin, "nodeScale"u8,
            def: Vector2.One,
            funcGetter: static (lua, top) => lua.ToVector2(top),
            funcResults: 2
        );
        
        plugin.GetOffset = NullConstOrGetter(plugin, "offset"u8,
            def: null,
            funcGetter: static (lua, top) => lua.ToVector2(top),
            funcResults: 2
        );
        
        plugin.NodeOffset = NullConstOrGetter_Noded(plugin, "nodeOffset"u8,
            def: null,
            funcGetter: static (lua, top) => lua.ToVector2(top),
            funcResults: 2
        );

        plugin.GetColor = NullConstOrGetter(plugin, "color"u8,
            def: Color.White,
            funcGetter: static (lua, top) => lua.ToColor(top, Color.White)
        );

        plugin.GetFillColor = NullConstOrGetter(plugin, "fillColor"u8,
            def: plugin.GetColor,
            funcGetter: static (lua, top) => lua.ToColor(top, Color.White)
        )!;

        plugin.GetBorderColor = NullConstOrGetter(plugin, "borderColor"u8,
            def: plugin.GetColor,
            funcGetter: static (lua, top) => lua.ToColor(top, Color.White)
        )!;
        
        plugin.NodeColor = NullConstOrGetter_Noded(plugin, "nodeColor"u8,
            def: (r,e,n,i) => plugin.GetColor(r, e),
            funcGetter: static (lua, top)  => lua.ToColor(top, Color.White));
        
        plugin.NodeBorderColor = NullConstOrGetter_Noded(plugin, "nodeBorderColor"u8,
            def: (r,e,n,i) => plugin.GetBorderColor(r, e),
            funcGetter: static (lua, top)  => lua.ToColor(top, Color.White));
        
        plugin.NodeFillColor = NullConstOrGetter_Noded(plugin, "nodeFillColor"u8,
            def: (r,e,n,i) => plugin.GetFillColor(r, e),
            funcGetter: static (lua, top)  => lua.ToColor(top, Color.White));

        plugin.BothNodeColors = (lua.PeekTableHasKey(top, "nodeFillColor"u8) || lua.PeekTableHasKey(top, "fillColor"u8))
                             && (lua.PeekTableHasKey(top, "nodeBorderColor"u8) || lua.PeekTableHasKey(top, "borderColor"u8));

        plugin.GetNodeVisibility = NullConstOrGetter_Entity(plugin, "nodeVisibility"u8,
            def: "selected",
            funcGetter: static (lua, top) => lua.FastToString(top)
        )!;

        plugin.GetRectangle = NullConstOrGetter(plugin, "rectangle"u8,
            def: null,
            funcGetter: static (lua, top) => lua.ToRectangle(top)
        );

        plugin.GetAssociatedMods = NullConstOrGetter_Entity(plugin, "associatedMods"u8,
            def: (List<string>)null!,
            funcGetter: static (lua, top) => lua.ToList<string>(top));

        plugin.NodeLineRenderType = NullConstOrGetter_Entity(plugin, "nodeLineRenderType"u8,
            def: "line",
            funcGetter: static (lua, top) => lua.FastToString(top))!;

        switch (lua.PeekTableType(top, "nodeLineRenderOffset"u8)) {
            case LuaType.Table:
                var offset = lua.PeekTableVector2Value(top, "nodeLineRenderOffset"u8);
                plugin.NodeLineRenderOffset = (_, _, _) => offset;
                break;
            case LuaType.Function:
                plugin.NodeLineRenderOffset = (entity, node, nodeId) => {
                    using (plugin.PushToStack()) {
                        var lua = plugin.LuaCtx.Lua;
                        lua.GetTable(plugin.StackLoc, "nodeLineRenderOffset"u8);
                        return lua.PCallFunction(static (lua, pos) => lua.ToVector2(pos), results: 1, entity, node, nodeId);
                    }
                };
                break;
        }

        if (lua.PeekTableType(top, "flip"u8) is LuaType.Function) {
            plugin.Flip = (room, entity, horizontal, vertical) => {
                using var _ = plugin.PushToStack();
                
                var lua = plugin.LuaCtx.Lua;

                lua.GetTable(plugin.StackLoc, "flip"u8);
                return lua.PCallFunction(static (lua, pos) => lua.ToBoolean(pos), results: 1, room, entity, horizontal, vertical);
            };
        }

        if (lua.PeekTableType(top, "rotate"u8) is LuaType.Function) {
            plugin.Rotate = (room, entity, dir) => {
                using var _ = plugin.PushToStack();
                
                var lua = plugin.LuaCtx.Lua;

                lua.GetTable(plugin.StackLoc, "rotate"u8);
                return lua.PCallFunction(static (lua, pos) => lua.ToBoolean(pos), results: 1, room, entity, dir);
            };
        }

        if (lua.PeekTableType(top, "move"u8) is LuaType.Function) {
            plugin.Move = (room, entity, nodeIndex, offsetX, offsetY) => {
                using var _ = plugin.PushToStack();
                var lua = plugin.LuaCtx.Lua;

                lua.GetTable(plugin.StackLoc, "move"u8);
                    
                lua.PCallFunction((_, _) => false, results: 1, room, entity, nodeIndex, offsetX, offsetY);
            };
        }

        if (lua.PeekTableType(top, "nodeRectangle"u8) is LuaType.Function) {
            plugin.NodeRectangle = (room, entity, node, nodeIndex) => {
                using var _ = plugin.PushToStack();
                var lua = plugin.LuaCtx.Lua;

                lua.GetTable(plugin.StackLoc, "nodeRectangle"u8);

                return lua.PCallFunction(static (lua, top) => lua.ToRectangle(top), results: 1, room, entity, node, nodeIndex);
            };
        }

        plugin.HasGetSprite = lua.PeekTableType(top, "sprite"u8) is LuaType.Function;
        plugin.HasGetNodeSprite = lua.PeekTableType(top, "nodeSprite"u8) is LuaType.Function;
        plugin.HasSelectionFunction = lua.PeekTableType(top, "selection"u8) is LuaType.Function;

        LonnPlacement? defaultPlacement = null;

        if (lua.GetTable(top, "placements"u8) == LuaType.Table) {
            var placement1loc = lua.GetTop();

            switch (lua.PeekTableType(placement1loc, "name"u8)) {
                case LuaType.String:
                    // name is provided, so there's 1 placement
                    defaultPlacement = new(lua);
                    plugin.Placements.Add(defaultPlacement);
                    break;
                default:
                    if (lua.GetTable(placement1loc, "default"u8) == LuaType.Table) {
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

        switch (lua.GetTable(top, "fieldInformation"u8)) {
            case LuaType.Table:
                var fieldInfoLoc = lua.GetTop();
                var dict = lua.TableToDictionary(fieldInfoLoc, makeLuaFuncRefs: true);
                var mainPlacement = defaultPlacement ?? plugin.Placements.FirstOrDefault() ?? new();

                plugin.FieldList = (e) => LonnFieldIntoToFieldList(dict, mainPlacement);
                break;
            case LuaType.Function:
                plugin.FieldList = (e) => {
                    using var _ = plugin.PushToStack();
                    
                    var type = lua.GetTable(plugin.StackLoc, "fieldInformation"u8);

                    if (type != LuaType.Function) {
                        lua.Pop(1);
                        return new();
                    }

                    var fields = lua.PCallFunction((ILuaWrapper)e, (lua, i) => {
                        var dict = lua.TableToDictionary(i, makeLuaFuncRefs: true);
                        var mainPlacement = defaultPlacement ?? plugin.Placements.FirstOrDefault() ?? new();

                        return LonnFieldIntoToFieldList(dict, mainPlacement);
                    }) ?? new();

                    return fields;
                };
                break;
            default:
                if (defaultPlacement is { }) {
                    plugin.FieldList = (e) => LonnFieldIntoToFieldList(new(), defaultPlacement);
                } else {
                    plugin.FieldList = (e) => new();
                }
                break;
        }
        lua.Pop(1);

        switch (lua.GetTable(top, "fieldOrder"u8)) {
            case LuaType.Table: {
                var order = lua.ToList(lua.GetTop())?.OfType<string>().ToList();
                if (order is { }) {
                    var origFieldListGetter = plugin.FieldList;
                    plugin.FieldList = (e) => origFieldListGetter(e).Ordered(order);
                }
            }
            break;
            case LuaType.Function: {
                Console.WriteLine($"Field Order is a function: {plugin.Name}");
                var origFieldListGetter = plugin.FieldList;

                plugin.FieldList = (e) => {
                    var fields = origFieldListGetter(e);

                    return fields.Ordered((entity) => {
                        using var _ = plugin.PushToStack();
                        
                        var type = lua.GetTable(plugin.StackLoc, "fieldOrder"u8);

                        if (type != LuaType.Function) {
                            lua.Pop(1);
                            return new();
                        }

                        var order = lua.PCallFunction(entity, (lua, i) => lua.ToList(i)?.OfType<string>().ToList()) ?? [];
                        lua.Pop(1);

                        return order;
                    });
                };
            }
            break;
        }
        lua.Pop(1);
        
        switch (lua.GetTable(top, "ignoredFields"u8)) {
            case LuaType.Table: {
                if (lua.ToList(lua.GetTop())?.OfType<string>().ToList() is { } ignored) {
                    var origFieldListGetter = plugin.FieldList;
                    plugin.FieldList = e => origFieldListGetter(e).SetHiddenFields(ignored);
                }
                break;
            }
            case LuaType.Function: {
                var origFieldListGetter = plugin.FieldList;

                plugin.FieldList = e => {
                    var fields = origFieldListGetter(e);

                    return fields.SetHiddenFields(ctx => {
                        using var _ = plugin.PushToStack();
                        
                        var type = lua.GetTable(plugin.StackLoc, "ignoredFields"u8);

                        if (type != LuaType.Function) {
                            lua.Pop(1);
                            return [];
                        }

                        var order = lua.PCallFunction(ctx, 
                            (lua, i) => lua.ToList(i)?.OfType<string>().ToList()) ?? [];
                        lua.Pop(1);

                        return order;
                    });
                };
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

    private static FieldList LonnFieldIntoToFieldList(Dictionary<string, object> dict, LonnPlacement mainPlacement)
        => LonnFieldIntoToFieldList(dict, mainPlacement.Data);

    public static FieldList LonnFieldIntoToFieldList(Dictionary<string, object> dict, Dictionary<string, object> mainPlacement) {
        var fieldList = new FieldList();

        foreach (var (key, val) in dict) {
            if (val is not Dictionary<string, object> infoDict)
                continue;

            var fieldType = (string?) infoDict!.GetValueOrDefault("fieldType", null);

            var defVal = mainPlacement.GetValueOrDefault(key);
            
            if (Fields.CreateFromLonn(defVal, fieldType, infoDict, key) is { } field) {
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

    internal enum LonnRetrievalStrategy {
        Missing,
        Const,
        Function
    }

    private static LonnRetrievalStrategy NullConstOrGetterImpl(LonnEntityPlugin pl, ReadOnlySpan<byte> fieldName)
        => NullConstOrGetterImpl(pl.LuaCtx.Lua, fieldName);
    
    internal static LonnRetrievalStrategy NullConstOrGetterImpl(Lua lua, ReadOnlySpan<byte> fieldName) {
        var top = lua.GetTop();

        switch (lua.GetFieldRva(top, fieldName)) {
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
    private static Func<ILuaWrapper, Entity, T?>? NullConstOrGetter<T>(LonnEntityPlugin pl, ReadOnlySpan<byte> fieldName,
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
                byte[] fieldNameBytes = fieldName.ToArray();
                return (r, e) => {
                    using var _ = pl.PushToStack();
                    
                    var lua = pl.LuaCtx.Lua;

                    lua.GetTable(pl.StackLoc, fieldNameBytes);
                    return lua.PCallFunction(r, e, funcGetter, results: funcResults)!;
                };
            default:
                return def is { } ? (r, e) => def : null;
        }
    }
    
    [return: NotNullIfNotNull(nameof(def))]
    private static Func<ILuaWrapper, Entity, Node, int, T?>? NullConstOrGetter_Noded<T>(LonnEntityPlugin pl, ReadOnlySpan<byte> fieldName,
        Func<ILuaWrapper, Entity, Node, int, T?>? def,
        Func<Lua, int, T> funcGetter,
        int funcResults = 1
    ) {
        var lua = pl.LuaCtx.Lua;
        var strat = NullConstOrGetterImpl(pl, fieldName);

        switch (strat) {
            case LonnRetrievalStrategy.Const:
                var con = funcGetter(lua, lua.GetTop());
                lua.Pop(1); // pop the field we got from NullConstOrGetterImpl
                return (r, e, n, i) => con;
            case LonnRetrievalStrategy.Function:
                var fieldNameBytes = fieldName.ToArray();
                return (r, e, n, i) => {
                    using var _ = pl.PushToStack();
                    
                    var lua = pl.LuaCtx.Lua;

                    lua.GetTable(pl.StackLoc, fieldNameBytes);
                    return lua.PCallFunction(r, e, n, i, funcGetter, results: funcResults)!;
                };
            default:
                return def;
        }
    }
    
    [return: NotNullIfNotNull(nameof(def))]
    private static Func<ILuaWrapper, Entity, Node, int, T?>? NullConstOrGetter_Noded<T>(LonnEntityPlugin pl, ReadOnlySpan<byte> fieldName,
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
                return (r, e, n, i) => con;
            case LonnRetrievalStrategy.Function:
                var fieldNameBytes = fieldName.ToArray();
                return (r, e, n, i) => {
                    using var _ = pl.PushToStack();
                    
                    var lua = pl.LuaCtx.Lua;

                    lua.GetTable(pl.StackLoc, fieldNameBytes);
                    return lua.PCallFunction(r, e, n, i, funcGetter, results: funcResults)!;
                };
            default:
                return def is {} ? (r, e, n, i) => def : null;
        }
    }

    [return: NotNullIfNotNull(nameof(def))]
    private static Func<Entity, T?>? NullConstOrGetter_Entity<T>(LonnEntityPlugin pl, ReadOnlySpan<byte> fieldName,
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
                var fieldNameBytes = fieldName.ToArray();
                return (r) => {
                    using var _ = pl.PushToStack();
                    var lua = pl.LuaCtx.Lua;

                    lua.GetTable(pl.StackLoc, fieldNameBytes);
                    return lua.PCallFunction(r, funcGetter, results: funcResults)!;
                };
            default:
                return def is { } ? (e) => def : null;
        }
    }

    private static Func<ILuaWrapper, Entity, T?>? NullConstOrGetter<T>(LonnEntityPlugin pl, ReadOnlySpan<byte> fieldName,
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
                var fieldNameBytes = fieldName.ToArray();
                return (r, e) => {
                    using var _ = pl.PushToStack();
                    var lua = pl.LuaCtx.Lua;

                    lua.GetTable(pl.StackLoc, fieldNameBytes);
                    return lua.PCallFunction(r, e, funcGetter, results: funcResults)!;
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

        Name = lua.PeekTableStringValue(start, "name"u8) ?? "default";


        if (lua.GetTable(start, "data"u8) is LuaType.Table)
            Data = lua.TableToDictionary(lua.GetTop(), DataKeyBlacklist, makeLuaFuncRefs: true);
        // pop the "data" table
        lua.Pop(1);
        
        if (lua.GetTable(start, "associatedMods"u8) is LuaType.Table) {
            AssociatedMods = lua.ToList<string>(lua.GetTop());
        }
        // pop the "associatedMods" table
        lua.Pop(1);
    }

#warning Remove, once placements support nodes and TableToDictionary supports tables in tables...
    private static readonly HashSet<string> DataKeyBlacklist = new() { "nodes" };
}
