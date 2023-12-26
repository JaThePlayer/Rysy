using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("bigWaterfall")]
public class BigWaterfall : RectangleEntity, IPlaceable {
    record Sprites(SpriteTemplate LeftFill, SpriteTemplate RightFill, SpriteTemplate LeftOutline, SpriteTemplate RightOutline);

    private static readonly Sprites FgSprites = new(
        SpriteTemplate.FromTexture("Rysy:bigWaterfallFillLeft", -49900),
        SpriteTemplate.FromTexture("Rysy:bigWaterfallFillRight", -49900),
        SpriteTemplate.FromTexture("Rysy:bigWaterfallOutlineLeft", -49900),
        SpriteTemplate.FromTexture("Rysy:bigWaterfallOutlineRight", -49900)
    );
    
    private static readonly Sprites BgSprites = new(
        SpriteTemplate.FromTexture("Rysy:bigWaterfallFillLeft", 10010),
        SpriteTemplate.FromTexture("Rysy:bigWaterfallFillRight", 10010),
        SpriteTemplate.FromTexture("Rysy:bigWaterfallOutlineLeft", 10010),
        SpriteTemplate.FromTexture("Rysy:bigWaterfallOutlineRight", 10010)
    );
    
    public override Color OutlineColor => Color.LightSkyBlue * 0.8f;
    public override Color FillColor => Color.LightSkyBlue * 0.3f;

    public Layers Layer => Enum("layer", Layers.BG);

    public override int Depth => GetDepth(Layer);

    public static FieldList GetFields() => new(new {
        layer = Fields.EnumNamesDropdown(Layers.FG)
    });

    public static PlacementList GetPlacements() => [
        new("foreground"),
        new("background", new { layer = Layers.BG })
    ];

    public override IEnumerable<ISprite> GetSprites() {
        return GetSprites(Pos, Width, Height, FillColor, OutlineColor, Layer);
    }
    
    public static int GetDepth(Layers layer) => layer switch {
        Layers.FG => -49900,
        _ => 10010,
    };

    public static IEnumerable<ISprite> GetSprites(Vector2 pos, int width, int h, Color fillColor, Color outlineColor, Layers layer) {
        var sprites = layer switch {
            Layers.FG => FgSprites,
            _ => BgSprites,
        };
        
        var th = sprites.LeftFill.Texture.Height;
        var tw = 8;

        yield return ISprite.Rect(pos.AddX(tw), width - tw*2, h, fillColor) with {
            Depth = GetDepth(layer),
        };
        
        for (int y = 0; y < h; y += th) {
            var innerPos = new Vector2(pos.X, pos.Y + y);
            var rightPos = innerPos.AddX(width - tw);
            
            if (y + th > h) {
                // Make sure we don't overshoot the height
                yield return sprites.LeftFill.CreateUntemplated(innerPos, fillColor).CreateSubtexture(0, 0, tw, h - y);
                yield return sprites.LeftOutline.CreateUntemplated(innerPos, outlineColor).CreateSubtexture(0, 0, tw, h - y);
                yield return sprites.RightFill.CreateUntemplated(rightPos, fillColor).CreateSubtexture(0, 0, tw, h - y);
                yield return sprites.RightOutline.CreateUntemplated(rightPos, outlineColor).CreateSubtexture(0, 0, tw, h - y);
                
                break;
            }
            
            yield return sprites.LeftFill.Create(innerPos, fillColor);
            yield return sprites.LeftOutline.Create(innerPos, outlineColor);
            
            yield return sprites.RightFill.Create(rightPos, fillColor);
            yield return sprites.RightOutline.Create(rightPos, outlineColor);
        }
    }

    public enum Layers {
        FG,
        BG
    }
}
