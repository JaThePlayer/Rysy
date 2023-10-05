using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("wallBooster")]
public class WallBooster : Entity, IPlaceable {
    public override int Depth => 1999;

    public virtual string GetSprite(bool ice, SliceLocation loc) => (ice, loc) switch {
        (false, SliceLocation.Top) => "objects/wallBooster/fireTop00",
        (false, SliceLocation.Bottom) => "objects/wallBooster/fireBottom00",
        (false, _) => "objects/wallBooster/fireMid00",
        (true, SliceLocation.Top) => "objects/wallBooster/iceTop00",
        (true, SliceLocation.Bottom) => "objects/wallBooster/iceBottom00",
        (true, _) => "objects/wallBooster/iceMid00",
    };

    public virtual bool Ice => Bool("notCoreMode", false);
    public virtual bool Left => Bool("left", false);

    public override bool ResizableY => true;

    public override IEnumerable<ISprite> GetSprites() {
        var ice = Ice;
        var left = Left;

        var h = Height;

        for (int i = 0; i < h; i += 8) {
            yield return ISprite.FromTexture(GetSprite(ice, GetSliceLocation(i, h))) with {
                Pos = Pos + new Vector2(left ? 0 : 8, i),
                Scale = new(left ? 1f : -1f, 1f)
            };
        }
    }

    public override Entity? TryFlipHorizontal() => CloneWith(pl => pl["left"] = !Left);

    public static SliceLocation GetSliceLocation(int current, int max, int sliceSize = 8) {
        return current == 0 ? SliceLocation.Top
             : (current + sliceSize < max) ? SliceLocation.Middle
             : SliceLocation.Bottom;
    }

    public static FieldList GetFields() => new(new {
        left = false,
        notCoreMode = false
    });

    public static PlacementList GetPlacements() => new() {
        new("booster_right", new {
            left = true,
        }),
        new("booster_left"),
        new("ice_right", new {
            notCoreMode = true,
            left = true,
        }),
        new("ice_left", new {
            notCoreMode = true,
        }),
    };

    public enum SliceLocation {
        Top = 0,
        Middle = 1,
        Bottom = 2,
    }
}
