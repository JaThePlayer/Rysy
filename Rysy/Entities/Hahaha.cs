using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Selections;

namespace Rysy.Entities;

[CustomEntity("hahaha")]
public sealed class Hahaha : Entity, IPlaceable {
    public override int Depth => -10001;

    public override IEnumerable<ISprite> GetSprites() {
        var pos = Pos;

        yield return ISprite.FromTexture(pos.Add(-11, -1), "characters/oldlady/ha00");
        yield return ISprite.FromTexture(pos, "characters/oldlady/ha00");
        yield return ISprite.FromTexture(pos.Add(11, -1), "characters/oldlady/ha00");
    }

    public override ISelectionCollider GetMainSelection()
        => ISelectionCollider.FromRect(Pos.Add(-11, -1), 33, 8);

    public static FieldList GetFields() => new(new {
        ifset = "",
        triggerLaughSfx = false
    });

    public static PlacementList GetPlacements() => new("hahaha");
}