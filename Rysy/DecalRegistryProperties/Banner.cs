using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.DecalRegistryProperties;

[CustomEntity("banner")]
public sealed class BannerDecalRegistryProperty : DecalRegistryProperty, IPlaceable {
    public static FieldList GetFields() => new(new {
        speed = 1f,
        amplitude = 1f,
        sliceSize = Fields.Int(1).WithMin(1),
        sliceSinIncrement = 0.050f,
        easeDown = false,
        offset = 0f,
        onlyIfWindy = false,
    });

    public static PlacementList GetPlacements() => new("default");

    public override IEnumerable<ISprite> GetSprites(VirtTexture texture, SpriteRenderCtx ctx) {
        var sliceSize = Data.Int("sliceSize", 1).AtLeast(1);
        var easeDown = Data.Bool("easeDown", false);
        var waveSpeed = Data.Float("speed", 1f);
        var waveAmplitude = Data.Float("amplitude", 1f);
        var sliceSinIncrement = Data.Float("sliceSinIncrement", 1f);
        var offset = Data.Float("offset");

        var count = texture.Height / sliceSize;

        var baseSprite = ISprite.FromTexture(texture);
        baseSprite.Origin = new Vector2(0.5f, 1);

        var sineTimer = ctx.Time;
        
        for (int i = 0; i < texture.Height; i += sliceSize)
        {
            var spr = baseSprite.CreateSubtexture(0, i, texture.Width, sliceSize);
            
            float percent = easeDown ? i / (float)count : 1f - i / (float)count;
            
            float x = float.Sin(sineTimer * waveSpeed + i * sliceSinIncrement) * percent * waveAmplitude + percent * offset;

            spr.Pos += new Vector2(x.Floor(), i - texture.Height / 2f);

            yield return spr;
        }
    }
}