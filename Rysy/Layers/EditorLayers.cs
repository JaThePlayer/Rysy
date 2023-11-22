﻿using Rysy.Helpers;
using Rysy.Selections;

namespace Rysy.Layers;

public static class EditorLayers {
    public static EditorLayer Fg { get; } = new TileEditorLayer(TileLayer.FG);
    public static EditorLayer Bg { get; } = new TileEditorLayer(TileLayer.BG);
    public static EditorLayer BothTilegrids { get; } = new FakeLayer("Both");
    
    public static EditorLayer Entities { get; } = new EntityLayer(SelectionLayer.Entities);
    public static EditorLayer Triggers { get; } = new EntityLayer(SelectionLayer.Triggers);
    public static EditorLayer FgDecals { get; } = new EntityLayer(SelectionLayer.FGDecals);
    public static EditorLayer BgDecals { get; } = new EntityLayer(SelectionLayer.BGDecals);

    public static EditorLayer Room { get; } = new FakeLayer("Rooms");

    public static EditorLayer CustomLayer { get; } = new FakeLayer("Custom");
    public static EditorLayer All { get; } = new FakeLayer("All");
    public static EditorLayer Prefabs { get; } = new PrefabLayer();

    private static List<EditorLayer> KnownLayers { get; } = new() {
        Fg, Bg, BothTilegrids, Entities, Triggers, FgDecals, BgDecals, Room, CustomLayer, All, Prefabs
    };

    public static bool IsDecalLayer(EditorLayer layer) => layer == FgDecals || layer == BgDecals;

    public static SelectionLayer ToolLayerToEnum(EditorLayer layer, SelectionLayer customLayer = SelectionLayer.None) =>
        layer.SelectionLayer switch {
            SelectionLayer.None => customLayer,
            var other => other,
        };

    public static EditorLayer EditorLayerFromName(string name) {
        foreach (var known in KnownLayers) {
            if (known.Name == name)
                return known;
        }

        var fake = new FakeLayer(name);
        KnownLayers.Add(fake);
        Logger.Write(nameof(EditorLayers), LogLevel.Warning, $"Creating fake layer {name}!");
        return fake;
    }
}
