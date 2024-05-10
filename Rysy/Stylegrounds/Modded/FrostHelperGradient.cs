#pragma warning disable CS0649

using ImGuiNET;
using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;

namespace Rysy.Stylegrounds.Modded;

[CustomEntity("FrostHelper/Gradient", associatedMods: [ "FrostHelper" ])]
internal sealed class FrostHelperGradient : Style, IPlaceable {
    private const string DefaultGradient = "ffffff,ffffff,100";
    
    [Bind("gradient")]
    public LinearGradient Gradient;

    [Bind("direction")] 
    public LinearGradient.Directions Direction;

    [Bind("loopX")]
    public bool LoopX;
    
    [Bind("loopY")]
    public bool LoopY;
    
    // LinearGradientSprites are expensive to create, and GetSprites gets called each frame the styleground is visible.
    private LinearGradientSprite? _cachedSprite;
    private LinearGradientSprite? _cachedPreviewSprite;
    
    public static FieldList GetFields() => new(new {
        gradient = new LinearGradientField(DefaultGradient).WithSeparator(';'),
        direction = LinearGradient.Directions.Vertical,
        blendMode = Fields.Dropdown("alphablend", ["alphablend", "additive", "subtract", "reversesubtract", "multiply"]),
        loopX = false,
        loopY = false,
    });

    public static PlacementList GetPlacements() => [];

    public override IEnumerable<ISprite> GetPreviewSprites() {
        return _cachedPreviewSprite ??= ISprite.LinearGradient(PreviewRectangle(), Gradient, Direction, LoopX, LoopY);
    }
    
    public override IEnumerable<ISprite> GetSprites(StylegroundRenderCtx ctx) {
        var bounds = ctx.FullScreenBounds;
        
        // if zoom level changed, clear the cache. OnChanged handles properties being changed already.
        if (_cachedSprite is { } && _cachedSprite.Bounds != bounds) {
            _cachedSprite = null;
        }
        
        return _cachedSprite ??= ISprite.LinearGradient(bounds, Gradient, Direction, LoopX, LoopY);
    }

    public override SpriteBatchState? GetSpriteBatchState() 
        => GFX.GetCurrentBatchState() with {
            BlendState = ParseBlendMode(this.Attr("blendMode", "alphablend")),
        };

    public override void OnChanged(EntityDataChangeCtx ctx) {
        base.OnChanged(ctx);
        _cachedSprite = null;
        _cachedPreviewSprite = null;
    }

    private static BlendState ParseBlendMode(string mode) => mode switch {
        "alphablend" => BlendState.AlphaBlend,
        "additive" => BlendState.Additive,
        "subtract" => GfxSubtract,
        "reversesubtract" => EeveeHelperReverseSubtract,
        "multiply" => EeveeHelperMultiply,
        _ => BlendState.AlphaBlend
    };
    
    private static readonly BlendState EeveeHelperReverseSubtract = new()
    {
        ColorSourceBlend = Blend.One,
        ColorDestinationBlend = Blend.One,
        ColorBlendFunction = BlendFunction.Subtract,
        AlphaSourceBlend = Blend.One,
        AlphaDestinationBlend = Blend.One,
        AlphaBlendFunction = BlendFunction.Add
    };

    private static readonly BlendState EeveeHelperMultiply = new()
    {
        ColorBlendFunction = BlendFunction.Add,
        ColorSourceBlend = Blend.DestinationColor,
        ColorDestinationBlend = Blend.Zero
    };

    private static readonly BlendState GfxSubtract = new()
    {
        ColorSourceBlend = Blend.One,
        ColorDestinationBlend = Blend.One,
        ColorBlendFunction = BlendFunction.ReverseSubtract,
        AlphaSourceBlend = Blend.One,
        AlphaDestinationBlend = Blend.One,
        AlphaBlendFunction = BlendFunction.Add
    };
}

sealed record LinearGradientField : ListField, IFieldConvertible<LinearGradient> {
    public LinearGradientField(string @default) : base(new GradientEntryField(@default), @default)
    {
    }

    public LinearGradient ConvertMapDataValue(object value) {
        if (!LinearGradient.TryParse(value.ToString(), null, out var entry)) {
            entry = LinearGradient.ErrorGradient;
        }

        return entry;
    }
}

sealed record GradientEntryField : ComplexTypeField<LinearGradient.Entry> {
    public GradientEntryField(string def) {
        Default = Parse(def);
    }
    
    public override LinearGradient.Entry Parse(string data) {
        if (!LinearGradient.Entry.TryParse(data, null, out var entry)) {
            entry = new() {
                Percent = 100,
                ColorFrom = Color.Red * 0.3f,
                ColorTo = Color.Red * 0.3f,
            };
        }

        return entry;
    }

    public override string ConvertToString(LinearGradient.Entry data) {
        return data.ToString();
    }

    public override bool RenderDetailedWindow(ref LinearGradient.Entry data) {
        bool anyChanged = false;

        anyChanged |= ImGuiManager.ColorEdit("From", ref data.ColorFrom, ColorFormat.RGBA, "The color at the start of this segment");
        anyChanged |= ImGuiManager.ColorEdit("To", ref data.ColorTo, ColorFormat.RGBA, "The color at the end of this segment");
        anyChanged |= ImGui.InputFloat("Percent", ref data.Percent).WithTooltip("How much of the screen this entry takes up");

        return anyChanged;
    }
}