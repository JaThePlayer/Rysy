using Rysy.Helpers;
using BirdNPCModes = Rysy.Helpers.CelesteEnums.BirdNPCModes;

namespace Rysy.Entities;

[CustomEntity("bird")]
public class BirdNpc : SpriteEntity, IPlaceable {
    public override string TexturePath => "characters/bird/crow00";

    public override int Depth => -1000000;

    public override Vector2 Origin => new(0.5f, 1.0f);

    public override Range NodeLimits => 0..;

    public override Vector2 Scale => new(Enum("mode", BirdNPCModes.Sleeping) switch {
        BirdNPCModes.ClimbingTutorial => -1,
        BirdNPCModes.DashingTutorial => 1,
        BirdNPCModes.DreamJumpTutorial => 1,
        BirdNPCModes.SuperWallJumpTutorial => -1,
        BirdNPCModes.HyperJumpTutorial => -1,
        BirdNPCModes.MoveToNodes => -1,
        BirdNPCModes.WaitForLightningOff => -1,
        BirdNPCModes.FlyAway => -1,
        BirdNPCModes.Sleeping => 1,
        _ => -1
    }, 1f);

    public static FieldList GetFields() => new(new {
        mode = BirdNPCModes.Sleeping,
        onlyOnce = false,
        onlyIfPlayerLeft = false
    });

    public static PlacementList GetPlacements() => new("bird");
}
