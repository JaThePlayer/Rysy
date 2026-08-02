using Rysy.Graphics;

namespace Rysy.Stylegrounds;

public static class StylegroundRenderer {
    public enum Layers {
        Fg = 1,
        Bg = 2,
        BgAndFg = Bg | Fg,
    }

    public static readonly RasterizerState CullNoneWithScissor = new() {
        CullMode = CullMode.None,
        ScissorTestEnable = true,
        FillMode = FillMode.Solid
    };

    public static void Render(Room? room, MapStylegrounds styles, Camera camera, Layers layers, 
        IReadOnlyList<IStyleMaskManager> styleMaskManagers, Rectangle? scissorRectWorldPos = null, Colorgrade? colorgrade = null) {
        ArgumentNullException.ThrowIfNull(styles);
        float scale = camera.Scale;

        if (room is null)
            return;

        var ctx = new StylegroundRenderCtx(room, camera, Settings.Instance?.AnimateStylegrounds ?? false);

        var st = new SpriteBatchState(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, 
            DepthStencilState.None, RasterizerState.CullNone, colorgrade?.Set(), Matrix.CreateScale(scale));

        if (scissorRectWorldPos is { } worldPosScissor) {
            var screenPos = camera.RealToScreen(worldPosScissor.Location.ToVector2()).ToPoint();
            st.ScissorRect = new(screenPos.X, screenPos.Y, (int) (worldPosScissor.Width * scale), (int) (worldPosScissor.Height * scale));
            st.RasterizerState = CullNoneWithScissor;
        }

        Gfx.BeginBatch(st);

        var allStyles = (layers switch {
            Layers.Bg => styles.AllBackgroundStylesRecursive(),
            Layers.Fg => styles.AllForegroundStylesRecursive(),
            Layers.BgAndFg => styles.AllStylesRecursive(),
            _ => Array.Empty<Style>(),
        }).ToListIfNotList();

        foreach (var s in allStyles) {
            if (!s.Visible(ctx))
                continue;
            
            var masks = ctx.Room.Entities.OfType<IStyleMask>();
            bool renderedMasked = false;
            foreach (var m in masks) {
                if (m.RenderMasked(s, ctx)) {
                    renderedMasked = true;
                    break;
                }
            }

            if (renderedMasked)
                continue;

            // See if the style should get masked, but there wasn't any style mask entity in this room.
            foreach (var manager in styleMaskManagers) {
                if (manager.IsMasked(s, ctx)) {
                    renderedMasked = true;
                    break;
                }
            }
            
            if (renderedMasked)
                continue;
            
            RenderUnmasked(s, ctx);
        }

        foreach (var manager in styleMaskManagers) {
            manager.AfterRender(allStyles, ctx);
        }
        
        Gfx.EndBatch();
    }

    public static void RenderUnmasked(Style s, StylegroundRenderCtx ctx) {
        try {
            if (!s.Visible(ctx))
                return;
            
            var state = s.GetSpriteBatchState();
            var sprites = s.GetSprites(ctx);
            var renderCtx = SpriteRenderCtx.Default(ctx.Animate);

            if (state is null) {
                foreach (var sprite in sprites) {
                    sprite.Render(renderCtx);
                }
                return;
            }

            var lastState = Gfx.EndBatch();
            Gfx.BeginBatch(state);
            try {
                foreach (var sprite in sprites) {
                    sprite.Render(renderCtx);
                }
            } finally {
                Gfx.EndBatch();
                Gfx.BeginBatch(lastState);
            }
        } catch (Exception ex) {
            Logger.Error(ex, $"Failed to render styleground: {s}");
        }
    }
}

public record StylegroundRenderCtx(Room Room, Camera Camera, bool Animate) {
    public Rectangle FullScreenBounds => new(0, 0, ScreenWidth, ScreenHeight);
    public int ScreenWidth => (int) (320 * 6f / Camera.Scale);
    public int ScreenHeight => (int) (180 * 6f / Camera.Scale);
}

/// <summary>
/// Allows managing globally which styles should get masked away and not rendered.
/// </summary>
public interface IStyleMaskManager {
    /// <summary>
    /// If no <see cref="IStyleMask"/> took over rendering of the given style, this method gets called to check whether
    /// the style should be rendered at all.
    /// </summary>
    /// <param name="style">The styleground to check.</param>
    /// <param name="ctx">The context in which the styleground is about to be rendered.</param>
    /// <returns>Whether this style got masked away and should not get rendered.</returns>
    public bool IsMasked(Style style, StylegroundRenderCtx ctx);

    /// <summary>
    /// Called once after rendering of all styles finished, can be used for post-processing.
    /// </summary>
    public void AfterRender(IReadOnlyList<Style> allStyles, StylegroundRenderCtx ctx);
}

/// <summary>
/// When implemented on an entity, allows it change how stylegrounds get rendered.
/// </summary>
public interface IStyleMask {
    /// <summary>
    /// Handles rendering the styleground inside the mask.
    /// Return false if you do not wish to change this styleground's rendering.
    /// Use <see cref="StylegroundRenderer.RenderUnmasked"/> to help render the style correctly.
    /// </summary>
    /// <param name="style">The styleground to render.</param>
    /// <param name="ctx">The context in which the styleground is rendered.</param>
    /// <returns>Whether the style got rendered by this function. If false, default rendering will occur.</returns>
    public bool RenderMasked(Style style, StylegroundRenderCtx ctx);
}
