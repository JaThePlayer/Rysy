﻿using KeraLua;
using Rysy.Extensions;
using Rysy.Mods;
using Rysy.Stylegrounds;
using System.Diagnostics.CodeAnalysis;

namespace Rysy.LuaSupport;
public sealed class LonnStylePlugin {
    public ModMeta? ParentMod { get; internal set; }

    public string Name { get; internal set; }

    public LuaCtx LuaCtx { get; private set; }
    public int StackLoc { get; private set; }

    public LuaStackHolder? StackHolder { get; private set; }

    private object LOCK = new();

    public PlacementList Placements { get; set; } = [];
    public Func<Style, FieldList>? FieldList { get; set; }
    public Func<Style, Dictionary<string, object>> DefaultData { get; set; }
    
    public Func<Style, List<string>?> GetAssociatedMods { get; private set; }

    public record struct LuaStackHolder(LonnStylePlugin Plugin, int Amt) : IDisposable {
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

    private void SetModNameInLua(Lua lua) {
        var modName = ParentMod?.Name ?? string.Empty;
        lua.PushString(modName);
        lua.SetGlobal("_RYSY_CURRENT_MOD");
    }

    public T PushToStack<T>(Func<LonnStylePlugin, T> cb) {
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

    public static List<LonnStylePlugin> FromCtx(LuaCtx ctx) {
        var lua = ctx.Lua;
        var top = lua.GetTop();

        var plugins = new List<LonnStylePlugin>();

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

    private static LonnStylePlugin FromLocation(LuaCtx ctx, Lua lua, int top) {
        var plugin = new LonnStylePlugin();
        plugin.LuaCtx = ctx;

        plugin.Name = lua.PeekTableStringValue(top, "name") ?? throw new Exception("Name isn't a string!");

        plugin.GetAssociatedMods = NullConstOrGetter_Style(plugin, "associatedMods",
            def: (List<string>)null!,
            funcGetter: (lua, top) => lua.ToList<string>(top));
        
        switch (lua.GetTable(top, "defaultData")) {
            case LuaType.Table:
                var data = lua.TableToDictionary(lua.GetTop());
                plugin.DefaultData = (style) => data;
                break;
            case LuaType.None or LuaType.Nil:
                plugin.DefaultData = (style) => new();
                break;
            case LuaType.Function:
                plugin.DefaultData = (style) => {
                    return plugin.PushToStack((plugin) => {
                        var type = lua.GetTable(plugin.StackLoc, "defaultData");

                        if (type != LuaType.Function) {
                            lua.Pop(1);
                            return new();
                        }

                        var fields = lua.PCallFunction((lua, i) => {
                            var dict = lua.TableToDictionary(i);

                            return dict;
                        }) ?? new();
                        lua.Pop(1);

                        return fields;
                    });
                };
                break;
        }
        lua.Pop(1);

        plugin.Placements = new("default");
        
        var defaultPlacement = plugin.Placements[0];

        switch (lua.GetTable(top, "fieldInformation")) {
            case LuaType.Table:
                var fieldInfoLoc = lua.GetTop();
                var dict = lua.TableToDictionary(fieldInfoLoc, makeLuaFuncRefs: true);

                plugin.FieldList = (style) => LonnEntityPlugin.LonnFieldIntoToFieldList(dict, plugin.DefaultData(style));
                break;
            case LuaType.Function:
                plugin.FieldList = (style) => {
                    return plugin.PushToStack((plugin) => {
                        var type = lua.GetTable(plugin.StackLoc, "fieldInformation");

                        if (type != LuaType.Function) {
                            lua.Pop(1);
                            return new();
                        }

                        var fields = lua.PCallFunction((lua, i) => {
                            var dict = lua.TableToDictionary(i, makeLuaFuncRefs: true);

                            return LonnEntityPlugin.LonnFieldIntoToFieldList(dict, plugin.DefaultData(style));
                        }) ?? new();
                        lua.Pop(1);

                        return fields;
                    });
                };
                break;
            default:
                if (defaultPlacement is { }) {
                    plugin.FieldList = (style) => LonnEntityPlugin.LonnFieldIntoToFieldList(new(), plugin.DefaultData(style));
                } else {
                    plugin.FieldList = (style) => new();
                }
                break;
        }
        lua.Pop(1);

        return plugin;
    }
    
    [return: NotNullIfNotNull(nameof(def))]
    private static Func<Style, T?>? NullConstOrGetter_Style<T>(LonnStylePlugin pl, string fieldName,
        T? def,
        Func<Lua, int, T> funcGetter,
        int funcResults = 1
    ) {
        var lua = pl.LuaCtx.Lua;
        var strat = LonnEntityPlugin.NullConstOrGetterImpl(lua, fieldName);

        switch (strat) {
            case LonnEntityPlugin.LonnRetrievalStrategy.Const:
                var con = funcGetter(lua, lua.GetTop());
                lua.Pop(1); // pop the field we got from NullConstOrGetterImpl
                return (r) => con;
            case LonnEntityPlugin.LonnRetrievalStrategy.Function:
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
}
