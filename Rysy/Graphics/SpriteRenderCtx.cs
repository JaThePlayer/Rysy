namespace Rysy.Graphics;

/// <summary>
/// Stores the context for <see cref="ISprite"/> rendering
/// </summary>
public record SpriteRenderCtx(Camera? Camera, Vector2 CameraOffset, Room? Room, bool Animate) {
    private static SpriteRenderCtx? _animated;
    private static SpriteRenderCtx? _unanimated;

    public static SpriteRenderCtx Default(bool? animated = null) {
        animated ??= Settings.Instance?.Animate ?? false;

        return animated switch {
            true => _animated ??= new(null, default, null, true),
            false => _unanimated ??= new(null, default, null, false)
        };
    }
}