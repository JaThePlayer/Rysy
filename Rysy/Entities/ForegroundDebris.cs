using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Selections;

namespace Rysy.Entities;

[CustomEntity("foregroundDebris")]
internal sealed class ForegroundDebris : Entity, IPlaceable {
    public override int Depth => -999900;

    public override IEnumerable<ISprite> GetSprites() {
        var path = Pos.SeededRandomInclusive(0, 1) switch {
            0 => "scenery/fgdebris/rock_a",
            _ => "scenery/fgdebris/rock_b"
        };

        foreach (var texture in GFX.Atlas.GetSubtextures(path)) {
            yield return ISprite.FromTexture(Pos, texture).Centered();
        }
    }

    public override ISelectionCollider GetMainSelection()
        => ISelectionCollider.FromRect(Pos - new Vector2(24), 48, 48);

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("foreground_debris");
}
