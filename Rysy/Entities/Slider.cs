using Rysy.Graphics;
using static Rysy.Helpers.CelesteEnums;

namespace Rysy.Entities;

[CustomEntity("slider")]
public sealed class Slider : Entity, IPlaceable {
    public override int Depth => 0;

    public override IEnumerable<ISprite> GetSprites()
        => ISprite.Circle(Pos, 12, Color.Red, 8);

    public static FieldList GetFields() => new(new {
        clockwise = true,
        surface = SliderSurfaces.Floor,
    });

    public static PlacementList GetPlacements() => new("clockwise");
}