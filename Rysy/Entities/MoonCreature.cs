using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("moonCreature")]
public sealed class MoonCreature : SpriteEntity, IPlaceable {
    public override int Depth => -1000000;

    public override string TexturePath => "scenery/moon_creatures/tiny05";

    public static FieldList GetFields() => new(new {
        number = 1
    });

    public static PlacementList GetPlacements() => new("moon_creature");
}