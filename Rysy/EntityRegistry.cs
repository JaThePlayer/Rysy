using Rysy.Entities;
using Rysy.Extensions;
using Rysy.LuaSupport;
using Rysy.Scenes;
using System.Reflection;

namespace Rysy;

public static class EntityRegistry {
    private static Dictionary<string, Type> SIDToType { get; set; } = new();
    private static Dictionary<string, LonnEntityPlugin> SIDToLonnPlugin { get; set; } = new();
    public static Dictionary<string, FieldList> SIDToFields { get; set; } = new();

    public static List<Placement> EntityPlacements { get; set; } = new();
    public static List<Placement> TriggerPlacements { get; set; } = new();

    private static LuaCtx? _LuaCtx = null;
    private static LuaCtx LuaCtx => _LuaCtx ??= LuaCtx.CreateNew();

    public const string FGDecalSID = "fgDecal";
    public const string BGDecalSID = "bgDecal";

    public static async ValueTask RegisterAsync() {
        SIDToType.Clear();
        SIDToLonnPlugin.Clear();
        SIDToFields.Clear();
        EntityPlacements.Clear();
        TriggerPlacements.Clear();

        RegisterHardcoded();

        var loadingScene = RysyEngine.Scene as LoadingScene;
        loadingScene?.SetText("Registering entities");


        if (Settings.Instance.LonnPluginPath is { } path) {
            using var w = new ScopedStopwatch("Registering Lua entities");

            foreach (var item in Directory.EnumerateFiles(Path.Combine(path, "entities"), "*.lua")) {
                RegisterFromLua(File.ReadAllText(item), Path.GetFileName(item), trigger: false);
            }

            foreach (var item in Directory.EnumerateFiles(Path.Combine(path, "triggers"), "*.lua")) {
                RegisterFromLua(File.ReadAllText(item), Path.GetFileName(item), trigger: true);
            }
        }


        using (var watch = new ScopedStopwatch("Registering entities")) {
            //await Task.WhenAll(AppDomain.CurrentDomain.GetAssemblies().SelectToTaskRun(RegisterFrom));
        }
    }

    private static void RegisterHardcoded() {
        SIDToType[FGDecalSID] = typeof(Decal);
        SIDToType[BGDecalSID] = typeof(Decal);

        var decalFields = Decal.GetFields();

        SIDToFields[FGDecalSID] = decalFields;
        SIDToFields[BGDecalSID] = decalFields;
    }

    public static void RegisterFromLua(string lua, string chunkName, bool trigger) {
        try {
            LuaCtx.Lua.PCallStringThrowIfError(lua, chunkName, results: 1);
            List<LonnEntityPlugin>? plugins = null;

            try {
                plugins = LonnEntityPlugin.FromCtx(LuaCtx);
            } finally {
                LuaCtx.Lua.Pop(1);
            }

            foreach (var pl in plugins) {
                lock (SIDToType) {
                    SIDToType[pl.Name] = trigger ? typeof(LonnTrigger) : typeof(LonnEntity);
                }
                lock (SIDToLonnPlugin) {
                    SIDToLonnPlugin[pl.Name] = pl;
                }

                var placements = trigger ? TriggerPlacements : EntityPlacements;

                lock (placements) {
                    foreach (var item in pl.Placements) {
                        placements.Add(new($"{pl.Name} [{item.Name}]") {
                            ValueOverrides = item.Data,
                            SID = pl.Name,
                            Tooltip = "[From Lonn]",
                            PlacementHandler = trigger ? EntityPlacementHandler.Trigger : EntityPlacementHandler.Entity
                        });
                    }
                }
            }
        } catch (Exception ex) {
            Logger.Write("EntityRegistry.Lua", LogLevel.Error, $"Failed to register lua entity {chunkName}: {ex}");
            return;
        }
    }

    public static void RegisterFrom(Assembly asm) {
        foreach (var t in asm.GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Entity)) && t != typeof(UnknownEntity) && t != typeof(Trigger))) {
            var sids = t.GetCustomAttributes<CustomEntityAttribute>().Select(attr => attr.Name).ToArray();

            if (sids.Length == 0) {
                Logger.Write("EntityRegistry", LogLevel.Warning, $"Non-abstract type {t} extends {typeof(Entity)}, but doesn't have the {typeof(CustomEntityAttribute)} attribute!");
                continue;
            }

            lock (SIDToType) {
                foreach (var sid in sids) {
                    SIDToType[sid] = t;
                }
            }

            var getPlacementsMethod = t.GetMethod("GetPlacements", BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes);
            if (getPlacementsMethod is { } && (IEnumerable<Placement>?) getPlacementsMethod.Invoke(null, null) is { } placements) {
                var isTrigger = t.IsSubclassOf(typeof(Trigger));
                var placementsRegistry = isTrigger ? TriggerPlacements : EntityPlacements;

                lock (placementsRegistry) {
                    foreach (var placement in placements) {
                        placement.SID ??= sids.Length == 1 ? sids[0] : throw new Exception($"Entity {t} has multiple {typeof(CustomEntityAttribute)} attributes, but its placement {placement.Name} doesn't have the SID field set");
                        placement.PlacementHandler = isTrigger ? EntityPlacementHandler.Trigger : EntityPlacementHandler.Entity;
                    }
                    placementsRegistry.AddRange(placements);
                }
            }

            var getFieldsMethod = t.GetMethod("GetFields", BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes);
            if (getFieldsMethod is { } && (FieldList?) getFieldsMethod.Invoke(null, null) is { } fields) {
                foreach (var sid in sids) {
                    SIDToFields[sid] = fields;
                }
            }
        }
    }

    
    public static Entity Create(Placement from, Vector2 pos, Room room, bool assignID, bool isTrigger) {
        var sid = from.SID ?? throw new NullReferenceException($"Placement.SID is null");
        Dictionary<string, object> data;

        if (SIDToFields.TryGetValue(sid, out var fields)) {
            data = 
                fields.Select(f => (f.Key, f.Value.GetDefault()))
                .Concat(from.ValueOverrides.Select(o => (o.Key, o.Value)))
                .DistinctBy(p => p.Key)
                .ToDictionary(f => f.Item1, f => f.Item2);
        } else {
            data = from.ValueOverrides;
        }

        var entity = Create(sid, pos, assignID ? null : -1, new(sid, data, from.Nodes), room, isTrigger);

        from.Finalizer?.Invoke(entity);

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

        if (e is LonnEntity lonnPlugin) {
            lonnPlugin.Plugin = SIDToLonnPlugin[sid];
        }
        if (e is LonnTrigger lonnTrigger) {
            lonnTrigger.Plugin = SIDToLonnPlugin[sid];
        }

        var min = e.MinimumSize;

        if (e.ResizableX && e.Width < min.X) {
            e.Width = min.X;
        }

        if (e.ResizableY && e.Height < min.Y) {
            e.Height = min.Y;
        }

        var minimumNodes = e.NodeLimits.Start.Value;
        if (minimumNodes > 0 && e.Nodes is not { }) {
            var nodes = e.EntityData.Nodes = new List<Node>(minimumNodes);
            for (int i = 0; i < minimumNodes; i++) {
                nodes.Add(new(0, 0));
            }

            e.InitializeNodePositions();
        }

        if (e is Decal d) {
            d.FG = sid == FGDecalSID;
        }

        return e;
    }
}
