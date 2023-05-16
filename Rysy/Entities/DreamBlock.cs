using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("dreamBlock")]
public class DreamBlock : RectangleEntity, ISolid, IPlaceable {
    public override Color FillColor => Color.Black;

    public override Color OutlineColor => Color.White;

    public override int Depth => Bool("below") ? 5000 : -11000;

    public override Range NodeLimits => 0..1;

    public static FieldList GetFields() => new(new {
        fastMoving = false,
        below = false,
    });

    public static PlacementList GetPlacements() => new("dream_block");

    public override bool ResizableX => true;
    public override bool ResizableY => true;
}
