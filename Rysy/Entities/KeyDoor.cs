using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("lockBlock")]
public sealed class KeyDoor : SpriteBankEntity, IPlaceable {
    public override int Depth => Depths.Solids;

    public override string SpriteBankEntry => $"lockdoor_{Attr("sprite", "wood")}";

    public override string Animation => "idle";

    public override Vector2 Offset => new(16f);

    public static FieldList GetFields() => new(new {
        sprite = Fields.SpriteBankPath("wood", "^lockdoor_(.*)"),
        unlock_sfx = "",
        stepMusicProgress = false
    });

    public static PlacementList GetPlacements() => new("wood");
}
