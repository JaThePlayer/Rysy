using Rysy.Helpers;
using BonfireModes = Rysy.Helpers.CelesteEnums.BonfireModes;

namespace Rysy.Entities;

[CustomEntity("bonfire")]
public class Bonfire : SpriteEntity, IPlaceable {
    public override int Depth => -5;

    public override string TexturePath => Enum("mode", BonfireModes.Lit) switch {
        BonfireModes.Lit => "objects/campfire/fire08",
        BonfireModes.Smoking => "objects/campfire/smoking04",
        _ => "objects/campfire/fire00",
    };


    public override Vector2 Origin => new(0.5f, 1.0f);

    public static FieldList GetFields() => new(new {
        mode = BonfireModes.Lit
    });

    public static PlacementList GetPlacements() => new("bonfire");
}
