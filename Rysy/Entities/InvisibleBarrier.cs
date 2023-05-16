using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("invisibleBarrier")]
public sealed class InvisibleBarrier : RectangleEntity, IPlaceable {
    public override int Depth => 0;

    public override Color FillColor => new(0.4f, 0.4f, 0.4f, 0.8f);

    public override Color OutlineColor => new(0.5f, 0.5f, 0.5f, 1f);

    public static FieldList GetFields() => new(new {

    });

    public static PlacementList GetPlacements() => new("invisible_barrier");
}