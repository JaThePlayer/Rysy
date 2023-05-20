using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("spikesUp")]
public sealed class SpikesUp : Entity, IPlaceable {
    public override int Depth => -1;
    public override bool ResizableX => true;

    public override IEnumerable<ISprite> GetSprites() => SpikeHelper.GetSprites(this, SpikeHelper.Direction.Up, Attr("type", "default"));

    public override Entity? TryFlipVertical() => CloneWith(pl => pl.SID = "spikesDown");

    public override ISelectionCollider GetMainSelection() => ISelectionCollider.FromRect(X, Y - 8, Width, 8);

    public static FieldList GetFields() => new(new {
        type = SpikeHelper.GetTypeField()
    });

    public static PlacementList GetPlacements() => SpikeHelper.CreatePlacements(t => $"up_{t}");
}

[CustomEntity("spikesDown")]
public sealed class SpikesDown : Entity, IPlaceable {
    public override int Depth => -1;
    public override bool ResizableX => true;

    public override IEnumerable<ISprite> GetSprites() => SpikeHelper.GetSprites(this, SpikeHelper.Direction.Down, Attr("type", "default"));

    public override Entity? TryFlipVertical() => CloneWith(pl => pl.SID = "spikesUp");


    public static FieldList GetFields() => new(new {
        type = SpikeHelper.GetTypeField()
    });

    public static PlacementList GetPlacements() => SpikeHelper.CreatePlacements(t => $"down_{t}");
}

[CustomEntity("spikesRight")]
public sealed class SpikesRight : Entity, IPlaceable {
    public override int Depth => -1;
    public override bool ResizableY => true;

    public override IEnumerable<ISprite> GetSprites() => SpikeHelper.GetSprites(this, SpikeHelper.Direction.Right, Attr("type", "default"));

    public override Entity? TryFlipHorizontal() => CloneWith(pl => pl.SID = "spikesLeft");

    public static FieldList GetFields() => new(new {
        type = SpikeHelper.GetTypeField()
    });

    public static PlacementList GetPlacements() => SpikeHelper.CreatePlacements(t => $"right_{t}");
}

[CustomEntity("spikesLeft")]
public sealed class SpikesLeft : Entity, IPlaceable {
    public override int Depth => -1;
    public override bool ResizableY => true;

    public override IEnumerable<ISprite> GetSprites() => SpikeHelper.GetSprites(this, SpikeHelper.Direction.Left, Attr("type", "default"));

    public override Entity? TryFlipHorizontal() => CloneWith(pl => pl.SID = "spikesRight");

    public override ISelectionCollider GetMainSelection() => ISelectionCollider.FromRect(X - 8, Y, 8, Height);

    public static FieldList GetFields() => new(new {
        type = SpikeHelper.GetTypeField()
    });

    public static PlacementList GetPlacements() => SpikeHelper.CreatePlacements(t => $"left_{t}");
}