using Rysy.Extensions;
using Rysy.Graphics.TextureTypes;
using Rysy.Selections;

namespace Rysy.Graphics;

public record struct RepeatingSprite : ISprite {
    public RepeatingSprite(SpriteTemplate template, Rectangle bounds, Color color) {
        Template = template;
        Bounds = bounds;
        Color = color;
    }
    
    public Rectangle Bounds { get; set; }
    
    public SpriteTemplate Template { get; set; }
    
    public int? Depth { get; set; }
    
    public Color Color { get; set; }

    public ISprite WithMultipliedAlpha(float alpha) => this with { Color = Color * alpha };

    public bool IsLoaded => Template.IsLoaded;
    
    public void Render(SpriteRenderCtx ctx) {
        if (Template.Texture is not { Texture: { } texture2d } vTexture) {
            return;
        }

        var bounds = Bounds;
        if (ctx.Camera is { } cam && !cam.IsRectVisible(bounds.MovedBy(ctx.CameraOffset)))
            return;

        // Modded, un-atlased textures can be rendered more efficiently if PointWrap is used.
        if (vTexture is ModTexture && Gfx.GetCurrentBatchState().SamplerState == SamplerState.PointWrap) {
            var clipRect = vTexture.ClipRect;

            clipRect.Width = bounds.Width;
            clipRect.Height = bounds.Height;
        
            Gfx.Batch.Draw(texture2d, bounds.Location.ToVector2(), clipRect, Color, Template.Rotation, Template.Origin, Template.Scale, Template.Flip, 0f);
            return;
        }

        
        var texW = vTexture.Width;
        var texH = vTexture.Height;
        
        for (float x = bounds.X; x < bounds.Right; x += texW) {
            for (float y = bounds.Y; y < bounds.Bottom; y += texH) {
                Template.RenderAt(ctx, new(x, y), Color, default);
            }
        }
    }

    public ISelectionCollider GetCollider()
        => ISelectionCollider.FromRect(Bounds);
}
