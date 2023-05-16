using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("fireBarrier")]
public sealed class FireBarrier : RectangleEntity, IPlaceable {
    public override Color FillColor => new(209f / 255f, 9f / 255f, 1f / 255f, 102f / 255f);

    public override Color OutlineColor => new(246f / 255f, 98f / 255f, 18f / 255f);

    public override int Depth => -8500;

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("fire_barrier");
}
