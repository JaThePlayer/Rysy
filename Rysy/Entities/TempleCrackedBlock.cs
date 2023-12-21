using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("templeCrackedBlock")]
public class TempleCrackedBlock : Entity, ISolid, IPlaceable {
    public override int Depth => Depths.SolidsBelow;

    public override bool ResizableX => true;

    public override bool ResizableY => true;

    private static Sprite GetSprite(NineSliceLocation loc, int tx, int ty) {
        var (x, y) = loc switch {
            NineSliceLocation.TopLeft => (0, 0),
            NineSliceLocation.TopMiddle => (tx % 4 * 8 + 8, 0),
            NineSliceLocation.TopRight => (8 * 5, 0),

            NineSliceLocation.Left => (0, ty % 4 * 8 + 8),
            NineSliceLocation.Middle => (tx % 4 * 8 + 8, ty % 4 * 8 + 8),
            NineSliceLocation.Right => (8 * 5, ty % 4 * 8 + 8),

            NineSliceLocation.BottomLeft => (0, 40),
            NineSliceLocation.BottomMiddle => (tx % 4 * 8 + 8, 40),
            NineSliceLocation.BottomRight => (8 * 5, 40),
            _ => (0, 0),
        };
        return ISprite.FromTexture("objects/temple/breakBlock00").CreateSubtexture(x, y, 8, 8);
    }

    public override IEnumerable<ISprite> GetSprites() => NineSliceHelper.GetSprites(this, GetSprite);

    public static FieldList GetFields() => new(new {
        persistent = false
    });

    public static PlacementList GetPlacements() => new("temple_block");
}
