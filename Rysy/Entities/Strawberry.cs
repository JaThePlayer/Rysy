using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("strawberry")]
[CustomEntity("goldenBerry")]
[CustomEntity("memorialTextController")]
public class Strawberry : SpriteEntity {
    public override string TexturePath =>
          Moon ? "collectables/moonBerry/normal00"
        : Golden ? $"collectables/goldberry/{(Winged ? "wings" : "idle")}00"
        : $"collectables/strawberry/{(Winged ? "wings" : "normal")}00";

    public override int Depth => Depths.Top;

    public bool Winged => Bool("winged") || EntityData.SID == "memorialTextController";
    public bool Golden => EntityData.SID is "memorialTextController" or "goldenBerry";
    public bool Moon => Bool("moon");

    public override IEnumerable<ISprite> GetNodePathSprites() => NodePathTypes.Fan(this);

    public override IEnumerable<ISprite> GetNodeSprites(int nodeIndex) {
        yield return GetSprite("collectables/strawberry/seed00") with {
            Pos = Nodes![nodeIndex]
        };
    }

    public override Range NodeLimits => 0..;
}
