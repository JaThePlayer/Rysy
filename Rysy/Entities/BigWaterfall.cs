using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("bigWaterfall")]
public class BigWaterfall : Entity {
    public virtual Color SurfaceColor => Color.LightSkyBlue * 0.8f;
    public virtual Color FillColor => Color.LightSkyBlue * 0.3f;

    public override int Depth => Enum("layer", Layers.BG) switch {
        Layers.FG => -49900,
        Layers.BG or _ => 10010,
    };


    public override IEnumerable<ISprite> GetSprites() {
        yield return ISprite.OutlinedRect(Pos, Width, Height, FillColor, SurfaceColor);
    }


    public enum Layers {
        FG,
        BG
    }
}
