using static Rysy.Helpers.CelesteEnums;

namespace Rysy.Triggers;

[CustomEntity("cameraAdvanceTargetTrigger")]
public sealed class CameraAdvanceTarget : Trigger, IPlaceable {
    public override Range NodeLimits => 1..1;

    public static FieldList GetFields() => new(new {
        lerpStrengthX = 1.0,
        lerpStrengthY = 1.0,
        positionModeX = PositionModes.NoEffect,
        positionModeY = PositionModes.NoEffect,
        xOnly = false,
        yOnly = false
    });

    public static PlacementList GetPlacements() => new("camera_advance_target");
}