using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.Selections;

namespace Rysy.Entities;

[CustomEntity("triggerSpikesOriginalUp")]
[CustomEntity("triggerSpikesOriginalDown")]
[CustomEntity("triggerSpikesOriginalLeft")]
[CustomEntity("triggerSpikesOriginalRight")]
public sealed class TriggerSpikes : Entity, IMultiSIDPlaceable {
    public override int Depth => -1;

    public static SpikeHelper.Direction Direction(string sid) => sid switch {
        "triggerSpikesOriginalDown" => SpikeHelper.Direction.Down,
        "triggerSpikesOriginalLeft" => SpikeHelper.Direction.Left,
        "triggerSpikesOriginalRight" => SpikeHelper.Direction.Right,
        _ => SpikeHelper.Direction.Up,
    };

    public override bool ResizableX => Direction(Name) is SpikeHelper.Direction.Up or SpikeHelper.Direction.Down;
    public override bool ResizableY => Direction(Name) is SpikeHelper.Direction.Left or SpikeHelper.Direction.Right;

    public override Entity? TryFlipHorizontal() => Direction(Name) switch {
        SpikeHelper.Direction.Left => CloneWith(pl => pl.SID = "triggerSpikesOriginalRight"),
        SpikeHelper.Direction.Right => CloneWith(pl => pl.SID = "triggerSpikesOriginalLeft"),
        _ => null,
    };

    public override Entity? TryFlipVertical() => Direction(Name) switch {
        SpikeHelper.Direction.Up => CloneWith(pl => pl.SID = "triggerSpikesOriginalDown"),
        SpikeHelper.Direction.Down => CloneWith(pl => pl.SID = "triggerSpikesOriginalUp"),
        _ => null,
    };

    public override Entity? TryRotate(RotationDirection dir) => CloneWith(pl => pl.WithSID(dir.AddRotationTo(Direction(Name)) switch {
        SpikeHelper.Direction.Up => "triggerSpikesOriginalUp",
        SpikeHelper.Direction.Down => "triggerSpikesOriginalDown",
        SpikeHelper.Direction.Left => "triggerSpikesOriginalLeft",
        SpikeHelper.Direction.Right => "triggerSpikesOriginalRight",
        _ => throw new NotImplementedException(),
    }).SwapWidthAndHeight());

    public override IEnumerable<ISprite> GetSprites()
        => SpikeHelper.GetSprites(this, Direction(Name), Attr("type", "default"), triggerSpikes: true);

    public override ISelectionCollider GetMainSelection()
        => SpikeHelper.GetSelection(this, Direction(Name));

    public static FieldList GetFields(string sid) => new(new {
        type = SpikeHelper.GetTypeField()
    });

    public static PlacementList GetPlacements(string sid) {
        var prefix = Direction(sid) switch {
            SpikeHelper.Direction.Up => "up",
            SpikeHelper.Direction.Down => "down",
            SpikeHelper.Direction.Left => "left",
            _ => "right",
        };

        return SpikeHelper.CreatePlacements((t) => $"{prefix}_{t}");
    }
}