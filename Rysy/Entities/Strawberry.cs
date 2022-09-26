using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("strawberry")]
[CustomEntity("goldenBerry")]
[CustomEntity("memorialTextController")]
public class Strawberry : SpriteEntity, INodeSpriteProvider
{
    public override string TexturePath => 
          Moon ? "collectables/moonBerry/normal00"
        : Golden ? $"collectables/goldberry/{(Winged ? "wings" : "idle")}00"
        : $"collectables/strawberry/{(Winged ? "wings" : "normal")}00";

    public override int Depth => Depths.Top;

    public bool Winged => Bool("winged") || EntityData.Name == "memorialTextController";
    public bool Golden => EntityData.Name == "memorialTextController" || EntityData.Name == "goldenBerry";
    public bool Moon => Bool("moon");

    public IEnumerable<ISprite> GetNodeSprites(int nodeIndex)
    {
        yield return GetSprite("collectables/strawberry/seed00") with
        {
            Pos = Nodes![nodeIndex]
        };
    }
}
