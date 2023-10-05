using Rysy;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("memorial")]
[CustomEntity("everest/memorial")]
public sealed class Memorial : SpriteEntity, IPlaceable {
    private const string DefaultTexture = "scenery/memorial/memorial";

    public override int Depth => 100;

    public override string TexturePath => Attr("sprite", DefaultTexture);

    public override Vector2 Origin => new(0.5f, 1f);

    public static FieldList GetFields() => new(new {
        dialog = "MEMORIAL",
        sprite = DefaultTexture,
        spacing = 16
    });

    public static PlacementList GetPlacements() => new() {
        new Placement("memorial").WithSID("everest/memorial")
    };
}