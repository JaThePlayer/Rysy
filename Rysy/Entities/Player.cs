using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("player")]
public class Player : SpriteEntity, IPlaceable {
    public override int Depth => 0;
    public override string TexturePath => "characters/player/sitDown00";
    public override Vector2 Origin => new(.5f, 1f);

    public static List<Placement>? GetPlacements() => new() {
        new Placement("Player (Spawn Point)").WithTooltip("Defines a spawnpoint for this room")
    };
}
