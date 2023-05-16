using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("darkChaserEnd")]
public class BadelineChaserBarrier : RectangleEntity, IPlaceable {
    public override Color FillColor => new(0.4f, 0.0f, 0.4f, 0.4f);

    public override Color OutlineColor => new(0.4f, 0.0f, 0.4f, 1.0f);

    public override int Depth => 0;

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("barrier");
}
