using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("bounceBlock")]
public class BounceBlock : Entity, ISolid, IPlaceable {
    public override int Depth => Depths.SolidsBelow;

    public override IEnumerable<ISprite> GetSprites() {
        var isIce = Bool("notCoreMode", false);

        yield return ISprite.NineSliceFromTexture(Rectangle, isIce ? "objects/BumpBlockNew/ice00" : "objects/BumpBlockNew/fire00");

        yield return ISprite.FromTexture(Center, isIce ? "objects/BumpBlockNew/ice_center00" : "objects/BumpBlockNew/fire_center00").Centered();
    }

    public override bool ResizableX => true;
    public override bool ResizableY => true;

    public static FieldList GetFields() => new(new {
        notCoreMode = false
    });

    public static PlacementList GetPlacements() => [
        new Placement("fire") {
            AlternativeNames = [ "fire_core" ],
        },
        new Placement("ice", new { notCoreMode = true }) {
            AlternativeNames = [ "ice_core" ],
        }
    ];
    
    public override bool CanTrim(string key, object val) => IsDefault(key, val);
}
