namespace Rysy.Helpers;

public enum TileLayer {
    Bg,
    Fg
}

public static class TileLayerExt {
    public static string FastToString(this TileLayer layer) => layer switch {
        TileLayer.Bg => "BG",
        TileLayer.Fg => "FG",
        _ => layer.ToString(),
    };
}
