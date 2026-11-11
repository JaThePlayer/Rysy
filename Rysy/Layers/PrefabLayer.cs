using Rysy.Helpers;
using Rysy.Selections;

namespace Rysy.Layers;

public sealed class PrefabLayer(PrefabHelper prefabs) : EditorLayer, IPlacementEditorLayer {
    public PrefabHelper PrefabHelper => prefabs;
    
    public override string Name => "Prefabs";

    public override SelectionLayer SelectionLayer => SelectionLayer.All;

    public override IReadOnlyList<object> GetContents(Room room) => [];

    public override Cache<IReadOnlyList<Placement>> Materials => field ??= prefabs.CurrentPrefabs.CreateCache(
        IReadOnlyList<Placement> (d) => d
            .Select(s => prefabs.PlacementFromName(s.Key)!)
            .ToList());
}