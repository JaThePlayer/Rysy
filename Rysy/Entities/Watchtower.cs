using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("towerviewer")]
public class Watchtower : SpriteEntity, IPlaceable {
    public override string TexturePath => "objects/lookout/lookout05";

    public override Vector2 Origin => new(0.5f, 1.0f);

    public override int Depth => -8500;

    public override Range NodeLimits => 0..;

    public static FieldList GetFields() => new(new {
        summit = false,
        onlyY = false
    });

    public static PlacementList GetPlacements() => [
        new Placement("watchtower") {
            AlternativeNames = [
                "lookout",
                "binoculars",
            ]
        }
    ];
}
