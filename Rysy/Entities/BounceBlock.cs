using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("bounceBlock")]
public class BounceBlock : Entity, ISolid, IPlaceable {
    const int SpriteSize = 64;
    const int Tiles = SpriteSize / 8 - 2;

    public override int Depth => Depths.SolidsBelow;

    private static Sprite GetSpriteHot(NineSliceLocation loc, int tx, int ty) {
        GetSubtexturePos(loc, tx, ty, out int x, out int y);
        return ISprite.FromTexture("objects/BumpBlockNew/fire00").CreateSubtexture(x, y, 8, 8);
    }

    private static Sprite GetSpriteCold(NineSliceLocation loc, int tx, int ty) {
        GetSubtexturePos(loc, tx, ty, out int x, out int y);
        return ISprite.FromTexture("objects/BumpBlockNew/ice00").CreateSubtexture(x, y, 8, 8);
    }

    private static void GetSubtexturePos(NineSliceLocation loc, int tx, int ty, out int x, out int y) {
        tx -= 1;
        ty -= 1;
        (x, y) = loc switch {
            NineSliceLocation.TopLeft => (0, 0),
            NineSliceLocation.TopMiddle => (tx % Tiles * 8 + 8, 0),
            NineSliceLocation.TopRight => (SpriteSize - 8, 0),

            NineSliceLocation.Left => (0, ty % Tiles * 8 + 8),
            NineSliceLocation.Middle => (tx % Tiles * 8 + 8, ty % Tiles * 8 + 8),
            NineSliceLocation.Right => (SpriteSize - 8, ty % Tiles * 8 + 8),

            NineSliceLocation.BottomLeft => (0, SpriteSize - 8),
            NineSliceLocation.BottomMiddle => (tx % Tiles * 8 + 8, SpriteSize - 8),
            NineSliceLocation.BottomRight => (SpriteSize - 8, SpriteSize - 8),
            _ => (0, 0),
        };
    }

    public override IEnumerable<ISprite> GetSprites() {
        var isIce = Bool("notCoreMode", false);

        foreach (var item in NineSliceHelper.GetSprites(this, isIce ? GetSpriteCold : GetSpriteHot)) {
            yield return item;
        }

        yield return ISprite.FromTexture(Center, isIce ? "objects/BumpBlockNew/ice_center00" : "objects/BumpBlockNew/fire_center00").Centered();
    }

    public override bool ResizableX => true;
    public override bool ResizableY => true;

    public static FieldList GetFields() => new(new {
        notCoreMode = false
    });

    public static PlacementList GetPlacements() => [
        new("fire"),
        new("ice", new { notCoreMode = true })
    ];
    
    public override bool CanTrim(string key, object val) => IsDefault(key, val);
}
