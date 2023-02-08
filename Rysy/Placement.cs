namespace Rysy;

public record class Placement(string Name) {
    public string? SID;

    public string? Tooltip;

    // set in entity registry
    public bool IsTrigger { get; internal set; }

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
}

public interface IPlaceable {
    public static abstract List<Placement>? GetPlacements();

    public static virtual FieldList GetFields() => new();
}