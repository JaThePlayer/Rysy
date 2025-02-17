using Rysy.Graphics;
using Rysy.Graphics.TextureTypes;
using Rysy.Gui.Windows;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Mods;
using Rysy.Selections;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Rysy;

public record class Placement : IUntypedData {
    /// <summary>
    /// Creates a placement with value overrides using an anonymous object of the style of { fieldName = value, field2 = value2, ...}
    /// </summary>
    /// <param name="name">The name of this placement</param>
    /// <param name="overridesLonnDecl">The object to use to create overrides</param>
    public Placement(string name, object overridesLonnDecl) : this(name) {
        var props = overridesLonnDecl.GetType().GetProperties();

        foreach (var prop in props) {
            var value = prop.GetValue(overridesLonnDecl);

            if (value is { })
                this[prop.Name] = value;
        }
    }

    public Placement(string name) {
        Name = name;
    }

    public Placement() {
        Name = "";
    }

    public string Name { get; set; }

    public string? SID { get; set; }

    public string? Tooltip { get; set; }

    [JsonIgnore]
    public Action<Entity>? Finalizer;

    // set in entity registry
    private IPlacementHandler? _PlacementHandler;

    public RegisteredEntityType RegisteredEntityType { get; set; }
    
    [JsonIgnore]
    public IPlacementHandler PlacementHandler {
        get => _PlacementHandler ??= GuessHandler()!;
        internal set => _PlacementHandler = value;
    }

    private IPlacementHandler? GuessHandler() {
        if (SID == null)
            return null;

        var trivial = SID switch {
            EntityRegistry.BGDecalSID => EntityPlacementHandler.BGDecals,
            EntityRegistry.FGDecalSID => EntityPlacementHandler.FGDecals,
            _ => null,
        };

        if (trivial is { }) {
            return trivial;
        };

        var t = EntityRegistry.GetTypeForSID(SID, RegisteredEntityType);

        if (t is not { }) {
            return null;
        }

        if (t.IsAssignableTo(typeof(Trigger))) {
            return EntityPlacementHandler.Trigger;
        }

        if (t.IsAssignableTo(typeof(Entity))) {
            return EntityPlacementHandler.Entity;
        }

        return null;
    }

    public Dictionary<string, object> ValueOverrides { get; set; } = new();

    public Vector2[]? Nodes { get; set; }

    internal bool FromLonn;
    
    private List<string> _AssociatedMods;

    public Placement WithSID(string sid) {
        SID = sid;

        return this;
    }

    public Placement WithTooltip(string tooltip) {
        Tooltip = tooltip;

        return this;
    }

    public Placement WithFinalizer(Action<Entity> act) {
        Finalizer += act;

        return this;
    }

    public Placement SwapWidthAndHeight() {
        (this["width"], this["height"]) = (this["height"], this["width"]);

        return this;
    }

    public Placement WithAssociatedMods(IEnumerable<string> mods) {
        _AssociatedMods = mods.ToList();

        return this;
    }

    /// <summary>
    /// Sets/gets a value from <see cref="ValueOverrides"/>. Enums get converted to strings via ToString.
    /// Setting a field to null removes that field instead.
    /// Trying to get a field that doesn't exist returns null.
    /// </summary>
    public object? this[string key] {
        get => ValueOverrides.TryGetValue(key, out var v) ? v : null;
        set {
            switch (value) {
                case Enum e:
                    ValueOverrides[key] = e.ToString();
                    break;
                case null:
                    ValueOverrides.Remove(key);
                    break;
                default:
                    ValueOverrides[key] = value;
                    break;
            }
        }
    }

    /// <summary>
    /// Adds additional value overrides from <paramref name="newOverrides"/> on top of the existing ones.
    /// Mutates and returns this instance.
    /// </summary>
    /// <returns>this</returns>
    public Placement WithOverrides(Dictionary<string, object> newOverrides) {
        ValueOverrides = new(ValueOverrides.Concat(newOverrides));

        return this;
    }

    public Placement CreateClone() {
        var pl = new Placement(this) {
            ValueOverrides = new(ValueOverrides),
            Nodes = Nodes?.ShallowClone()
        };

        return pl;
    }

    /// <summary>
    /// Tries to get the mod this placement comes from
    /// </summary>
    public ModMeta? GetDefiningMod() {
        if (PlacementHandler?.GetDefiningMod() is { } mod)
            return mod;
        
        if (IsDecal()) {
            var texture = GFX.Atlas[Decal.GetTexturePathFromPlacement(this)];
            if (texture is ModTexture modTexture)
                return modTexture.Mod;
            return null;
        }

        if (SID is { } sid) {
            return EntityRegistry.GetDefiningMod(sid, RegisteredEntityType);
        }

        return null;
    }

