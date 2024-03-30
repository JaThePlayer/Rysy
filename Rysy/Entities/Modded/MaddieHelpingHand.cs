using Rysy.Helpers;
using Rysy.LuaSupport;

namespace Rysy.Entities.Modded;

[CustomEntity("MaxHelpingHand/FlagRainbowSpinnerColorController", associatedMods: [ "MaxHelpingHand" ])]
[CustomEntity("MaxHelpingHand/RainbowSpinnerColorController", associatedMods: [ "MaxHelpingHand" ])]
[CustomEntity("MaxHelpingHand/FlagRainbowSpinnerColorAreaController", associatedMods: [ "MaxHelpingHand" ])]
[CustomEntity("MaxHelpingHand/RainbowSpinnerColorAreaController", associatedMods: [ "MaxHelpingHand" ])]
internal sealed class RainbowSpinnerColorController : LonnEntity, IRainbowSpinnerController, IPlaceable {
    [Bind("colors")]
    private ReadOnlyArray<Color> Colors;
    
    [Bind("gradientSize")]
    private float GradientSize;
    
    [Bind("loopColors")]
    private bool LoopColors;
    
    [Bind("centerX")]
    private float CenterX;
    
    [Bind("centerY")]
    private float CenterY;
    
    [Bind("gradientSpeed")]
    private float GradientSpeed;

    private Vector2 Center => new(CenterX, CenterY);
    
    private bool IsAreaController 
        => Name is "MaxHelpingHand/RainbowSpinnerColorAreaController" or "MaxHelpingHand/FlagRainbowSpinnerColorAreaController";
    
    public bool TryGetRainbowColor(Vector2 pos, float time, out Color res) {
        if (IsAreaController && !Rectangle.Contains(pos)) {
            res = default;
            return false;
        }
        
        res = GetModHue(Colors,GradientSize, pos, LoopColors, Center, GradientSpeed, time);
        return true;
    }

    public bool IsLocal => IsAreaController;

    // https://github.com/maddie480/MaddieHelpingHand/blob/db38d49ab9c2a1a031dff4733f806d77e1d1c869/Entities/RainbowSpinnerColorController.cs#L311
    private static Color GetModHue(IReadOnlyList<Color> colors, float gradientSize, Vector2 position, bool loopColors, Vector2 center, float gradientSpeed, float time) {
        if (colors.Count == 1) {
            // edge case: there is 1 color, just return it!
            return colors[0];
        }

        float progress = (position - center).Length() + time * gradientSpeed;
        while (progress < 0) {
            progress += gradientSize;
        }
        progress = progress % gradientSize / gradientSize;
        if (!loopColors) {
            progress = Easing.YoYo(progress);
        }

        if (progress == 1) {
            return colors[^1];
        }

        float globalProgress = (colors.Count - 1) * progress;
        int colorIndex = (int) globalProgress;
        float progressInIndex = globalProgress - colorIndex;
        
        return Color.Lerp(colors[colorIndex], colors[colorIndex + 1], progressInIndex);
    }

    
    // Define fields for the [Bind] attribute, but the lonn plugins are still used for actual placements
    public static FieldList GetFields() => new(new {
        colors = Fields.List("89E5AE,88E0E0,87A9DD,9887DB,D088E2",Fields.RGB(Color.White)),
        gradientSize = 280.0f,
        gradientSpeed = 50.0f,
        centerX = 0.0f,
        centerY = 0.0f,
        loopColors = false,
    });

    public static PlacementList GetPlacements() => [];
}