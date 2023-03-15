using Rysy.Graphics;
using Rysy.History;

namespace Rysy;

public record class Placement(string Name) {
    public string? SID;

    public string? Tooltip;

    // set in entity registry
    public IPlacementHandler PlacementHandler { get; set; }

    public Dictionary<string, object> ValueOverrides { get; set; } = new();

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

    public IHistoryAction Place(Vector2 pos, Room room) => PlacementHandler.Place(this, pos, room);

    public IEnumerable<ISprite> GetPreviewSprites(Vector2 pos, Room room) => PlacementHandler.GetPreviewSprites(this, pos, room);

    public static Placement? TryCreateFromObject(object obj) => obj switch {
        IConvertibleToPlacement convertible => convertible.ToPlacement(),
        _ => null,
    };
}

public interface IPlacementHandler {
    public IEnumerable<ISprite> GetPreviewSprites(Placement placement, Vector2 pos, Room room);

    public IHistoryAction Place(Placement placement, Vector2 pos, Room room);
}

public record class EntityPlacementHandler(SelectionLayer Layer) : IPlacementHandler {
    public static EntityPlacementHandler Entity = new(SelectionLayer.Entities);
    public static EntityPlacementHandler Trigger = new(SelectionLayer.Triggers);
    public static EntityPlacementHandler FGDecals = new(SelectionLayer.FGDecals);
    public static EntityPlacementHandler BGDecals = new(SelectionLayer.BGDecals);

    private Entity CreateFromPlacement(Placement placement, Vector2 pos, Room room) {
        placement.ValueOverrides["_editorLayer"] = Persistence.Instance?.EditorLayer ?? 0;

        switch (Layer) {
            case SelectionLayer.Entities:
                return EntityRegistry.Create(placement, pos, room, false, false);
            case SelectionLayer.FGDecals:
                var entity = EntityRegistry.Create(placement, pos, room, false, false);
                entity.AsDecal()!.FG = true;
                return entity;
            case SelectionLayer.BGDecals:
                return EntityRegistry.Create(placement, pos, room, false, false);
            case SelectionLayer.Triggers:
                return EntityRegistry.Create(placement, pos, room, false, true);
        }
        throw new Exception($"Can't create entity from layer: {Layer}");
    }

    public IEnumerable<ISprite> GetPreviewSprites(Placement placement, Vector2 pos, Room room) {
        return CreateFromPlacement(placement, pos, room).GetSprites();
    }

    public IHistoryAction Place(Placement placement, Vector2 pos, Room room) {
        var entity = CreateFromPlacement(placement, pos, room);

        return new AddEntityAction(entity, room);
    }
}

public interface IConvertibleToPlacement {
    public Placement ToPlacement();
}

public interface IPlaceable {
    public static abstract List<Placement>? GetPlacements();

    public static virtual FieldList GetFields() => new();
}