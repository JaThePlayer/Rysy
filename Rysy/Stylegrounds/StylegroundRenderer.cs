using Rysy.Graphics;

namespace Rysy.Stylegrounds;

public static class StylegroundRenderer {
    public enum Layers {
        FG = 1,
        BG = 2,
        BGAndFG = BG | FG,
    }

    private static readonly RasterizerState CullNoneWithScissor = new() {
        CullMode = CullMode.None,
        ScissorTestEnable = true,
        FillMode = FillMode.Solid
    };

    public static bool NotMasked(Style style) {
        return !style.IsMasked();
    }

    public static void Render(Room room, MapStylegrounds styles, Camera camera, Layers layers, Func<Style, bool> filter, Rectangle? scissorRectWorldPos = null) {
        ArgumentNullException.ThrowIfNull(styles);
        float scale = camera.Scale;

        var ctx = new StylegroundRenderCtx(room, camera, Settings.Instance?.AnimateStylegrounds ?? false);

        var st = new SpriteBatchState(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.CreateScale(scale));

        if (scissorRectWorldPos is { } worldPosScissor) {
            var screenPos = camera.RealToScreen(worldPosScissor.Location.ToVector2()).ToPoint();
            st.ScissorRect = new(screenPos.X, screenPos.Y, (int) (worldPosScissor.Width * scale), (int) (worldPosScissor.Height * scale));
            st.RasterizerState = CullNoneWithScissor;
        }

        GFX.BeginBatch(st);

        var allStyles = layers switch {
            Layers.BG => styles.AllBackgroundStylesRecursive(),
            Layers.FG => styles.AllForegroundStylesRecursive(),
            Layers.BGAndFG => styles.AllStylesRecursive(),
            _ => Array.Empty<Style>(),
        };

        foreach (var s in allStyles) {
            if (filter(s))
                Render(s, ctx);
        }

        //ISprite.OutlinedRect(new(0,0, 320, 180), Color.Transparent, Color.Red).Render();

        GFX.EndBatch();
    }

    private static void Render(Style s, StylegroundRenderCtx ctx) {
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

        var lastState = GFX.EndBatch();
        GFX.BeginBatch(state);
        foreach (var sprite in sprites) {
            sprite.Render(renderCtx);
        }
        GFX.EndBatch();
        GFX.BeginBatch(lastState);
    }
}

public record StylegroundRenderCtx(Room Room, Camera Camera, bool Animate) {
    public Rectangle FullScreenBounds => new(0, 0, ScreenWidth, ScreenHeight);
    public int ScreenWidth => (int) (320 * 6f / Camera.Scale);
    public int ScreenHeight => (int) (180 * 6f / Camera.Scale);
}
