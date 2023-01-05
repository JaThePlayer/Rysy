using Rysy.Entities;
using Rysy.Scenes;
using Rysy.Triggers;
using System.Reflection;

namespace Rysy;

public static class EntityRegistry {
    public static Dictionary<string, Type> SIDToType = new();

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
        }
    }

    public static Entity Create(BinaryPacker.Element from, Room room, bool trigger) {
        var sid = from.Name ?? throw new Exception($"Entity SID is null in entity element???");

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

        e.ID = from.Int("id");
        e.EntityData = new(sid, from);
        e.Room = room;
        e.Pos = new(from.Int("x"), from.Int("y"));

        return e;
    }
}
