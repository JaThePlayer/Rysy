using Rysy.Helpers;
using Rysy.Selections;

namespace Rysy.Layers; 

public class EntityLayer : EditorLayer {
    public EntityLayer(SelectionLayer layer) {
        SelectionLayer = layer;
    }

    public override string Name => SelectionLayer.FastToString();
    public override SelectionLayer SelectionLayer { get; }
    public override IEnumerable<Placement> GetMaterials() {
        return SelectionLayer switch {
            SelectionLayer.Entities => EntityRegistry.EntityPlacements,
            SelectionLayer.Triggers => EntityRegistry.TriggerPlacements,
            SelectionLayer.FGDecals => FgDecalPlacements.Value,
            SelectionLayer.BGDecals => BgDecalPlacements.Value,
            _ => throw new NotImplementedException(SelectionLayer.FastToString())
        };
    }
    
    private static Cache<List<Placement>> FgDecalPlacements { get; }
        = Decal.ValidDecalPaths.Chain((paths) => paths.Select(p => PlacementFromString(p, SelectionLayer.FGDecals)!).ToList());
    private static Cache<List<Placement>> BgDecalPlacements { get; }
        = Decal.ValidDecalPaths.Chain((paths) => paths.Select(p => PlacementFromString(p, SelectionLayer.BGDecals)!).ToList());

    private static Placement? PlacementFromString(string str, SelectionLayer layer) {
        return layer switch {
            SelectionLayer.FGDecals => Decal.PlacementFromPath(str, true, Vector2.One, Color.White, rotation: 0f),
            SelectionLayer.BGDecals => Decal.PlacementFromPath(str, false, Vector2.One, Color.White, rotation: 0f),
            _ => null,
        };
    }
}