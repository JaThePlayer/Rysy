using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("starJumpBlock")]
public class StarJumpBlock : Entity
{
    public override int Depth => Depths.Solids;

    private int Rng(int max) => Room.Random.Next(max);

    public string EdgeH => $"objects/starjumpBlock/edgeH{Rng(3):d2}";
    public string EdgeV => $"objects/starjumpBlock/edgeV{Rng(3):d2}";
    public string Corner => $"objects/starjumpBlock/corner{Rng(3):d2}";

    private Sprite GetSprite(NineSliceLocation loc)
    {
        var (texture, sx, sy) = loc switch
        {
            NineSliceLocation.TopLeft => (Corner, -1f, 1f),
            NineSliceLocation.TopMiddle => (EdgeH, 1f, 1f),
            NineSliceLocation.TopRight => (Corner, 1f, 1f),
            NineSliceLocation.Left => (EdgeV, -1f, 1f),
            NineSliceLocation.Middle => (null, 1f, 1f),
            NineSliceLocation.Right => (EdgeV, 1f, 1f),
            NineSliceLocation.BottomLeft => (Corner, -1f, -1f),
            NineSliceLocation.BottomMiddle => (EdgeH, 1f, -1f),
            NineSliceLocation.BottomRight => (Corner, 1f, -1f),
            _ => throw new NotImplementedException(),
        };

        return ISprite.FromTexture(texture ?? "") with
        {
            Scale = new(sx, sy),
            Origin = new(.5f, .5f),
            Pos = new(4f)
        };
    }

    public override IEnumerable<ISprite> GetSprites()
    {
        return ConnectedEntityHelper.GetSprites(this, Room.Entities[typeof(StarJumpBlock)], GetSprite, ignoreMiddle: true, handleInnerCorners: false);
    }
}
