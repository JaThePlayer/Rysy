using Microsoft.CodeAnalysis;
using Rysy.Entities;
using Rysy.Extensions;
using Rysy.LuaSupport;
using Rysy.Mods;
using Rysy.Scenes;
using System.Reflection;

namespace Rysy;

public static class EntityRegistry {
    static EntityRegistry() {
        RegisterHardcoded();
    }

    private static Dictionary<string, Type> SIDToType { get; set; } = new(StringComparer.Ordinal);
    private static Dictionary<string, LonnEntityPlugin> SIDToLonnPlugin { get; set; } = new(StringComparer.Ordinal);
    private static Dictionary<string, Func<FieldList>> SIDToFields { get; set; } = new(StringComparer.Ordinal);
    private static Dictionary<string, ModMeta> SIDToMod { get; set; } = new(StringComparer.Ordinal);

    public static List<Placement> EntityPlacements { get; set; } = new();
    public static List<Placement> TriggerPlacements { get; set; } = new();
    public static List<Placement> StylegroundPlacements { get; set; } = new();

    private static LuaCtx? _LuaCtx = null;
    private static LuaCtx LuaCtx => _LuaCtx ??= LuaCtx.CreateNew();

    public const string FGDecalSID = "fgDecal";
    public const string BGDecalSID = "bgDecal";

    public static FieldList GetFields(Entity entity)
        => GetFields(entity.Name);

    public static FieldList GetFields(string sid) {
        if (SIDToFields.TryGetValue(sid, out var getter)) {
            return getter();
        }

        return new();
    }

    public static Type? GetTypeForSID(string sid) {
        if (SIDToType.TryGetValue(sid, out var type))
            return type;

        return null;
    }

    public static ModMeta? GetMod(string sid) {
        return SIDToMod.GetValueOrDefault(sid);
    }

    public static async ValueTask RegisterAsync(bool loadLuaPlugins = true, bool loadCSharpPlugins = true) {
        _LuaCtx = null;
        SIDToType.Clear();
        SIDToLonnPlugin.Clear();
        SIDToFields.Clear();
        EntityPlacements.Clear();
        TriggerPlacements.Clear();
        SIDToMod.Clear();

        RegisterHardcoded();

        const string baseText = "Registering entities:";
        LoadingScene.Text = baseText;
        using var watch = new ScopedStopwatch("Registering entities");

        /*
        if (loadLuaPlugins && Settings.Instance.LonnPluginPath is { } path) {
            using var w = new ScopedStopwatch("Registering Lua entities");
            LoadingScene.Text = $"{baseText} Loenn";

            foreach (var item in Directory.EnumerateFiles(Path.Combine(path, "entities"), "*.lua")) {
                RegisterFromLua(File.ReadAllText(item), Path.GetFileName(item), trigger: false);
            }

            foreach (var item in Directory.EnumerateFiles(Path.Combine(path, "triggers"), "*.lua")) {
                RegisterFromLua(File.ReadAllText(item), Path.GetFileName(item), trigger: true);
            }
        }*/

        foreach (var (_, mod) in ModRegistry.Mods) {
            LoadingScene.Text = $"{baseText} {mod.Name}";

            LoadPluginsFromMod(mod, loadLuaPlugins, loadCSharpPlugins);
        }

        ModRegistry.RegisterModAssemblyScanner(ModScanner);
    }

    private static void ModScanner(ModMeta mod, Assembly? oldAsm) {
        if (oldAsm is { }) {
            foreach (var t in GetEntityTypesFromAsm(oldAsm)) {
                foreach (var sid in GetSIDsForType(t)) {
                    SIDToType.Remove(sid);
                    SIDToMod.Remove(sid);
                    EntityPlacements.RemoveAll(pl => !pl.FromLonn && pl.SID == sid);
                }
            }
        }

        if (mod.PluginAssembly is { } asm) {
            RegisterFrom(asm, mod);
        }

        // perform a quick reload to make entities use their new C# type.
        if (RysyEngine.Scene is EditorScene editor) {
            editor.QuickReload();
        }
    }

    private static void LoadPluginsFromMod(ModMeta mod, bool loadLuaPlugins, bool loadCSharpPlugins) {
        if (loadLuaPlugins)
            foreach (var pluginPath in mod.Filesystem.FindFilesInDirectoryRecursive("Loenn", "lua").ToList()) {
                if (pluginPath.StartsWith("Loenn/entities", StringComparison.Ordinal)) {
                    LoadLuaPluginFromModFile(mod, pluginPath, trigger: false);

                } else if (pluginPath.StartsWith("Loenn/triggers", StringComparison.Ordinal)) {
                    LoadLuaPluginFromModFile(mod, pluginPath, trigger: true);
                }
            }
    }

