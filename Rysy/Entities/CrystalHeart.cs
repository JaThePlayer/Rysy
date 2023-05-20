using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("blackGem")]
public class CrystalHeart : SpriteEntity, IPlaceable {
    public override string TexturePath => "collectables/heartGem/0/00";

    public override int Depth => 0;

    public static FieldList GetFields() => new(new {
        fake = false,
        removeCameraTriggers = false,
        fakeHeartDialog = "CH9_FAKE_HEART",
        keepGoingDialog = "CH9_KEEP_GOING"
    });

    public static PlacementList GetPlacements() => new("crystal_heart");
}
