using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("player")]
public class Player : SpriteEntity, IPlaceable {
    public override int Depth => 0;
    public override string TexturePath => "characters/player/sitDown00";
    public override Vector2 Origin => new(.5f, 1f);

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("player");

    // vanilla maps have "width" defined on spawnpoints for some reason, breaking automatic selections...
    public override ISelectionCollider GetMainSelection() 
        => ISelectionCollider.FromSprite(GetSprite());
}
