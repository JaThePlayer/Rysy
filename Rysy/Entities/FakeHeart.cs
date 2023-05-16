using Rysy.Helpers;
using HeartColors = Rysy.Helpers.CelesteEnums.HeartColors;

namespace Rysy.Entities;

[CustomEntity("fakeHeart")]
public sealed class FakeHeart : SpriteEntity, IPlaceable {
    public override string TexturePath => Enum("color", HeartColors.Normal) switch {
        HeartColors.BSide => "collectables/heartGem/1/00",
        HeartColors.CSide => "collectables/heartGem/2/00",
        _ => "collectables/heartGem/0/00",
    };

    public override int Depth => -2000000;

    public static FieldList GetFields() => new(new {
        color = HeartColors.Random
    });

    public static PlacementList GetPlacements() => new("crystal_heart");
}
