using Rysy.Graphics;

namespace Rysy.Stylegrounds;

public static class StylegroundRenderer {
    public enum Layers {
        FG = 1,
        BG = 2,
        BGAndFG = BG | FG,
    }

    public static void Render(Room room, MapStylegrounds styles, Camera camera, Layers layers) {
        ArgumentNullException.ThrowIfNull(styles);
        var cam = new Camera();
        cam.Scale = camera.Scale;

        var ctx = new StylegroundRenderCtx(room, camera, Settings.Instance?.AnimateStylegrounds ?? false);

        GFX.BeginBatch(cam);

        var allStyles = layers switch {
            Layers.BG => styles.AllBackgroundStylesRecursive(),
            Layers.FG => styles.AllForegroundStylesRecursive(),
            Layers.BGAndFG => styles.AllStylesRecursive(),
            _ => Array.Empty<Style>(),
        };

        foreach (var s in allStyles) {
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
        //Console.WriteLine((lastState, state));
        GFX.BeginBatch(state);
        foreach (var sprite in sprites) {
            sprite.Render();
        }
        GFX.EndBatch();
        GFX.BeginBatch(lastState);
    }
}

public record class StylegroundRenderCtx(Room Room, Camera Camera, bool Animate);
