using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("hanginglamp")]
public class HangingLamp : LoopingSpriteSliceEntity, IPlaceable {
    public override int TileSize => 8;

    public override string TexturePath => "objects/hanginglamp";

    public override int Depth => 2000;

    public override LoopingMode LoopMode => LoopingMode.UseEdgeTiles_RepeatMiddle;

    public override bool ResizableY => true;

    // Hanging lamps are a bit special, and the topmost subtexture doesn't contain the chain
    // because of this, we need to add it in ourselves
    public override IEnumerable<ISprite> GetSprites() {
        // add the chain at the base of the lamp
        yield return GetSprite().CreateSubtexture(0, 8, 8, 8) with {
            Origin = new(),
        };

        foreach (var item in base.GetSprites())
            yield return item;
    }

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("hanging_lamp");

}
