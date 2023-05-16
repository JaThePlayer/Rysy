using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("lightningBlock")]
public sealed class LightningBreakerBox : SpriteEntity, IPlaceable {
    public override int Depth => -10550;

    public override string TexturePath => "objects/breakerBox/Idle00";

    private bool FlipX => Bool("flipX");

    public override Vector2 Scale => new(FlipX ? -1 : 1, 1);

    public override Vector2 Origin => new(FlipX ? 0.75f : 0.25f, 0.25f);

    public static FieldList GetFields() => new(new {
        flipX = false,
        music_progress = -1,
        music_session = false,
        music = "",
        flag = false
    });

    public static PlacementList GetPlacements() => new("breaker_box");
}