    private static void LoadLuaPluginFromModFile(ModMeta mod, string pluginPath, bool trigger) {
        mod.Filesystem.TryWatchAndOpen(pluginPath, stream => {
            var plugin = stream.ReadAllText();

            RegisterFromLua(plugin, pluginPath, trigger, mod);
        });
    }

    private static void RegisterHardcoded() {
        SIDToType[FGDecalSID] = typeof(Decal);
        SIDToType[BGDecalSID] = typeof(Decal);

        var decalFields = Decal.GetFields();

        SIDToFields[FGDecalSID] = () => decalFields;
        SIDToFields[BGDecalSID] = () => decalFields;
    }

    public static void RegisterFromLua(string lua, string chunkName, bool trigger, ModMeta? mod = null) {
        try {
            LuaCtx.Lua.SetCurrentModName(mod);
            LuaCtx.Lua.PCallStringThrowIfError(lua, chunkName, results: 1);
            List<LonnEntityPlugin>? plugins = null;

            try {
                plugins = LonnEntityPlugin.FromCtx(LuaCtx);
            } finally {
                LuaCtx.Lua.Pop(1);
            }

            foreach (var pl in plugins) {
                pl.ParentMod = mod;

                lock (SIDToType) {
                    SIDToType[pl.Name] = trigger ? typeof(LonnTrigger) : typeof(LonnEntity);
                }
                lock (SIDToLonnPlugin) {
                    SIDToLonnPlugin[pl.Name] = pl;
                }
                if (mod is { })
                    lock (SIDToMod) {
                        if (!SIDToMod.ContainsKey(pl.Name))
                            SIDToMod[pl.Name] = mod;
                    }

                RegisterLuaPlacements(pl.Name, trigger, pl.Placements);

                if (pl.FieldList is { } fields) {
                    lock (SIDToFields) {
                        SIDToFields[pl.Name] = fields;
                    }
                }
            }
        } catch (Exception ex) {
            Logger.Write("EntityRegistry.Lua", LogLevel.Error, $"Failed to register lua entity {chunkName} [{mod?.Name}]: {ex}");
            return;
        }
    }

    internal static void RegisterLuaPlacements(string sid, bool trigger, List<LonnEntityPlugin.LonnPlacement> placements) {
        var placementsRegistry = trigger ? TriggerPlacements : EntityPlacements;

        lock (placementsRegistry) {
            foreach (var palcement in placements) {
                placementsRegistry.Add(new(palcement.Name) {
                    ValueOverrides = palcement.Data,
                    SID = sid,
                    PlacementHandler = trigger ? EntityPlacementHandler.Trigger : EntityPlacementHandler.Entity,
                    FromLonn = true,
                });
            }
        }
    }

    private static IEnumerable<Type> GetEntityTypesFromAsm(Assembly asm) 
        => asm.GetTypes().Where(t => !t.IsAbstract && (t.IsSubclassOf(typeof(Entity)) || t.IsSubclassOf(typeof(Style))) && t != typeof(UnknownEntity) && t != typeof(Trigger));

    private static List<string> GetSIDsForType(Type type)
        => type.GetCustomAttributes<CustomEntityAttribute>().Select(attr => attr.Name).ToList();

    public static void RegisterFrom(Assembly asm, ModMeta? mod = null) {
        foreach (var t in GetEntityTypesFromAsm(asm)) {
            var sids = GetSIDsForType(t);

            if (sids.Count == 0) {
                //Logger.Write("EntityRegistry", LogLevel.Warning, $"Non-abstract type {t} extends {typeof(Entity)}, but doesn't have the {typeof(CustomEntityAttribute)} attribute!");
                continue;
            }

            lock (SIDToType) {
                foreach (var sid in sids) {
                    SIDToType[sid] = t;
                }
            }

            if (mod is { })
                lock (SIDToMod) {
                    foreach (var sid in sids) {
                        if (!SIDToMod.ContainsKey(sid))
                            SIDToMod[sid] = mod;
                    }
                }

            var getPlacementsMethod = t.GetMethod("GetPlacements", BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes);
            try {
                if (getPlacementsMethod is { }) {
                    foreach (var sid in sids) {
                        var placements = (IEnumerable<Placement>?) getPlacementsMethod.Invoke(null, null);

                        if (placements is { })
                            AddPlacements(t, new() { sid }, placements);
                    }
                }
            } catch (Exception e) {
                Logger.Error(e, $"Failed to get placements for entity {string.Join(',', sids)}");
            }

            var getPlacementsForSIDMethod = t.GetMethod("GetPlacements", BindingFlags.Public | BindingFlags.Static, new Type[] { typeof(string) });
            try {
                if (getPlacementsForSIDMethod is { }) {
                    foreach (var sid in sids) {
                        var placements = (IEnumerable<Placement>?) getPlacementsForSIDMethod.Invoke(null, new object[] { sid });

                        if (placements is { })
                            AddPlacements(t, new() { sid }, placements);
                    }
                }
            } catch (Exception e) {
                Logger.Error(e, $"Failed to get placements for entity {string.Join(',', sids)}");
            }

            var getFieldsMethod = t.GetMethod("GetFields", BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes);
            if (getFieldsMethod is { }) {
                var dele = getFieldsMethod.CreateDelegate<Func<FieldList>>();
                foreach (var sid in sids) {
                    SIDToFields[sid] = dele;
                }
            }

            var getFieldsForSIDMethod = t.GetMethod("GetFields", BindingFlags.Public | BindingFlags.Static, new Type[] { typeof(string) });
            if (getFieldsForSIDMethod is { }) {
                var dele = getFieldsForSIDMethod.CreateDelegate<Func<string, FieldList>>();
                foreach (var sid in sids) {
                    SIDToFields[sid] = () => dele(sid);
                }
            }
        }
    }

