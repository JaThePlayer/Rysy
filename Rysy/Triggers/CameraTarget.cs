using static Rysy.Helpers.CelesteEnums;

namespace Rysy.Triggers;

[CustomEntity("cameraTargetTrigger")]
public sealed class CameraTarget : Trigger, IPlaceable {
    public override string Category => TriggerCategories.Camera;

    public override Range NodeLimits => 1..1;

    public static FieldList GetFields() => new(new {
        lerpStrength = 1.0,
        positionMode = PositionModes.NoEffect,
        xOnly = false,
        yOnly = false,
        deleteFlag = ""
    });

    public static PlacementList GetPlacements() => new("camera_target");
}