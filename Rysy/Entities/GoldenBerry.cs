using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("goldenBerry")]
public sealed class GoldenBerry : SpriteEntity, IPlaceable {
    public override int Depth => 0;

    public override string TexturePath => (Bool("winged"), Nodes is [..]) switch {
        (true, true) => "collectables/ghostgoldberry/wings01",
        (true, false) => "collectables/goldberry/wings01",
        (false, true) => "collectables/ghostgoldberry/idle00",
        (false, false) => "collectables/goldberry/idle00",
    };

    public override IEnumerable<ISprite> GetNodeSprites(int nodeIndex)
        => ISprite.FromTexture(Nodes[nodeIndex], "collectables/goldberry/seed00");

    public static FieldList GetFields() => new(new {
        winged = false,
        moon = false
    });

    public static PlacementList GetPlacements() => new() {
        new("golden"),
        new("golden_winged", new {
            winged = true,
        })
    };
}