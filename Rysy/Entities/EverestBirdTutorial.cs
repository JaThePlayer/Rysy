using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("everest/customBirdTutorial")]
public sealed class EverestBirdTutorial : SpriteEntity, IPlaceable {
    public override string TexturePath => "characters/bird/crow00";

    public override int Depth => -1000000;

    public override Vector2 Origin => new(0.5f, 1.0f);

    public override Range NodeLimits => 0..;

    public override Vector2 Scale => new(Bool("faceLeft") ? -1 : 1, 1);

    public static FieldList GetFields() => new(new {
        faceLeft = true,
        birdId = "",
        onlyOnce = false,
        caw = true,
        info = Fields.Dropdown("TUTORIAL_DREAMJUMP", CelesteEnums.BirdTutorials),
        controls = "DownRight,+,Dash,tinyarrow,Jump"
    });

    public static PlacementList GetPlacements() => new("bird");
}
