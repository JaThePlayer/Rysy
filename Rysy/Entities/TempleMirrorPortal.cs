using Rysy.Extensions;
using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("templeMirrorPortal")]
public sealed class TempleMirrorPortal : Entity, IPlaceable {
    public override int Depth => -1999;

    public override IEnumerable<ISprite> GetSprites() {
        var pos = Pos;

        yield return ISprite.FromTexture(pos, "objects/temple/portal/portalframe").Centered();
        yield return ISprite.FromTexture(pos, "objects/temple/portal/portalcurtain00").Centered();

        var torchSprite = ISprite.FromTexture("objects/temple/portal/portaltorch00").Centered() with {
            Origin = new(0.5f, 0.75f),
        };

        yield return torchSprite with {
            Pos = pos.AddX(90)
        };
        yield return torchSprite with {
            Pos = pos.AddX(-90)
        };
    }

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("temple_mirror_portal");
}