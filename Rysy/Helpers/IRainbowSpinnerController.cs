namespace Rysy.Helpers;

/// <summary>
/// When implemented by an entity, allows changing how rainbow colors get created.
/// </summary>
public interface IRainbowSpinnerController {
    /// <summary>
    /// Tries to get a custom rainbow color at a given position at a given time.
    /// </summary>
    public bool TryGetRainbowColor(Vector2 pos, float time, out Color res);
    
    public bool IsLocal { get; }
}