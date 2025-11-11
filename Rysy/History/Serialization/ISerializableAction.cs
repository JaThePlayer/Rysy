using Rysy.Extensions;
using Rysy.Helpers;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rysy.History;

/*
public interface ISerializableAction {
    public Type DataType { get; }
}*/

#pragma warning disable CA2225

/// <summary>
/// Stores a reference to an existing entity
/// </summary>
public sealed record EntityRef(int Id, string RoomName) {
    [JsonIgnore]
    private Entity? _entity;
    
    public EntityRef(Entity entity) : this(entity.Id, entity.RoomName) {
        _entity = entity;
    }

    public Entity? TryResolve(Map map) {
        if (_entity is { } cached)
            return cached;
        
        if (map.TryGetRoomByName(RoomName) is not { } room)
            return null;

        return _entity = room.TryGetEntityById(Id);
    }

    public Entity Resolve(Map map) {
        if (TryResolve(map) is { } e)
            return e;

        throw new Exception($"Couldn't resolve entity: {RoomName}[{Id}]");
    }

    public static implicit operator EntityRef(Entity e) => new(e);
}

/// <summary>
/// Stores a reference to an existing room
/// </summary>
public sealed record RoomRef(string Name) {
    [JsonIgnore]
    private Room? _room;
    
    public RoomRef(Room room) : this(room.Name) {
        _room = room;
    }
    
    public Room? TryResolve(Map map) {
        return _room ??= map.TryGetRoomByName(Name);
    }
    
    public Room Resolve(Map map) {
        return TryResolve(map) ?? throw new Exception($"Couldn't resolve room: {Name}");
    }
    
    public static implicit operator RoomRef(Room r) => new(r);
}

public static class HistorySerializer {
    public static BinaryPacker.Element SerializeToElement<T>(T obj) where T : class {
        return SerializeAnyToElement(obj);
    }
    
    public static BinaryPacker.Element SerializeAnyToElement(object obj, Type? asType = null) {
        if (obj is IPackable packable) {
            var packed = packable.Pack();
            if (obj is Entity e)
                packed.Attributes["_registeredEntity"] = e.RegistryType.ToString();
            return packed;
        }

        asType ??= obj.GetType();

        var el = new BinaryPacker.Element(asType.FullName);
        el.Attributes = [];

        if (obj is IEnumerable enumerable) {
            var children = new List<BinaryPacker.Element>();
            foreach (var inner in enumerable) {
                children.Add(SerializeAnyToElement(inner));
            }
            el.Children = children.ToArray();
        } else {
            var props = asType.GetProperties();
            foreach (var prop in props) {
                if (prop.GetMethod is { } getter && getter.GetParameters() is [ ] && getter.Invoke(obj, null) is { } val) {
                    el.Attributes[prop.Name] = val switch {
                        IPackable packableVal => packableVal.Pack(),
                        Entity entity => entity.Pack(),
                        int or float or string or bool or short or byte => val,
                        Vector2 vec => new BinaryPacker.Element("Vector2") {
                            Attributes = new() {
                                ["X"] = vec.X,
                                ["Y"] = vec.Y
                            }
                        },
                        _ => SerializeAnyToElement(val)
                    };
                }
            }
        }

        return el;
    }

    public static bool TryDeserialize<T>(BinaryPacker.Element el, Room room, [NotNullWhen(true)] out T? ret) {
        ret = default;

        Type? type = null;

        if (el.Name == "Rysy.History.MergedAction") {
            var actions = new List<IHistoryAction>(el.Children.Length);
            foreach (var ch in el.Children) {
                if (TryDeserialize<IHistoryAction>(ch, room, out var innerAction))
                    actions.Add(innerAction);
            }

            ret = (T)(object)actions.MergeActions()!;
            return true;
        }

        if (el.TryGetValue("_registeredEntity", out var entityType)) {
            type = EntityRegistry.GetTypeForSid(el.Name ?? "", entityType switch {
                "Entity" => RegisteredEntityType.Entity,
                "Trigger" => RegisteredEntityType.Trigger,
                "Style" => RegisteredEntityType.Style,
                _ => RegisteredEntityType.Entity
            });
            if (type is not null) {
                ret = (T)(object)EntityRegistry.Create(el, room, false)!;
                return true;
            }
        }

        
        type ??= GetTypeFromStr(el.Name ?? "");
        if (type is null)
            return false;

        if (RuntimeHelpers.GetUninitializedObject(type) is not T into)
            return false;
        

        if (el.Attributes is { } attrs) {
            var props = type.GetProperties();
            foreach (var prop in props) {
                if (prop.SetMethod is { } setMethod && attrs.TryGetValue(prop.Name, out var val)) {
                    if (val.GetType() == prop.PropertyType)
                        setMethod.Invoke(into, [ val ]);
                    else if (val is BinaryPacker.Element innerEl) {
                        if (TryDeserialize<object>(innerEl, room, out var innerObj)) {
                            setMethod.Invoke(into, [ innerObj ]);
                        }
                    }
                    else if (val is JsonElement jsonElement) {
                        var innerEl2 = jsonElement.Deserialize<BinaryPacker.Element>(JsonSerializerHelper.DefaultOptions)!;
                        if (TryDeserialize<object>(innerEl2, room, out var innerObj)) {
                            setMethod.Invoke(into, [ innerObj ]);
                        } else {
                            Console.WriteLine($"Failed to deserialize: {innerEl2.ToJson()} [{innerEl2.Name}] (json: {jsonElement})");
                        }
                    }
                    else {
                        Console.WriteLine($"Cant set prop {prop.Name}, got {val} [{val.GetType()}]");
                    }
                }
            }
        }

        ret = into;
        return true;

        Type? GetTypeFromStr(string name) {
            return Type.GetType(name);
        }
    }
}

#pragma warning restore CA2225