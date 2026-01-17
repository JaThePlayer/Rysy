using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Selections;

namespace Rysy.Entities;

[CustomEntity("resortLantern")]
public sealed class ResortLantern : Entity, IPlaceable {
    public override int Depth => 2000;

    public override IEnumerable<ISprite> GetSprites() {
        var pos = Pos;
        // TODO: correct this, needs a IsSolidAt(Rectangle) - new Hitbox(8f, 8f, -4f, -4f), base.CollideCheck<Solid>(this.Position + Vector2.UnitX * 8f)
        var connected = 
            Room.IsSolidAt(pos.AddX(8).AddY(-4))
            || Room.IsSolidAt(pos.AddX(8).AddY(0));

        yield return ISprite.FromTexture(pos, "objects/resortLantern/holder").Centered() with {
            Scale = new(connected ? -1 : 1, 1),
        };
        yield return ISprite.FromTexture(pos.AddX(connected ? 2 : 0), "objects/resortLantern/lantern00").Centered() with {
            Scale = new(connected ? -1 : 1, 1),
        };
    }

    public override ISelectionCollider GetMainSelection()
        => ISelectionCollider.FromSprites(GetSprites());

    public static FieldList GetFields() => new(new {

    });

    public static PlacementList GetPlacements() => new("lantern");
}