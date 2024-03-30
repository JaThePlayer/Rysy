using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Graphics.TextureTypes;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;
using System.Runtime.InteropServices;

namespace Rysy.Stylegrounds;

[CustomEntity("parallax")]
public sealed class Parallax : Style, IPlaceable {
    public override string DisplayName => Texture;

    public string Texture => Attr("texture");

    public float Alpha => Float("alpha", 1f);

    public Color Color => GetColor("color", Color.White, Helpers.ColorFormat.RGB);

    public Vector2 Pos => new(Float("x", 0f), Float("y", 0f));

    public Vector2 Scroll => new(Float("scrollx", 0f), Float("scrolly", 0f));

    public Vector2 Speed => new(Float("speedx", 0f), Float("speedy", 0f));

    public BlendState Blend => BlendModes.TryGetValue(Attr("blendmode", "alphablend"), out var state) ? state : BlendState.AlphaBlend;

    [Bind("fadex")]
    private ReadOnlyArray<Fade.Region> _fadeXRegions;
    
    [Bind("fadey")]
    private ReadOnlyArray<Fade.Region> _fadeYRegions;
    
    public Fade FadeX => new(_fadeXRegions);

    public Fade FadeY => new(_fadeYRegions);

    public bool FlipX => Bool("flipx", false);
    public bool FlipY => Bool("flipy", false);

    public bool LoopX => Bool("loopx", true);
    public bool LoopY => Bool("loopy", true);

    public static FieldList GetFields() => new(new {
        texture = Fields.AtlasPath("bgs/07/07/bg00", "(bgs/.*)"),
        blendmode = Fields.Dropdown("alphablend", BlendModes.Select(kv => kv.Key).ToList(), editable: true),
        alpha = 1f,
        color = Fields.RGB(Color.White).AllowNull(),
        scrollx = 0f,
        scrolly = 0f,
        speedx = 0f,
        speedy = 0f,
        x = 0f,
        y = 0f,
        flipx = false,
        flipy = false,
        fadex = new FadeRegionField().ToList(':') with {
            MinElements = 0,
        },
        fadey = new FadeRegionField().ToList(':') with {
            MinElements = 0,
        },
        instantIn = false,
        instantOut = false,
        loopx = true,
        loopy = true,
        fadeIn = false,
    });

    public static PlacementList GetPlacements() => new("parallax");

    private ColoredSpriteTemplate? _cachedBaseSprite;
    
    private ColoredSpriteTemplate? GetBaseSprite(float alpha) {
        var texture = Texture;
        if (string.IsNullOrWhiteSpace(texture))
            return null;

        var color = Color * alpha;
        var targetTexture = GFX.Atlas[texture];
        var scale = new Vector2(FlipX ? -1f : 1f, FlipY ? -1f : 1f);

        if (_cachedBaseSprite is { } cached && cached.Template.Texture == targetTexture) {
            cached.Color = color;
            cached.Template.Scale = scale;
            return cached;
        }

        var template = SpriteTemplate.FromTexture(targetTexture, 0);
        template.Scale = scale;
        
        return _cachedBaseSprite = template.CreateColoredTemplate(color);
    }

    public override IEnumerable<ISprite> GetPreviewSprites() {
        var baseSprite = GetBaseSprite(Alpha);

        return baseSprite?.Create(default) ?? [];
    }

    internal static Vector2 CalcCamPos(Camera camera)
        => camera.ScreenToReal(Vector2.Zero).Floored() + new Vector2(0f, 0f);

    // Pre-allocated buffer of struct sprites, allows avoiding big memory allocations from re-rendering parallax each frame
    private List<ColorTemplatedSprite>? _cachedVanillaSprites;
    
