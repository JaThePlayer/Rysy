using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("darkChaser")]
public class BadelineChaser : SpriteEntity, IPlaceable {
    public override string TexturePath => "characters/badeline/sleep00";

    public override int Depth => 0;

    public override Vector2 Origin => new(.5f, 1f);

    public static FieldList GetFields() => new(new {
        canChangeMusic = true
    });

    public static PlacementList GetPlacements() => new("dark_chaser");
}