    private static void AddPlacements(Type? t, List<string> sids, IEnumerable<Placement> placements) {
        if (t is null)
            return;

        var isTrigger = t.IsSubclassOf(typeof(Trigger));
        var isStyle = t.IsSubclassOf(typeof(Style));

        var placementsRegistry = isStyle ? StylegroundPlacements : isTrigger ? TriggerPlacements : EntityPlacements;

        lock (placementsRegistry) {
            foreach (var placement in placements) {
                placement.SID ??= sids.Count == 1 ? sids[0] : throw new Exception($"Entity {t} has multiple {typeof(CustomEntityAttribute)} attributes, but its placement {placement.Name} doesn't have the SID field set");
                placement.PlacementHandler = isTrigger ? EntityPlacementHandler.Trigger : EntityPlacementHandler.Entity;
            }
            placementsRegistry.AddRange(placements);
        }
    }

    public static Entity Create(Placement from, Vector2 pos, Room room, bool assignID, bool isTrigger) {
        var sid = from.SID ?? throw new NullReferenceException($"Placement.SID is null");
        Dictionary<string, object> data = new(from.ValueOverrides, StringComparer.Ordinal);

        if (SIDToFields.TryGetValue(sid, out var fields)) {
            foreach (var (name, field) in fields()) {
                if (!data.ContainsKey(name))
                    data.Add(name, field.GetDefault());
            }
        }

        var entity = Create(sid, pos, assignID ? null : -1, new(sid, data, from.Nodes), room, isTrigger);

        from.Finalizer?.Invoke(entity);

        var minimumNodes = entity.NodeLimits.Start.Value;
        if (minimumNodes > 0 && entity.Nodes is { Count: 0 }) {
            var nodes = entity.EntityData.Nodes;
            for (int i = 0; i < minimumNodes; i++) {
                nodes.Add(new(0, 0));
            }

            entity.InitializeNodePositions();
        }

        return entity;
    }

    public static Entity Create(BinaryPacker.Element from, Room room, bool trigger) {
        var sid = from.Name ?? throw new Exception($"Entity SID is null in entity element???");

        return Create(sid, new(from.Int("x"), from.Int("y")), from.Int("id"), new(sid, from), room, trigger);
    }

    private static Entity Create(string sid, Vector2 pos, int? id, EntityData entityData, Room room, bool trigger) {
        Entity e;
        if (SIDToType.TryGetValue(sid, out var type)) {
            e = Activator.CreateInstance(type) switch {
                Entity ent => ent,
                var other => throw new InvalidCastException($"Cannot convert {other} to {typeof(Entity)}")
            };
        } else {
            if (Settings.Instance?.LogMissingEntities ?? false)
                Logger.Write("EntityRegistry.Create", LogLevel.Warning, $"Unknown entity: {sid}");
            e = trigger ? new Trigger() : new UnknownEntity();
        }

        e.EntityData = entityData;
        e.ID = id ?? room.NextEntityID();
        e.Room = room;
        e.Pos = pos;
        e.OnChanged();
        entityData.OnChanged += e.OnChanged;

        if (e is LonnEntity lonnEntity) {
            if (!SIDToLonnPlugin.TryGetValue(sid, out var plugin)) {
                plugin = LonnEntityPlugin.Default(LuaCtx, sid);
                SIDToLonnPlugin[sid] = plugin;
            }

            lonnEntity.Plugin = plugin;
        }
        if (e is LonnTrigger lonnTrigger) {
            if (!SIDToLonnPlugin.TryGetValue(sid, out var plugin)) {
                plugin = LonnEntityPlugin.Default(LuaCtx, sid);
                SIDToLonnPlugin[sid] = plugin;
            }

            lonnTrigger.Plugin = plugin;
        }

        var min = e.MinimumSize;

        if (e.ResizableX && e.Width < min.X) {
            e.Width = min.X;
        }

        if (e.ResizableY && e.Height < min.Y) {
            e.Height = min.Y;
        }

        if (e is Decal d) {
            d.FG = sid == FGDecalSID;

            d.OnCreated();
        }

        return e;
    }
}
