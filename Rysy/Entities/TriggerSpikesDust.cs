using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.Selections;

namespace Rysy.Entities;

[CustomEntity("triggerSpikesUp")]
[CustomEntity("triggerSpikesDown")]
[CustomEntity("triggerSpikesLeft")]
[CustomEntity("triggerSpikesRight")]
public sealed class TriggerSpikesDust : Entity, IMultiSIDPlaceable {
    public override int Depth => 0;
    public override bool ResizableX => Direction(Name) is SpikeHelper.Direction.Up or SpikeHelper.Direction.Down;
    public override bool ResizableY => Direction(Name) is SpikeHelper.Direction.Left or SpikeHelper.Direction.Right;

    public override Entity? TryFlipHorizontal() => Direction(Name) switch {
        SpikeHelper.Direction.Left => CloneWith(pl => pl.SID = "triggerSpikesRight"),
        SpikeHelper.Direction.Right => CloneWith(pl => pl.SID = "triggerSpikesLeft"),
        _ => null,
    };

    public override Entity? TryFlipVertical() => Direction(Name) switch {
        SpikeHelper.Direction.Up => CloneWith(pl => pl.SID = "triggerSpikesDown"),
        SpikeHelper.Direction.Down => CloneWith(pl => pl.SID = "triggerSpikesUp"),
        _ => null,
    };

    public override IEnumerable<ISprite> GetSprites()
        => SpikeHelper.GetDustSprites(this, Direction(Name));

    public override ISelectionCollider GetMainSelection()
        => SpikeHelper.GetSelection(this, Direction(Name));

    public static FieldList GetFields(string sid) => new();

    public static PlacementList GetPlacements(string sid) {
        var prefix = Direction(sid) switch {
            SpikeHelper.Direction.Up => "up",
            SpikeHelper.Direction.Down => "down",
            SpikeHelper.Direction.Left => "left",
            _ => "right",
        };

        return new(prefix);
    }

    public static SpikeHelper.Direction Direction(string sid) => sid switch {
        "triggerSpikesDown" => SpikeHelper.Direction.Down,
        "triggerSpikesLeft" => SpikeHelper.Direction.Left,
        "triggerSpikesRight" => SpikeHelper.Direction.Right,
        _ => SpikeHelper.Direction.Up,
    };
}