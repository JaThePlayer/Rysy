using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("playbackTutorial")]
public sealed class PlayerPlayback : SpriteEntity, IPlaceable {
    public override int Depth => Depths.Above;
    public override string TexturePath => "characters/player/sitDown00";
    public override Vector2 Origin => new(.5f, 1f);

    public override Color Color => Color.Red;

    public override Range NodeLimits => 0..2;

    public override IEnumerable<ISprite> GetSprites() {
        return PlaybackRegistry.GetSprites(Pos, Attr("tutorial", "wavedash")).Prepend(GetSprite());
    }

    public override IEnumerable<ISprite> GetNodeSprites(int nodeIndex) {
        yield return ISprite.OutlinedRect(Nodes[nodeIndex].Pos.Add(-4, -4), 8, 8, Color.AliceBlue * 0.3f, Color.AliceBlue);
    }

    public static FieldList GetFields() => new(new {
        tutorial = Fields.Path("wavedash", "Tutorials", "bin").AllowEdits(),
    });

    public static PlacementList GetPlacements() => new("playback");
}

