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

public class EntityPlacementHandler : IPlacementHandler {
    public static EntityPlacementHandler Instance = new();

    private static Entity CreateFromPlacement(Placement placement, Vector2 pos, Room room) {
        return EntityRegistry.Create(placement, pos, room, false, false);
    }

    public IEnumerable<ISprite> GetPreviewSprites(Placement placement, Vector2 pos, Room room) {
        return CreateFromPlacement(placement, pos, room).GetSprites();
    }

    public IHistoryAction Place(Placement placement, Vector2 pos, Room room) {
        var entity = CreateFromPlacement(placement, pos, room);

        return new AddEntityAction(entity, room);
    }
}

public class TriggerPlacementHandler : IPlacementHandler {
    public static TriggerPlacementHandler Instance = new();

    private static Entity CreateFromPlacement(Placement placement, Vector2 pos, Room room) {
        return EntityRegistry.Create(placement, pos, room, false, true);
    }

    public IEnumerable<ISprite> GetPreviewSprites(Placement placement, Vector2 pos, Room room) {
        return CreateFromPlacement(placement, pos, room).GetSprites();
    }

    public IHistoryAction Place(Placement placement, Vector2 pos, Room room) {
        var entity = CreateFromPlacement(placement, pos, room);

        return new AddEntityAction(entity, room);
    }
}

public class DecalPlacementHandler : IPlacementHandler {
    public bool FG { get; init; }

    public static DecalPlacementHandler FGInstance = new() { FG = true };
    public static DecalPlacementHandler BGInstance = new() { FG = false };

    public IEnumerable<ISprite> GetPreviewSprites(Placement placement, Vector2 pos, Room room) {
        return new ISprite[] { Decal.FromPlacement(placement, pos, room, FG).GetSprite() };
    }

    public IHistoryAction Place(Placement placement, Vector2 pos, Room room) {
        var decal = Decal.FromPlacement(placement, pos, room, FG);

        return new AddDecalAction(decal, room);
    }
}

public interface IConvertibleToPlacement {
    public Placement ToPlacement();
}

public interface IPlaceable {
    public static abstract List<Placement>? GetPlacements();

    public static virtual FieldList GetFields() => new();
}