    /// <summary>
    /// Gets all mods that are associated with this placement. This list might not contain the mod returned by <see cref="GetDefiningMod"/>
    /// </summary>
    public ReadOnlyList<string> GetAssociatedMods() {
        if (_AssociatedMods is { } cached)
            return cached;

        if (!IsEntityOrTrigger()) {
            if (GetDefiningMod()?.Name is { } name)
                return _AssociatedMods = [name];
            return _AssociatedMods = [];
        }
        try {
            var handler = PlacementHandler.CreateSelection(this, default, Room.DummyRoom);
            if (handler is EntitySelectionHandler entityHandler) {
                var entity = entityHandler.Entity;

                return _AssociatedMods = EntityRegistry.GetAssociatedMods(entity);
            }
        } catch
        {

        }
        

        return _AssociatedMods = new List<string>(0);
    }

    public bool AreAssociatedModsADependencyOfCurrentMap() {
        if (EditorState.Map?.Mod is not { } currentMod) {
            return true;
        }

        foreach (var associated in GetAssociatedMods()) {
            if (!currentMod.DependencyMet(associated)) {
                return false;
            }
        }
        
        return true;
    }

    /// <summary>
    /// Checks whether this placement will place a trigger or entity.
    /// </summary>
    public bool IsEntityOrTrigger() => PlacementHandler is EntityPlacementHandler { Layer: SelectionLayer.Triggers or SelectionLayer.Entities };

    /// <summary>
    /// Checks whether this placement will place a trigger.
    /// </summary>
    public bool IsTrigger() => PlacementHandler is EntityPlacementHandler { Layer: SelectionLayer.Triggers };

    /// <summary>
    /// Checks whether this placement will place a decal.
    /// </summary>
    public bool IsDecal() => PlacementHandler is EntityPlacementHandler { Layer: SelectionLayer.BGDecals or SelectionLayer.FGDecals };

    //public IHistoryAction Place(Vector2 pos, Room room) => PlacementHandler.Place(this, pos, room);

    public IEnumerable<ISprite> GetPreviewSprites(ISelectionHandler selection, Vector2 pos, Room room) 
        => PlacementHandler.GetPreviewSprites(selection, pos, room);
    
    public IEnumerable<ISprite> GetWidgetSprites(ISelectionHandler selection, Vector2 pos, Room room) 
        => PlacementHandler.GetWidgetSprites(selection, pos, room);

    public static Placement? TryCreateFromObject(object obj) => obj switch {
        IConvertibleToPlacement convertible => convertible.ToPlacement(),
        _ => null,
    };

    bool IUntypedData.TryGetValue(string key, [NotNullWhen(true)] out object? value) {
        return ValueOverrides.TryGetValue(key, out value);
    }
}

public class PlacementList : List<Placement> {
    public PlacementList() { }

    public PlacementList(string defaultPlacementName) {
        Add(new(defaultPlacementName));
    }

    public PlacementList(IEnumerable<Placement> placements) : base(placements) {

    }

    public static PlacementList FromEnum<T>(Func<string, Placement> generator) where T : struct, Enum {
        var values = Enum.GetNames<T>();

        return new(values.Select(x => generator(x)));
    }
}

public interface IPlacementHandler {
    public bool ShowVanillaAsDefiningModInPlacementName() => true;
    
    public ModMeta? GetDefiningMod() => null;
    
    public Vector2? GetPreviewSpritesOffset(ISelectionHandler handler, Vector2 pos, Room room) => null;
    
    public IEnumerable<ISprite> GetPreviewSprites(ISelectionHandler handler, Vector2 pos, Room room);

    public virtual IEnumerable<ISprite> GetWidgetSprites(ISelectionHandler handler, Vector2 pos, Room room)
        => GetPreviewSprites(handler, pos, room);
    
    //public ISelectionHandler GetHandler(Placement placement);

    public IHistoryAction Place(ISelectionHandler handler, Room room);
    public ISelectionHandler CreateSelection(Placement placement, Vector2 pos, Room room);
}

public record class EntityPlacementHandler(SelectionLayer Layer) : IPlacementHandler {
    public static readonly EntityPlacementHandler Entity = new(SelectionLayer.Entities);
    public static readonly EntityPlacementHandler Trigger = new(SelectionLayer.Triggers);
    public static readonly EntityPlacementHandler FGDecals = new(SelectionLayer.FGDecals);
    public static readonly EntityPlacementHandler BGDecals = new(SelectionLayer.BGDecals);

    private Entity CreateFromPlacement(Placement placement, Vector2 pos, Room room) {
        Entity? entity = null;
        switch (Layer) {
            case SelectionLayer.Entities:
                entity = EntityRegistry.Create(placement, pos, room, false, false);
                break;
            case SelectionLayer.FGDecals:
                entity = EntityRegistry.Create(placement, pos, room, false, false);
                entity.AsDecal()!.FG = true;
                break;
            case SelectionLayer.BGDecals:
                entity = EntityRegistry.Create(placement, pos, room, false, false);
                break;
            case SelectionLayer.Triggers:
                entity = EntityRegistry.Create(placement, pos, room, false, true);
                break;
        }

        if (entity is { }) {
            ResetEntitySize(entity);

            return entity;
        }

        throw new Exception($"Can't create entity from layer: {Layer}");
    }

