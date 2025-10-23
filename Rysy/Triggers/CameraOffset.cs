namespace Rysy.Triggers;

[CustomEntity("cameraOffsetTrigger")]
public sealed class CameraOffset : Trigger, IPlaceable {
    public override string Category => TriggerCategories.Camera;

    public static FieldList GetFields() => new(new {
        cameraX = 0.0,
        cameraY = 0.0
    });

    public static PlacementList GetPlacements() => new("camera_offset");
}