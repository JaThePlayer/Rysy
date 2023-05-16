using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("coreMessage")]
public class CoreMessage : Entity {
    public override int Depth => 0;
}

[CustomEntity("everest/coreMessage")]
public class EverestCoreMessage : SpriteEntity, IPlaceable {
    public override int Depth => 0;

    public override string TexturePath => "Rysy:core_message";

    public static FieldList GetFields() => new(new {
        line = 0,
        dialog = "app_ending",
        outline = false
    });

    public static PlacementList GetPlacements() => new("core_message");
}
