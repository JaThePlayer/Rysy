using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.DecalRegistryProperties;

[CustomEntity("lightOcclude")]
public sealed class LightOccludeDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
    public override bool AllowMultiple => true;

    public static FieldList GetFields() => new(new {
        x = 0,
        y = 0,
        width = 16,
        height = 16,
        alpha = 1f,
    });

    public static PlacementList GetPlacements() => new("default");

    public override IEnumerable<ISprite> GetSprites(VirtTexture texture, SpriteRenderCtx ctx) {
        yield return ISprite.FromTexture(texture).Centered();

        var center = new Vector2(Data.Float("x"), Data.Float("y"));

        yield return ISprite.OutlinedRect(center, Data.Int("width", 16), Data.Int("height", 16), Color.Transparent, Color.Red);
    }
}