#pragma warning disable CS0649

using Rysy.Graphics;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;
using System.Text.RegularExpressions;

namespace Rysy.Stylegrounds;

[CustomEntity("parallax")]
public sealed partial class Parallax : Style, IPlaceable {
    public override string DisplayName => Texture;

    [Bind("texture")]
    public string Texture;

    [Bind("alpha")]
    public float Alpha;

    [Bind("color")]
    public Color Color;

    [Bind("x")] private float _x;
    [Bind("y")] private float _y;
    [Bind("scrollx")] private float _scrollx;
    [Bind("scrolly")] private float _scrolly;
    [Bind("speedx")] private float _speedx;
    [Bind("speedy")] private float _speedy;
    [Bind("blendmode")] private string? _blendmodeStr;
    
    public Vector2 Pos => new(_x, _y);

    public Vector2 Scroll => new(_scrollx, _scrolly);

    public Vector2 Speed => new(_speedx, _speedy);

    public BlendState Blend => BlendModes.TryGetValue(_blendmodeStr ??= "alphablend", out var state)
        ? state.state : BlendState.AlphaBlend;

    [Bind("fadex")]
    private ReadOnlyArray<Fade.Region> _fadeXRegions;
    
    [Bind("fadey")]
    private ReadOnlyArray<Fade.Region> _fadeYRegions;
    
    public Fade FadeX => new(_fadeXRegions);

    public Fade FadeY => new(_fadeYRegions);

    [Bind("flipx")]
    public bool FlipX;
    
    [Bind("flipy")]
    public bool FlipY;

    [Bind("loopx")]
    public bool LoopX;
    
    [Bind("loopy")]
    public bool LoopY;

    [GeneratedRegex(@"^bgs/.*[^0-9]([^0-9]|0+)$")]
    private static partial Regex AnimationFrameIsZeroRegex();
    
