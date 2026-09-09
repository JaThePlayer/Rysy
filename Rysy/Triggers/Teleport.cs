using Rysy.Gui.FieldTypes;
using Rysy.Helpers;

namespace Rysy.Triggers;

[CustomEntity("everest/teleportTrigger")]
public sealed class Teleport : Trigger, IPlaceable {
    public static FieldList GetFields() => new(new {
        nextLevel = Fields.RoomName(""),
        introType = CelesteEnums.IntroTypes.None,
        flag = "",
        onlyOnce = false,
        __sep = new PaddingField("triggers.everest/teleportTrigger.attributes.description.useDefaultSpawn.separator"),
        useDefaultSpawn = false,
        __pad = new PaddingField(DrawSeparator: false),
        nearestSpawnX = Fields.Float(0f).MakeDisabled(ctx => ctx.Bool("useDefaultSpawn")),
        nearestSpawnY = Fields.Float(0f).MakeDisabled(ctx => ctx.Bool("useDefaultSpawn")),
    });

    public static PlacementList GetPlacements() => new("default");
}
