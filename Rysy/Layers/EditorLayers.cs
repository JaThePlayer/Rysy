using Rysy.Helpers;
using Rysy.Selections;

namespace Rysy.Layers;

public static class EditorLayers {
    public static EditorLayer Fg { get; } = new TileEditorLayer(TileLayer.Fg);
    public static EditorLayer Bg { get; } = new TileEditorLayer(TileLayer.Bg);
    public static EditorLayer BothTilegrids { get; } = new FakeLayer("Both");
    
    public static EditorLayer Entities { get; } = new EntityLayer(SelectionLayer.Entities);
    public static EditorLayer Triggers { get; } = new EntityLayer(SelectionLayer.Triggers);
    public static EditorLayer FgDecals { get; } = new EntityLayer(SelectionLayer.FgDecals);
    public static EditorLayer BgDecals { get; } = new EntityLayer(SelectionLayer.BgDecals);

    public static EditorLayer Room { get; } = new RoomLayer();

    public static EditorLayer CustomLayer { get; } = new FakeLayer("Custom");
    public static EditorLayer All { get; } = new FakeLayer("All", SelectionLayer.All);

    public static EditorLayer Missing { get; } = new FakeLayer("Missing");

    public static SelectionLayer ToolLayerToEnum(IEditorLayer layer, SelectionLayer customLayer = SelectionLayer.None) =>
        layer.SelectionLayer switch {
            SelectionLayer.None => customLayer,
            var other => other,
        };

    internal static IEditorLayer? LayerFromSelectionLayer(SelectionLayer selectionLayer, IReadOnlyList<IEditorLayer> layers) {
        foreach (var layer in layers) {
            if (layer.SelectionLayer == selectionLayer)
                return layer;
        }

        return null;
    }

    public static IEditorLayer? EditorLayerFromName(string? name, IReadOnlyList<IEditorLayer> layers) {
        if (name is null)
            return null;
        
        foreach (var known in layers) {
            if (known.Name == name)
                return known;
        }

        return null;
    }

    public static void RegisterVanillaLayers(IComponentRegistry registry) {
        registry.Add(Fg);
        registry.Add(Bg);
        registry.Add(BothTilegrids);
        registry.Add(Entities);
        registry.Add(Triggers);
        registry.Add(FgDecals);
        registry.Add(BgDecals);
        registry.Add(Room);
        registry.Add(CustomLayer);
        registry.Add(All);
        registry.Add(new PrefabLayer(registry.GetRequired<PrefabHelper>()));
    }
}
