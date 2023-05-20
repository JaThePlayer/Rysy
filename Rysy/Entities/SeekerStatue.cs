using Rysy.Graphics;
using Rysy.Helpers;
using static Rysy.Helpers.CelesteEnums;

namespace Rysy.Entities;

[CustomEntity("seekerStatue")]
public sealed class SeekerStatue : SpriteEntity, IPlaceable {
    public override int Depth => 0;

    public override string TexturePath => "decals/5-temple/statue_e";

    public override IEnumerable<ISprite> GetNodeSprites(int nodeIndex)
        => GetSprite("characters/monsters/predator73");

    public static FieldList GetFields() => new(new {
        hatch = SeekerStatueHatches.Distance
    });

    public static PlacementList GetPlacements() => new("seeker_statue");
}