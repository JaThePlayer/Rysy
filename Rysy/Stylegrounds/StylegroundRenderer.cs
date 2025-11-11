using Rysy.Graphics;

namespace Rysy.Stylegrounds;

public static class StylegroundRenderer {
    public enum Layers {
        Fg = 1,
        Bg = 2,
        BgAndFg = Bg | Fg,
    }

    private static readonly RasterizerState CullNoneWithScissor = new() {
        CullMode = CullMode.None,
        ScissorTestEnable = true,
        FillMode = FillMode.Solid
    };

    public static bool NotMasked(Style style) {
        return !style.IsMasked();
    }

    public static void Render(Room? room, MapStylegrounds styles, Camera camera, Layers layers, 
        Func<Style, bool> filter, Rectangle? scissorRectWorldPos = null, Colorgrade? colorgrade = null) {
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

        var allStyles = layers switch {
            Layers.Bg => styles.AllBackgroundStylesRecursive(),
            Layers.Fg => styles.AllForegroundStylesRecursive(),
            Layers.BgAndFg => styles.AllStylesRecursive(),
            _ => Array.Empty<Style>(),
        };

        foreach (var s in allStyles) {
            if (filter(s))
                Render(s, ctx);
        }

        //ISprite.OutlinedRect(new(0,0, 320, 180), Color.Transparent, Color.Red).Render();

        Gfx.EndBatch();
    }

    private static void Render(Style s, StylegroundRenderCtx ctx) {
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
