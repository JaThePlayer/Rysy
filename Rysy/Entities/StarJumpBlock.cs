using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("starJumpBlock")]
public class StarJumpBlock : Entity {
    private Random rng;

    public override int Depth => Depths.Solids;

    private int Rng(Random rng, int max) => rng.Next(max);

    public string EdgeH(Random rng) => $"objects/starjumpBlock/edgeH{Rng(rng, 3):d2}";
    public string EdgeV(Random rng) => $"objects/starjumpBlock/edgeV{Rng(rng, 3):d2}";
    public string Corner(Random rng) => $"objects/starjumpBlock/corner{Rng(rng, 3):d2}";

    private Sprite GetSprite(NineSliceLocation loc) {
        var (texture, sx, sy) = loc switch {
            NineSliceLocation.TopLeft => (Corner(rng), -1f, 1f),
            NineSliceLocation.TopMiddle => (EdgeH(rng), 1f, 1f),
            NineSliceLocation.TopRight => (Corner(rng), 1f, 1f),
            NineSliceLocation.Left => (EdgeV(rng), -1f, 1f),
            NineSliceLocation.Middle => (null, 1f, 1f),
            NineSliceLocation.Right => (EdgeV(rng), 1f, 1f),
            NineSliceLocation.BottomLeft => (Corner(rng), -1f, -1f),
            NineSliceLocation.BottomMiddle => (EdgeH(rng), 1f, -1f),
            NineSliceLocation.BottomRight => (Corner(rng), 1f, -1f),
            _ => throw new NotImplementedException(),
        };

        return ISprite.FromTexture(texture ?? "") with {
            Scale = new(sx, sy),
            Origin = new(.5f, .5f),
            Pos = new(4f)
        };
    }

    public override IEnumerable<ISprite> GetSprites() {
        rng = new Random((int) Pos.SeededRandom());
        return ConnectedEntityHelper.GetSprites(this, Room.Entities[typeof(StarJumpBlock)], GetSprite, ignoreMiddle: true, handleInnerCorners: false);
    }
}
