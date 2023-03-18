namespace Rysy.Helpers;

public static class LayerNames {
    public const string FG = "FG";
    public const string BG = "BG";
    public const string BOTH_TILEGRIDS = "Both";
    public const string ENTITIES = "Entities";
    public const string TRIGGERS = "Triggers";
    public const string FG_DECALS = "FGDecals";
    public const string BG_DECALS = "BGDecals";
    public const string ROOM = "Rooms";

    public const string CUSTOM_LAYER = "Custom";
    public const string ALL = "All";

    public static SelectionLayer ToolLayerToEnum(string layer, SelectionLayer customLayer = SelectionLayer.None) => layer switch {
        FG => SelectionLayer.FGTiles,
        BG => SelectionLayer.BGTiles,
        FG_DECALS => SelectionLayer.FGDecals,
        BG_DECALS => SelectionLayer.BGDecals,
        ENTITIES => SelectionLayer.Entities,
        TRIGGERS => SelectionLayer.Triggers,
        ROOM => SelectionLayer.Rooms,
        ALL => SelectionLayer.All,
        CUSTOM_LAYER => customLayer,
        _ => SelectionLayer.None,
    };
}