    private static void ResetEntitySize(Entity? entity) {
        if (entity is { }) {
            var min = entity.RecommendedMinimumSize;
            entity.Width = min.X;
            entity.Height = min.Y;
        }
    }

    public IEnumerable<ISprite> GetPreviewSprites(ISelectionHandler handler, Vector2 pos, Room room) {
        if (handler is EntitySelectionHandler entityHandler) {
            var entity = entityHandler.Entity;

            if (entity.Pos != pos) {
                entity.Pos = pos;
                entity.InitializeNodePositions();
            }

            if (entity.Room != room) {
                entity.Room = room;
                entity.ClearInnerCaches();
            }

            // todo: hacky!!!
            entity.Selected = true;
            var sprites = entity.GetSpritesWithNodes().OrderByDescending(x => x.Depth).ToList();
            entity.Selected = false;

            return sprites;
        }
            

        return Array.Empty<ISprite>();
    }

    public IEnumerable<ISprite> GetWidgetSprites(ISelectionHandler handler, Vector2 pos, Room room) {
        if (handler is EntitySelectionHandler entityHandler) {
            var entity = entityHandler.Entity;

            if (entity.Pos != pos) {
                entity.Pos = pos;
                entity.InitializeNodePositions();
            }

            // todo: hacky!!!
            entity.Selected = true;
            var sprites = (entity is Trigger trigger
                ? trigger.GetPreviewSprites()
                : entity.GetSpritesWithNodes()).OrderByDescending(x => x.Depth).ToList();
            entity.Selected = false;

            return sprites;
        }
            

        return Array.Empty<ISprite>();
    }
    
    public IHistoryAction Place(ISelectionHandler handler, Room room) {
        var act = handler.PlaceClone(room);
        handler.TryResize(new(int.MinValue, int.MinValue))?.Apply(room.Map);

        return act;
    }

    public ISelectionHandler CreateSelection(Placement placement, Vector2 pos, Room room) {
        var entity = CreateFromPlacement(placement, pos, room);

        return entity.CreateSelection().Handler;
    }
}

public record class RoomPlacementHandler : IPlacementHandler {
    public bool ShowVanillaAsDefiningModInPlacementName() => false;
    
    public Vector2? GetPreviewSpritesOffset(ISelectionHandler handler, Vector2 pos, Room? alwaysNull) {
        var room = ((RoomSelectionHandler) handler).Room;
        room.Pos = pos;

        return pos;
    }
    
    public IEnumerable<ISprite> GetPreviewSprites(ISelectionHandler handler, Vector2 pos, Room? alwaysNull) {
        var room = ((RoomSelectionHandler) handler).Room;
        room.Pos = pos;
        room.ClearEntityRenderCache();
        
        return room.GetInteriorSprites()
            .Concat(room.GetBorderSprite(EditorState.Camera.Scale) with {
                Depth = int.MinValue,
                Pos = new(0, 0, room.Width, room.Height)
            });
    }

    public IHistoryAction Place(ISelectionHandler handler, Room? alwaysNull) {
        var roomHandler = (RoomSelectionHandler) handler;
        
        var act = roomHandler.PlaceClone(newRoom => {
            if (EditorState.Map is { } map) {
                newRoom.Name = map.GuessNewRoomNameFromParent(EditorState.CurrentRoom) ?? map.DeduplicateRoomName("new_room");
                EditorState.CurrentRoom = newRoom;
                RysyState.Scene.AddWindow(new RoomEditWindow(newRoom, false));
            }
        });
        handler.TryResize(new(int.MinValue, int.MinValue))?.Apply(EditorState.Map!);
        if (roomHandler.Room.Entities["player"].Count > 0) {
            handler.TryResize(new(40 * 8 - 8, 23 * 8 - 8))?.Apply(EditorState.Map!);
        }
        
        return act;
    }

    public ISelectionHandler CreateSelection(Placement placement, Vector2 pos, Room? alwaysNull) {
        var newRoom = new Room(EditorState.Map!, placement.Int("width", 40 * 8), placement.Int("height", 23 * 8));
        newRoom.Pos = pos;

        if (placement.Bool("hasPlayer")) {
            newRoom.Entities.Add(EntityRegistry.CreateFromMainPlacement("player", new Vector2(2, 2) * 8f, newRoom, new()));
        }

        return new RoomSelectionHandler(newRoom);
    }
}

public interface IConvertibleToPlacement {
    public Placement ToPlacement();
}

public interface IPlaceable {
    public static abstract FieldList GetFields();
    public static abstract PlacementList GetPlacements();
}

public interface IMultiSIDPlaceable {
    public static abstract FieldList GetFields(string sid);
    public static abstract PlacementList GetPlacements(string sid);
}