using Rysy.Helpers;
using Rysy.Selections;

namespace Rysy.Layers;

public static class EditorLayers {
    public static TileEditorLayer Fg { get; } = new TileEditorLayer(TileLayer.Fg);
    public static TileEditorLayer Bg { get; } = new TileEditorLayer(TileLayer.Bg);
    public static EditorLayer BothTilegrids { get; } = new FakeLayer("Both");
    
    public static EntityLayer Entities { get; } = new EntityLayer(SelectionLayer.Entities);
    public static EntityLayer Triggers { get; } = new EntityLayer(SelectionLayer.Triggers);
    public static EntityLayer FgDecals { get; } = new EntityLayer(SelectionLayer.FgDecals);
    public static EntityLayer BgDecals { get; } = new EntityLayer(SelectionLayer.BgDecals);

    public static RoomLayer Room { get; } = new RoomLayer();

    public static CustomSelectionLayer CustomLayer { get; } = new CustomSelectionLayer();
    public static AllLayer All { get; } = new AllLayer();

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
        registry.Add(Entities);
        registry.Add(Triggers);
        registry.Add(FgDecals);
        registry.Add(BgDecals);
        registry.Add(Fg);
        registry.Add(Bg);
        registry.Add(BothTilegrids);
        registry.Add(Room);
        registry.Add(All);
        registry.Add(CustomLayer);
        registry.Add(new PrefabLayer(registry.GetRequired<PrefabHelper>()));
    }
}
