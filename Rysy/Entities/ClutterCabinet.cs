using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("clutterCabinet")]
public sealed class ClutterCabinet : SpriteEntity
{
    public override int Depth => -10001;
    public override string TexturePath => "objects/resortclutter/cabinet00";
    public override Vector2 Offset => new(8f);
}
