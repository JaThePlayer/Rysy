using Rysy.Helpers;

namespace Rysy.Triggers;

[CustomEntity("everest/smoothCameraOffsetTrigger")]
public sealed class SmoothCameraOffset : Trigger, IPlaceable {
    public override string Category => TriggerCategories.Camera;

    public static FieldList GetFields() => new(new {
        offsetXFrom = 0.0,
        offsetXTo = 0.0,
        offsetYFrom = 0.0,
        offsetYTo = 0.0,
        positionMode = CelesteEnums.PositionModes.NoEffect,
        onlyOnce = false,
        xOnly = false,
        yOnly = false
    });

    public static PlacementList GetPlacements() => new("smooth_camera");
}