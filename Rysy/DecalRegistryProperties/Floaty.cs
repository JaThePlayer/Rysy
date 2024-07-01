using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.DecalRegistryProperties;

[CustomEntity("floaty")]
public sealed class FloatyDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
    public static FieldList GetFields() => new(new {
    });

    public static PlacementList GetPlacements() => new("default");

    public override IEnumerable<ISprite> GetSprites(VirtTexture texture, SpriteRenderCtx ctx) {
        // base.Add(this.wave = new SineWave(Calc.Random.Range(0.1f, 0.4f), Calc.Random.NextFloat() * 6.28318548f));
        float counter = MathF.PI * 2f * 0.1f * ctx.Time;

        return ISprite.FromTexture(new Vector2(0, float.Sin(counter) * 4f), texture).Centered();
    }
}