using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("everest/npc")]
public sealed class Npc : SpriteEntity, IPlaceable {
    public override int Depth => 100;

    public override string TexturePath => $"characters/{Attr("sprite", "player/idle")}";

    public override Vector2 Origin => new(0.5f, 1.0f);

    public override Vector2 Scale => new(Bool("flipX") ? -1 : 1, Bool("flipY") ? -1 : 1);

    public static FieldList GetFields() => new(new {
        sprite = Fields.AtlasPath("player/idle", "^characters/(.*)00"),
        spriteRate = 1,
        dialogId = "",
        onlyOnce = true,
        endLevel = false,
        flipX = false,
        flipY = false,
        approachWhenTalking = false,
        approachDistance = 16,
        indicatorOffsetX = 0,
        indicatorOffsetY = 0
    });

    public static PlacementList GetPlacements() => new("npc");
}