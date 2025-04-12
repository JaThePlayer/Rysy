using Rysy.Helpers;
using Rysy.Selections;

namespace Rysy.Layers;

public class PrefabLayer() : EditorLayer("Prefabs") {
    public override SelectionLayer SelectionLayer => SelectionLayer.All;

    public override IEnumerable<Placement> GetMaterials() =>
        PrefabHelper.CurrentPrefabs.Select(s => PrefabHelper.PlacementFromName(s.Key)!);
}