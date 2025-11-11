using Rysy.Helpers;
using Rysy.Selections;

namespace Rysy.Layers; 

public class EntityLayer : EditorLayer {
    public EntityLayer(SelectionLayer layer) {
        SelectionLayer = layer;
    }

    public override string Name => SelectionLayer.FastToString();

    public override string? MaterialLangPrefix => SelectionLayer switch {
        SelectionLayer.Entities => "entities",
        SelectionLayer.Triggers => "triggers",
        _ => null,
    };

    public override SelectionLayer SelectionLayer { get; }
    public override IEnumerable<Placement> GetMaterials() {
        return SelectionLayer switch {
            SelectionLayer.Entities => EntityRegistry.EntityPlacements,
            SelectionLayer.Triggers => EntityRegistry.TriggerPlacements,
            SelectionLayer.FgDecals => FgDecalPlacements.Value,
            SelectionLayer.BgDecals => BgDecalPlacements.Value,
            _ => throw new NotImplementedException(SelectionLayer.FastToString())
        };
    }
    
    private static Cache<List<Placement>> FgDecalPlacements { get; }
        = Decal.ValidDecalPaths.Chain((paths) => paths.Select(p => PlacementFromString(p, SelectionLayer.FgDecals)!).ToList());
    private static Cache<List<Placement>> BgDecalPlacements { get; }
        = Decal.ValidDecalPaths.Chain((paths) => paths.Select(p => PlacementFromString(p, SelectionLayer.BgDecals)!).ToList());

    private static Placement? PlacementFromString(string str, SelectionLayer layer) {
        return layer switch {
            SelectionLayer.FgDecals => Decal.PlacementFromPath(str, true, Vector2.One, Color.White, rotation: 0f),
            SelectionLayer.BgDecals => Decal.PlacementFromPath(str, false, Vector2.One, Color.White, rotation: 0f),
            _ => null,
        };
    }
}