namespace Rysy.Helpers;

public enum TileLayer {
    BG,
    FG
}

public static class TileLayerExt {
    public static string FastToString(this TileLayer layer) => layer switch {
        TileLayer.BG => "BG",
        TileLayer.FG => "FG",
        _ => layer.ToString(),
    };
}
