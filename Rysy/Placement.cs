using Rysy.Graphics;
using Rysy.History;

namespace Rysy;

public record class Placement(string Name) {
    public string? SID;

    public string? Tooltip;

    public Action<Entity>? Finalizer;

    // set in entity registry
    public IPlacementHandler PlacementHandler { get; internal set; }

    public Dictionary<string, object> ValueOverrides { get; set; } = new();

    public Vector2[]? Nodes { get; set; }

    public Placement ForSID(string sid) {
        SID = sid;

        return this;
    }

    public Placement WithTooltip(string tooltip) {
        Tooltip = tooltip;

        return this;
    }

    public object this[string key] {
        get => ValueOverrides[key];
        set => ValueOverrides[key] = value;
    }

    //public IHistoryAction Place(Vector2 pos, Room room) => PlacementHandler.Place(this, pos, room);

    public IEnumerable<ISprite> GetPreviewSprites(ISelectionHandler selection, Vector2 pos, Room room) => PlacementHandler.GetPreviewSprites(selection, pos, room);

    public static Placement? TryCreateFromObject(object obj) => obj switch {
        IConvertibleToPlacement convertible => convertible.ToPlacement(),
        _ => null,
    };
}

public interface IPlacementHandler {
    public IEnumerable<ISprite> GetPreviewSprites(ISelectionHandler handler, Vector2 pos, Room room);


    //public ISelectionHandler GetHandler(Placement placement);

    public IHistoryAction Place(ISelectionHandler handler, Room room);
    public ISelectionHandler CreateSelection(Placement placement, Vector2 pos, Room room);
}

public record class EntityPlacementHandler(SelectionLayer Layer) : IPlacementHandler {
    public static EntityPlacementHandler Entity = new(SelectionLayer.Entities);
    public static EntityPlacementHandler Trigger = new(SelectionLayer.Triggers);
    public static EntityPlacementHandler FGDecals = new(SelectionLayer.FGDecals);
    public static EntityPlacementHandler BGDecals = new(SelectionLayer.BGDecals);

    private Entity CreateFromPlacement(Placement placement, Vector2 pos, Room room) {
        placement.ValueOverrides["_editorLayer"] = Persistence.Instance?.EditorLayer ?? 0;

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
            var min = entity.MinimumSize;
            entity.Width = min.X;
            entity.Height = min.Y;
        }
    }

    public IEnumerable<ISprite> GetPreviewSprites(ISelectionHandler handler, Vector2 pos, Room room) {
        if (handler is EntitySelectionHandler entityHandler) {
            entityHandler.Entity.Pos = pos;
            entityHandler.Entity.InitializeNodePositions();
            return entityHandler.Entity.GetSpritesWithNodes();
        }
            

        return Array.Empty<ISprite>();
    }

    public IHistoryAction Place(ISelectionHandler handler, Room room) {
        var act = handler.PlaceClone(room);
        handler.TryResize(new(int.MinValue))?.Apply();

        return act;
    }

    public ISelectionHandler CreateSelection(Placement placement, Vector2 pos, Room room) {
        var entity = CreateFromPlacement(placement, pos, room);

        return new EntitySelectionHandler(entity);
    }
}

public interface IConvertibleToPlacement {
    public Placement ToPlacement();
}

public interface IPlaceable {
    public static abstract List<Placement>? GetPlacements();

    public static abstract FieldList GetFields();
}