using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("cloud")]
public class Cloud : SpriteEntity, IPlaceable {
    public override string TexturePath => (Bool("fragile", false), Bool("small", false)) switch {
        (true, false) => "objects/clouds/fragile00",
        (true, true) => "objects/clouds/fragileRemix00",
        (false, false) => "objects/clouds/cloud00",
        (false, true) => "objects/clouds/cloudRemix00",
    };

    public override Vector2 Origin => new(.5f, .5f);

    public override int Depth => 0;

    public static FieldList GetFields() => new(new {
        fragile = false,
        small = false
    });

    public static PlacementList GetPlacements() => new() {
        new("normal"),
        new("fragile", new {
            fragile = true,
        }),
    };
}
