using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("spikesUp")]
public sealed class SpikesUp : Entity
{
    public override int Depth => -1;

    public override IEnumerable<ISprite> GetSprites() => SpikeHelper.GetSprites(this, SpikeHelper.Direction.Up, Attr("type", "default"));
}

[CustomEntity("spikesDown")]
public sealed class SpikesDown : Entity
{
    public override int Depth => -1;

    public override IEnumerable<ISprite> GetSprites() => SpikeHelper.GetSprites(this, SpikeHelper.Direction.Down, Attr("type", "default"));
}

[CustomEntity("spikesRight")]
public sealed class SpikesRight : Entity
{
    public override int Depth => -1;

    public override IEnumerable<ISprite> GetSprites() => SpikeHelper.GetSprites(this, SpikeHelper.Direction.Right, Attr("type", "default"));
}

[CustomEntity("spikesLeft")]
public sealed class SpikesLeft : Entity
{
    public override int Depth => -1;

    public override IEnumerable<ISprite> GetSprites() => SpikeHelper.GetSprites(this, SpikeHelper.Direction.Left, Attr("type", "default"));
}