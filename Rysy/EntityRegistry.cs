using Rysy.Entities;
using Rysy.Scenes;
using Rysy.Triggers;
using System.Reflection;

namespace Rysy;

public static class EntityRegistry {
    public static Dictionary<string, Type> SIDToType = new();
    public static Dictionary<string, FieldList> SIDToFields = new();

    public static List<Placement> EntityPlacements = new();
    public static List<Placement> TriggerPlacements = new();

    public static async ValueTask RegisterAsync() {
        SIDToType.Clear();

        var loadingScene = RysyEngine.Scene as LoadingScene;
        loadingScene?.SetText("Registering entities");
        using (var watch = new ScopedStopwatch("Registering entities")) {
            await Task.WhenAll(AppDomain.CurrentDomain.GetAssemblies().SelectToTaskRun(RegisterFrom));
        }
    }

    public static void RegisterFrom(Assembly asm) {
        foreach (var t in asm.GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(Entity)) && t != typeof(UnknownEntity) && t != typeof(Trigger))) {
            var sids = t.GetCustomAttributes<CustomEntityAttribute>().Select(attr => attr.Name).ToArray();

            if (sids.Length == 0) {
                throw new Exception($"Non-abstract type {t} extends {typeof(Entity)}, but doesn't have the {typeof(CustomEntityAttribute)} attribute!");
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
                        placement.IsTrigger = isTrigger;
                        placement.SID ??= sids.Length == 1 ? sids[0] : throw new Exception($"Entity {t} has multiple {typeof(CustomEntityAttribute)} attributes, but its placement {placement.Name} doesn't have the SID field set");
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

    public static Entity Create(Placement from, Vector2 pos, Room room, bool assignID) {
        var sid = from.SID ?? throw new NullReferenceException($"Placement.SID is null");
        return Create(sid, pos, assignID ? null : -1, new(sid, from.ValueOverrides, nodes: null), room, from.IsTrigger);
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

        e.ID = id ?? room.NextEntityID();
        e.EntityData = entityData;
        e.Room = room;
        e.Pos = pos;

        return e;
    }
}
