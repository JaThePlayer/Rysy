using Rysy.Entities;
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

    private static LuaCtx LuaCtx = LuaCtx.CreateNew();

    public const string FGDecalSID = "fgDecal";
    public const string BGDecalSID = "bgDecal";

    public static async ValueTask RegisterAsync() {
        SIDToType.Clear();
        SIDToLonnPlugin.Clear();
        SIDToFields.Clear();
        EntityPlacements.Clear();
        TriggerPlacements.Clear();

        SIDToType[FGDecalSID] = typeof(Decal);
        SIDToType[BGDecalSID] = typeof(Decal);

        var loadingScene = RysyEngine.Scene as LoadingScene;
        loadingScene?.SetText("Registering entities");


        if (Settings.Instance.LonnPluginPath is { } path)
            foreach (var item in Directory.EnumerateFiles(path, "*.lua")) {
                RegisterFromLua(File.ReadAllText(item), Path.GetFileName(item));
            }

        using (var watch = new ScopedStopwatch("Registering entities")) {
            await Task.WhenAll(AppDomain.CurrentDomain.GetAssemblies().SelectToTaskRun(RegisterFrom));
        }
    }

    public static void RegisterFromLua(string lua, string chunkName) {
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
                    SIDToType[pl.Name] = typeof(LonnEntity);
                }
                lock (SIDToLonnPlugin) {
                    SIDToLonnPlugin[pl.Name] = pl;
                }

                lock (EntityPlacements) {
                    foreach (var item in pl.Placements) {
                        EntityPlacements.Add(new($"{pl.Name} [{item.Name}]") {
                            ValueOverrides = item.Data,
                            SID = pl.Name,
                            Tooltip = "[From Lonn]",
                            PlacementHandler = EntityPlacementHandler.Entity
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
        return Create(sid, pos, assignID ? null : -1, new(sid, from.ValueOverrides, nodes: null), room, isTrigger);
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
            if (Settings.Instance.LogMissingEntities)
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

        var min = e.MinimumSize;

        if (e.ResizableX && e.Width < min.X) {
            e.Width = min.X;
        }

        if (e.ResizableY && e.Height < min.Y) {
            e.Height = min.Y;
        }

        return e;
    }
}
