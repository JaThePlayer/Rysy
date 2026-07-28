#pragma warning disable CS0649

using Hexa.NET.ImGui;
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
        blendMode = Fields.Dropdown("alphablend", Parallax.BlendModes.Select(kv => kv.Key).ToArray()),
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
        => Gfx.GetCurrentBatchState() with {
            BlendState = ParseBlendMode(this.Attr("blendMode", "alphablend")),
        };

    public override void OnChanged(EntityDataChangeCtx ctx) {
        base.OnChanged(ctx);
        _cachedSprite = null;
        _cachedPreviewSprite = null;
    }

    private static BlendState ParseBlendMode(string mode) => Parallax.BlendModes.GetValueOrDefault(mode, BlendState.AlphaBlend);
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

    public override bool TryParse(string data, out LinearGradient.Entry value) {
        return LinearGradient.Entry.TryParse(data, null, out value);
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

        anyChanged |= ImGuiManager.ColorEditTranslated("FrostHelper.fields.gradient.from", ref data.ColorFrom, ColorFormat.Rgba, "FrostHelper.fields.gradient.from.tooltip");
        anyChanged |= ImGuiManager.ColorEditTranslated("FrostHelper.fields.gradient.to", ref data.ColorTo, ColorFormat.Rgba, "FrostHelper.fields.gradient.to.tooltip");
        anyChanged |= ImGui.InputFloat("FrostHelper.fields.gradient.percent".Translate(), ref data.Percent).WithTranslatedTooltip("FrostHelper.fields.gradient.percent.tooltip");

        return anyChanged;
    }
}