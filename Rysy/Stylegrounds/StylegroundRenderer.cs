using Rysy.Graphics;

namespace Rysy.Stylegrounds;

public static class StylegroundRenderer {
    public enum Layers {
        FG = 1,
        BG = 2,
        BGAndFG = BG | FG,
    }

    private static RasterizerState CullNoneWithScissor = new() {
        CullMode = CullMode.None,
        ScissorTestEnable = true,
        FillMode = FillMode.Solid
    };

    public static bool NotMasked(Style style) {
        foreach (var tag in style.Tags) {
            if (tag.StartsWith("mask_", StringComparison.Ordinal) || tag.StartsWith("sjstylemask_", StringComparison.Ordinal)) {
                return false;
            }
        }

        return true;
    }

    public static Func<Style, bool> WithTag(string targetTag) => (style) => {
        foreach (var tag in style.Tags) {
            if (tag == targetTag) {
                return true;
            }
        }

        return false;
    };

    public static void Render(Room room, MapStylegrounds styles, Camera camera, Layers layers, Func<Style, bool> filter, Rectangle? scissorRectWorldPos = null) {
        ArgumentNullException.ThrowIfNull(styles);
        var cam = new Camera(RysyEngine.GDM.GraphicsDevice.Viewport);
        cam.Scale = camera.Scale;

        var ctx = new StylegroundRenderCtx(room, camera, Settings.Instance?.AnimateStylegrounds ?? false);

        var st = new SpriteBatchState(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, cam.Matrix);

        if (scissorRectWorldPos is { } worldPosScissor) {
            //var roomPos = camera.RealToScreen(room.Pos);
            //scissorRect ??= new((int) roomPos.X, (int) roomPos.Y, (int) (room.Width * cam.Scale), (int) (room.Height * cam.Scale));

            var screenPos = camera.RealToScreen(worldPosScissor.Location.ToVector2()).ToPoint();
            st.ScissorRect = new(screenPos.X, screenPos.Y, (int) (worldPosScissor.Width * cam.Scale), (int) (worldPosScissor.Height * cam.Scale));
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

        if (state is null) {
            foreach (var sprite in sprites) {
                sprite.Render();
            }
            return;
        }

        var lastState = GFX.EndBatch();
        GFX.BeginBatch(state);
        foreach (var sprite in sprites) {
            sprite.Render();
        }
        GFX.EndBatch();
        GFX.BeginBatch(lastState);
    }
}

public record class StylegroundRenderCtx(Room Room, Camera Camera, bool Animate);
