using Rysy.Extensions;
using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("resortRoofEnding")]
public sealed class ResortRoofEnding : Entity, IPlaceable {
    public override int Depth => 0;

    public override bool ResizableX => true;

    private static readonly string[] Textures = new string[] {     
        "decals/3-resort/roofCenter",
        "decals/3-resort/roofCenter_b",
        "decals/3-resort/roofCenter_c",
        "decals/3-resort/roofCenter_d"
    };

    public override IEnumerable<ISprite> GetSprites() {
        var pos = Pos.Add(8, 4);
        yield return ISprite.FromTexture(pos, "decals/3-resort/roofEdge_d").Centered();

        for (int x = 0; x < Width; x += 16) {
            yield return ISprite.FromTexture(pos, pos.SeededRandomFrom(Textures)).Centered();
            pos = pos.AddX(16);
        }

        yield return ISprite.FromTexture(pos, "decals/3-resort/roofEdge").Centered();
    }

    public static FieldList GetFields() => new(new {

    });

    public static PlacementList GetPlacements() => new("resort_roof_ending");
}