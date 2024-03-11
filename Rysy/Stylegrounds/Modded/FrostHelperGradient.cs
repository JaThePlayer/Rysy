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
    
    public static FieldList GetFields() => new(new {
        gradient = new LinearGradientField(DefaultGradient).WithSeparator(';'),
        direction = LinearGradient.Directions.Vertical,
        blendMode = Fields.Dropdown("alphablend", ["alphablend", "additive", "subtract", "reversesubtract", "multiply"]),
    });

    public static PlacementList GetPlacements() => [];

    public override IEnumerable<ISprite> GetPreviewSprites() {
        yield return ISprite.LinearGradient(new(0, 0, 320, 180), Gradient, Direction);
    }

    public override IEnumerable<ISprite> GetSprites(StylegroundRenderCtx ctx) {
        yield return ISprite.LinearGradient(new(0, 0, (int)(320 * 6f / ctx.Camera.Scale), (int)(180 * 6f / ctx.Camera.Scale)), Gradient, Direction);
    }

    public override SpriteBatchState? GetSpriteBatchState() 
        => GFX.GetCurrentBatchState() with {
            BlendState = ParseBlendMode(Attr("blendMode", "alphablend")),
        };
    
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