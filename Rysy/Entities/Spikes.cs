using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("spikesUp")]
public class SpikesUp : LoopingSpriteEntity
{
    public override string TexturePath => $"danger/spikes/{Attr("type", "default")}_up00";

    public override int Depth => -1;

    public override Vector2 Origin => new(0.5f, 1f);

    public override Vector2 Offset => new(4f, 1f);

    public override int? SpriteSpacingOverride => 8;
}

[CustomEntity("spikesDown")]
public class SpikesDown : LoopingSpriteEntity
{
    public override string TexturePath => $"danger/spikes/{Attr("type", "default")}_down00";

    public override int Depth => -1;

    public override Vector2 Origin => new(0.5f, 0f);

    public override Vector2 Offset => new(4f, -1f);

    public override int? SpriteSpacingOverride => 8;
}

[CustomEntity("spikesRight")]
public class SpikesRight : LoopingSpriteEntity
{
    public override string TexturePath => $"danger/spikes/{Attr("type", "default")}_right00";

    public override int Depth => -1;

    public override Vector2 Origin => new(0f, 0.5f);

    public override Vector2 Offset => new(-1f, 4f);

    public override int? SpriteSpacingOverride => 8;
}

[CustomEntity("spikesLeft")]
public class SpikesLeft : LoopingSpriteEntity
{
    public override string TexturePath => $"danger/spikes/{Attr("type", "default")}_left00";

    public override int Depth => -1;

    public override Vector2 Origin => new(1f, 0.5f);

    public override Vector2 Offset => new(1f, 4f);

    public override int? SpriteSpacingOverride => 8;
}