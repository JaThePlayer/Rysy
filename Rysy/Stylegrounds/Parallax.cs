using Microsoft.Xna.Framework.Graphics;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Graphics.TextureTypes;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;

namespace Rysy.Stylegrounds;

[CustomEntity("parallax")]
public sealed class Parallax : Style, IPlaceable {
    public override string DisplayName => Texture;

    public string Texture => Data.Attr("texture");

    public float Alpha => Data.Float("alpha", 1f);

    public Color Color => Data.GetColor("color", Color.White, Helpers.ColorFormat.RGB);

    public Vector2 Pos => new(Data.Float("x", 0f), Data.Float("y", 0f));

    public Vector2 Scroll => new(Data.Float("scrollx", 0f), Data.Float("scrolly", 0f));

    public Vector2 Speed => new(Data.Float("speedx", 0f), Data.Float("speedy", 0f));

    public BlendState Blend => BlendModes.TryGetValue(Data.Attr("blendmode", "alphablend"), out var state) ? state : BlendState.AlphaBlend;

    public Fade FadeX => new(Data.Attr("fadex"));

    public Fade FadeY => new(Data.Attr("fadey"));

    public bool FlipX => Data.Bool("flipx", false);
    public bool FlipY => Data.Bool("flipy", false);

    public bool LoopX => Data.Bool("loopx", true);
    public bool LoopY => Data.Bool("loopy", true);

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
        fadex = new FadeField().ToList(':') with {
            MinElements = 0,
        },
        fadey = new FadeField().ToList(':') with {
            MinElements = 0,
        },
        instantIn = false,
        instantOut = false,
        loopx = true,
        loopy = true,
        fadeIn = false,
    });

    public static PlacementList GetPlacements() => new("parallax");

    private Sprite? GetBaseSprite() => string.IsNullOrWhiteSpace(Texture) ? null : ISprite.FromTexture(Texture) with {
        Color = Color * Alpha,
    };

    public override IEnumerable<ISprite> GetPreviewSprites() {
        var baseSprite = GetBaseSprite();

        if (baseSprite is null) {
            yield break;
        }

        yield return baseSprite;
    }

    internal static Vector2 CalcCamPos(Camera camera)
        => camera.ScreenToReal(Vector2.Zero).Floored() + new Vector2(0f, 0f);

    public override IEnumerable<ISprite> GetSprites(StylegroundRenderCtx ctx) {
        if (ctx.Camera.Scale < 1f / 2f)
            yield break; // very tiny stylegrounds cause a lot of lag with vanilla textures, AND they look bad

        if (GetBaseSprite() is not { } baseSprite || baseSprite.Texture.Texture is not { }) {
            yield break;
        }
        if (baseSprite.Texture == GFX.UnknownTexture)
            yield break;

        var camPos = CalcCamPos(ctx.Camera);
        var pos = (Pos - camPos * Scroll).Floored();

        if (ctx.Animate)
            pos += Time.Elapsed * Speed;

        var fade = Alpha;
        fade *= FadeX.GetValueAt(camPos.X + 160f);
        fade *= FadeY.GetValueAt(camPos.Y + 90f);

        var color = Color;
        if (fade < 1f)
            color *= fade;

        if (color.A <= 1)
            yield break;

        var loopX = LoopX;
        var loopY = LoopY;

        var texW = baseSprite.Texture.Width;
        var texH = baseSprite.Texture.Height;
        
        if (loopX) {
            pos.X = (pos.X % texW - texW) % texW;
        }
        if (loopY) {
            pos.Y = (pos.Y % texH - texH) % texH;
        }
        if (FlipX) {
            baseSprite.Scale.X = -1f;
        }
        if (FlipY) {
            baseSprite.Scale.Y = -1f;
        }
        // todo: extract into LoopingSprite
        if (baseSprite.Texture is VanillaTexture vanilla) {
            var maxX = 320 * 6f / ctx.Camera.Scale;
            var maxY = 180 * 6f / ctx.Camera.Scale;

            for (float x = pos.X; x <= maxX + texW; x += texW) {
                for (float y = pos.Y; y <= maxY + texH; y += texH) {
                    yield return baseSprite with {
                        Pos = new Vector2(x, y),
                        Origin = default,
                        Color = color,
                    };
                    if (!loopY) {
                        break;
                    }
                }
                if (!loopX) {
                    break;
                }
            }
            yield break;
        }

        var texture = baseSprite with {
            Pos = new Vector2(pos.X, pos.Y),
            Origin = default,
            Color = color,
        };
        yield return new FunctionSprite<(Sprite, Parallax, Camera)>((texture, this, ctx.Camera), static (data, t) => {
            var texture = data.Item1;
            var self = data.Item2;

            if (texture.Texture.Texture is not { } tx) {
                return;
            }
            var clipRect = texture.Texture.ClipRect;

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

            GFX.Batch.Draw(tx, texture.Pos, clipRect, t.Color, texture.Rotation, texture.Origin, 1f, flip, 0f);
        }) with {
            Color = color,
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
