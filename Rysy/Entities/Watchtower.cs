using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("towerviewer")]
public class Watchtower : SpriteEntity {
    public override string TexturePath => "objects/lookout/lookout05";

    public override Vector2 Origin => new(0.5f, 1.0f);

    public override int Depth => -8500;
}
