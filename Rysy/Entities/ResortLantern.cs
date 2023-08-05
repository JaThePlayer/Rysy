using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Selections;

namespace Rysy.Entities;

[CustomEntity("resortLantern")]
public sealed class ResortLantern : Entity, IPlaceable {
    public override int Depth => 2000;

    public override IEnumerable<ISprite> GetSprites() {
        var pos = Pos;
        var connected = Room.IsSolidAt(pos.AddX(8));

        yield return ISprite.FromTexture(pos, "objects/resortLantern/holder").Centered() with {
            Scale = new(connected ? -1 : 1, 1),
        };
        yield return ISprite.FromTexture(pos, "objects/resortLantern/lantern00").Centered() with {
            Scale = new(connected ? -1 : 1, 1),
        };
    }

    public override ISelectionCollider GetMainSelection()
        => ISelectionCollider.FromSprites(GetSprites());

    public static FieldList GetFields() => new(new {

    });

    public static PlacementList GetPlacements() => new("lantern");
}