    public override IEnumerable<ISprite> GetSprites(StylegroundRenderCtx ctx) {
        if (ctx.Camera.Scale < 1f / 2f)
            return []; // very tiny stylegrounds cause a lot of lag with vanilla textures, AND they look bad

        if (string.IsNullOrWhiteSpace(Texture))
            return [];

        var camPos = CalcCamPos(ctx.Camera);
        var pos = Pos - camPos * Scroll;

        if (ctx.Animate)
            pos += Time.Elapsed * Speed;

        pos = pos.Floored();

        var fade = Alpha;
        fade *= FadeX.GetValueAt(camPos.X + 160f);
        fade *= FadeY.GetValueAt(camPos.Y + 90f);
        fade = fade.AtMost(1f);

        var baseSprite = GetBaseSprite(fade);
        if (baseSprite is null || baseSprite.Color.A <= 1)
            return [];

        var loopX = LoopX;
        var loopY = LoopY;

        var texW = baseSprite.Template.Texture.Width;
        var texH = baseSprite.Template.Texture.Height;
        
        if (loopX) {
            pos.X = (pos.X % texW - texW) % texW;
        }
        if (loopY) {
            pos.Y = (pos.Y % texH - texH) % texH;
        }
        
        // todo: extract into LoopingSprite
        if (baseSprite.Template.Texture is VanillaTexture vanilla) {
            var maxX = 320 * 6f / ctx.Camera.Scale;
            var maxY = 180 * 6f / ctx.Camera.Scale;

            var spriteCount = (loopX ? (int) ((maxX - pos.X + texW) / texW + 1) : 1) *
                              (loopY ? (int) ((maxY - pos.Y + texH) / texH + 1) : 1);
            var spritesList = _cachedVanillaSprites ??= new(spriteCount);
            spritesList.Clear();
            if (spriteCount >= spritesList.Count) {
                CollectionsMarshal.SetCount(spritesList, spriteCount);
            }
            
            var i = 0;
            var sprites = CollectionsMarshal.AsSpan(spritesList);
            for (float x = pos.X; x <= maxX + texW; x += texW) {
                for (float y = pos.Y; y <= maxY + texH; y += texH) {
                    sprites[i++] = baseSprite.Create(new(x, y));
                    if (!loopY) {
                        break;
                    }
                }
                if (!loopX) {
                    break;
                }
            }
            
            return spritesList.Take(i).Cast<ISprite>();
        }

        var texture = baseSprite.Create(new Vector2(pos.X, pos.Y));
        return new FunctionSprite<(ColorTemplatedSprite, Parallax, Camera)>((texture, this, ctx.Camera), static (data, t) => {
            var texture = data.Item1;
            var self = data.Item2;
            var textVirt = texture.Template.Template.Texture;

            if (textVirt.Texture is not { } tx) {
                return;
            }
            var clipRect = textVirt.ClipRect;

            if (self.LoopX)
                clipRect.Width = (int)(data.Item3.Viewport.Width / data.Item3.Scale - texture.Pos.X);//(int)(320 * 6f / data.Item3.Scale) + 640;
            if (self.LoopY)
                clipRect.Height = (int) (data.Item3.Viewport.Height / data.Item3.Scale - texture.Pos.Y);//(int) (180 * 6f / data.Item3.Scale) + 320;

            var flip = SpriteEffects.None;
            if (self.FlipX) {
                flip |= SpriteEffects.FlipHorizontally;
            }
            if (self.FlipY) {
                flip |= SpriteEffects.FlipVertically;
            }

            GFX.Batch.Draw(tx, texture.Pos, clipRect, t.Color, texture.Template.Template.Rotation, texture.Template.Template.Origin, 1f, flip, 0f);
        }) with {
            Color = baseSprite.Color,
        };
    }

    public override SpriteBatchState? GetSpriteBatchState() => GFX.GetCurrentBatchState() with 
    { 
        SamplerState = SamplerState.PointWrap,
        BlendState = Blend,
    };

    private static ListenableDictionary<string, BlendState> _BlendModes = new(StringComparer.OrdinalIgnoreCase) {
        ["alphablend"] = BlendState.AlphaBlend,
        ["additive"] = BlendState.Additive,
    };

    public static ReadOnlyListenableDictionary<string, BlendState> BlendModes => _BlendModes;

    public static void RegisterBlendMode(string name, BlendState state) {
        _BlendModes[name] = state;
    }
}
