using Rysy.Helpers;
using Rysy.Selections;

namespace Rysy.Layers;

public sealed class PrefabLayer(PrefabHelper prefabs) : EditorLayer {
    public override string Name => "Prefabs";

    public override SelectionLayer SelectionLayer => SelectionLayer.All;

    public override IEnumerable<Placement> GetMaterials() =>
        prefabs.CurrentPrefabs.Select(s => prefabs.PlacementFromName(s.Key)!);
}