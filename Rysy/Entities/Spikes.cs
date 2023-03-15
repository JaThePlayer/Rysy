using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("spikesUp")]
public sealed class SpikesUp : Entity {
    public override int Depth => -1;

    public override IEnumerable<ISprite> GetSprites() => SpikeHelper.GetSprites(this, SpikeHelper.Direction.Up, Attr("type", "default"));

    public override Entity? TryFlipVertical() => CloneWith(pl => pl.SID = "spikesDown");

    public override ISelectionCollider GetMainSelection() => ISelectionCollider.RectCollider(X, Y - 8, Width, 8);
}

[CustomEntity("spikesDown")]
public sealed class SpikesDown : Entity {
    public override int Depth => -1;

    public override IEnumerable<ISprite> GetSprites() => SpikeHelper.GetSprites(this, SpikeHelper.Direction.Down, Attr("type", "default"));

    public override Entity? TryFlipVertical() => CloneWith(pl => pl.SID = "spikesUp");
}

[CustomEntity("spikesRight")]
public sealed class SpikesRight : Entity {
    public override int Depth => -1;

    public override IEnumerable<ISprite> GetSprites() => SpikeHelper.GetSprites(this, SpikeHelper.Direction.Right, Attr("type", "default"));

    public override Entity? TryFlipHorizontal() => CloneWith(pl => pl.SID = "spikesLeft");
}

[CustomEntity("spikesLeft")]
public sealed class SpikesLeft : Entity {
    public override int Depth => -1;

    public override IEnumerable<ISprite> GetSprites() => SpikeHelper.GetSprites(this, SpikeHelper.Direction.Left, Attr("type", "default"));

    public override Entity? TryFlipHorizontal() => CloneWith(pl => pl.SID = "spikesRight");

    public override ISelectionCollider GetMainSelection() => ISelectionCollider.RectCollider(X - 8, Y, 8, Height);
}