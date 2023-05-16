using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("finalBoss")]
public sealed class BadelineBoss : SpriteEntity, IPlaceable {
    public override string TexturePath => "characters/badelineBoss/charge00";

    public override int Depth => -1000000;

    public override Range NodeLimits => 0..;

    public static FieldList GetFields() => new(new {
        patternIndex = Fields.Dropdown(1, CelesteEnums.BadelineBossShootingPatterns),
        startHit = false,
        cameraPastY = 120.0,
        cameraLockY = true,
        canChangeMusic = true
    });

    public static PlacementList GetPlacements() => new("boss");
}
