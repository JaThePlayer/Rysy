using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("dreammirror")]
public sealed class DreamMirror : Entity, IPlaceable {
    public override int Depth => 9000;

    public override IEnumerable<ISprite> GetSprites() {
        yield return ISprite.FromTexture(Pos, "objects/mirror/frame") with {
            Origin = new(0.5f, 1.0f),
            Depth = 9000,
        };

        yield return ISprite.FromTexture(Pos, "objects/mirror/glassbreak00") with {
            Origin = new(0.5f, 1.0f),
            Depth = 9500,
        };
    }

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("normal");
}