    public static FieldList GetFields() => new(new {
        texture = Fields.AtlasPath("bgs/07/07/bg00", "^(bgs/.*)$")
            .WithFilter(x => {
                // Only show the first frame of animated parallax.
                if (x.Path.StartsWith("bgs/MaxHelpingHand/animatedParallax", StringComparison.Ordinal)
                    && !AnimationFrameIsZeroRegex().IsMatch(x.Path)) {
                    return false;
                }

                return true;
            }),
        blendmode = Fields.BlendModeDropdown("alphablend").AllowEdits(),
        alpha = 1f,
        color = Fields.Rgb(Color.White).AllowNull(),
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
            Default = ""
        },
        fadey = new FadeRegionField().ToList(':') with {
            MinElements = 0,
            Default = ""
        },
        instantIn = false,
        instantOut = false,
        loopx = true,
        loopy = true,
        fadeIn = false,
    });

    public static PlacementList GetPlacements() => new("parallax");

    private SpriteTemplate? _cachedBaseSprite;
    
    private SpriteTemplate? GetBaseSprite() {
        var texture = Texture;
        if (string.IsNullOrWhiteSpace(texture))
            return null;

        // Checks for texture, flip, etc are not needed as we clear the cache in OnChanged already.
        if (_cachedBaseSprite is { } cached) {
            return cached;
        }

        var flip = SpriteEffects.None;
        if (FlipX)
            flip |= SpriteEffects.FlipHorizontally;
        if (FlipY)
            flip |= SpriteEffects.FlipVertically;

        var template = SpriteTemplate.FromTexture(texture, 0);
        template.Flip = flip;
        
        return _cachedBaseSprite = template;
    }

    public override IEnumerable<ISprite> GetPreviewSprites() {
        var baseSprite = GetBaseSprite();

        return baseSprite?.Create(default, Color * Alpha) ?? [];
    }

    public override void OnChanged(EntityDataChangeCtx ctx) {
        base.OnChanged(ctx);
        _cachedBaseSprite = null;
    }

    internal static Vector2 CalcCamPos(Camera camera)
        => camera.ScreenToReal(Vector2.Zero).Floored();
    
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

        var baseSprite = GetBaseSprite();
        if (baseSprite is null || fade <= 0 || baseSprite.Texture.Texture is not {})
            return [];

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

        var x = (int) pos.X;
        var y = (int) pos.Y;
            
        var bounds = new Rectangle(x, y, 
            loopX ? ctx.ScreenWidth - x + texW : texW,
            loopY ? ctx.ScreenHeight - y + texH : texH);
        
        return baseSprite.CreateRepeating(bounds, Color * fade);
    }
    
    public override IReadOnlyList<string>? AssociatedMods
        => BlendModes.TryGetValue(_blendmodeStr ?? "alphablend", out var known) ? known.searchable.Mods : base.AssociatedMods;

    public override SpriteBatchState? GetSpriteBatchState() => Gfx.GetCurrentBatchState() with 
    { 
        SamplerState = SamplerState.PointWrap,
        BlendState = Blend,
    };

    private static readonly BlendState EeveeHelperMultiplyBlend = new()
    {
        ColorBlendFunction = BlendFunction.Add,
        ColorSourceBlend = Microsoft.Xna.Framework.Graphics.Blend.DestinationColor,
        ColorDestinationBlend = Microsoft.Xna.Framework.Graphics.Blend.Zero
    };

    private static readonly BlendState EeveeHelperReverseSubtractBlend = new()
    {
        ColorSourceBlend = Microsoft.Xna.Framework.Graphics.Blend.One,
        ColorDestinationBlend = Microsoft.Xna.Framework.Graphics.Blend.One,
        ColorBlendFunction = BlendFunction.Subtract,
        AlphaSourceBlend = Microsoft.Xna.Framework.Graphics.Blend.One,
        AlphaDestinationBlend = Microsoft.Xna.Framework.Graphics.Blend.One,
        AlphaBlendFunction = BlendFunction.Add
    };
    
    private static readonly BlendState EeveeHelperSubtractBlend = new()
    {
        ColorSourceBlend = Microsoft.Xna.Framework.Graphics.Blend.One,
        ColorDestinationBlend = Microsoft.Xna.Framework.Graphics.Blend.One,
        ColorBlendFunction = BlendFunction.ReverseSubtract,
        AlphaSourceBlend = Microsoft.Xna.Framework.Graphics.Blend.One,
        AlphaDestinationBlend = Microsoft.Xna.Framework.Graphics.Blend.One,
        AlphaBlendFunction = BlendFunction.Add
    };
    
    private static readonly ListenableDictionary<string, (BlendState state, Searchable searchable)> BlendModesMutable = new(StringComparer.OrdinalIgnoreCase) {
        ["alphablend"] = (BlendState.AlphaBlend, new Searchable("alphablend".TranslateOrHumanize("rysy.blendmodes"))),
        ["additive"] = (BlendState.Additive, new Searchable("additive".TranslateOrHumanize("rysy.blendmodes"))),
        // Eevee Helper
        ["multiply"] = (EeveeHelperMultiplyBlend, new Searchable("multiply".TranslateOrHumanize("rysy.blendmodes"), [ "EeveeHelper" ], null)),
        ["subtract"] = (EeveeHelperSubtractBlend, new Searchable("subtract".TranslateOrHumanize("rysy.blendmodes"), [ "EeveeHelper" ], null)),
        ["reversesubtract"] = (EeveeHelperReverseSubtractBlend, new Searchable("reversesubtract".TranslateOrHumanize("rysy.blendmodes"), [ "EeveeHelper" ], null)),
    };

    public static ReadOnlyListenableDictionary<string, (BlendState state, Searchable searchable)> BlendModes => BlendModesMutable;

    public static void RegisterBlendMode(string name, Searchable searchable, BlendState state) {
        BlendModesMutable[name] = (state, searchable);
    }
}
