using Rysy.Graphics;

namespace Rysy.Stylegrounds;

[CustomEntity("stars")]
public sealed class Stars : Style, IPlaceable {
    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("default");

    public override IEnumerable<ISprite> GetPreviewSprites()
        => ISprite.Rect(PreviewRectangle(), Color.Black);

    public override IEnumerable<ISprite> GetSprites(StylegroundRenderCtx ctx)
        => ISprite.Rect(ctx.FullScreenBounds(), Color.Black);
}