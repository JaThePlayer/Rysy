using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("starJumpBlock")]
public class StarJumpBlock : Entity, IPlaceable {
    public override int Depth => Depths.Solids;

    public override bool ResizableX => true;
    public override bool ResizableY => true;

    public virtual string EdgeH(Vector2 pos) => $"objects/starjumpBlock/edgeH{pos.SeededRandomInclusive(0, 3):d2}";
    public virtual string EdgeV(Vector2 pos) => $"objects/starjumpBlock/edgeV{pos.SeededRandomInclusive(0, 3):d2}";
    public virtual string Corner(Vector2 pos) => $"objects/starjumpBlock/corner{pos.SeededRandomInclusive(0, 3):d2}";

    private Sprite GetSprite(Vector2 pos, NineSliceLocation loc) {
        var (texture, sx, sy) = loc switch {
            NineSliceLocation.TopLeft => (Corner(pos), -1f, 1f),
            NineSliceLocation.TopMiddle => (EdgeH(pos), 1f, 1f),
            NineSliceLocation.TopRight => (Corner(pos), 1f, 1f),
            NineSliceLocation.Left => (EdgeV(pos), -1f, 1f),
            NineSliceLocation.Middle => ("", 1f, 1f),
            NineSliceLocation.Right => (EdgeV(pos), 1f, 1f),
            NineSliceLocation.BottomLeft => (Corner(pos), -1f, -1f),
            NineSliceLocation.BottomMiddle => (EdgeH(pos), 1f, -1f),
            NineSliceLocation.BottomRight => (Corner(pos), 1f, -1f),
            _ => throw new NotImplementedException(),
        };

        return ISprite.FromTexture(pos.Add(4, 4), texture) with {
            Scale = new(sx, sy),
            Origin = new(.5f, .5f),
        };
    }

    public override IEnumerable<ISprite> GetSprites() {
        return ConnectedEntityHelper.GetSprites(this, Room.Entities["starJumpBlock"], GetSprite, ignoreMiddle: true, handleInnerCorners: false);
    }

    public static FieldList GetFields() => new(new {
        sinks = true
    });

    public static PlacementList GetPlacements() => new("star_jump_block");
}
