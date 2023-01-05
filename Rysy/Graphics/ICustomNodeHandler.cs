namespace Rysy.Graphics;

/// <summary>
/// Allows complete control over how nodes are rendered.
/// </summary>
public interface ICustomNodeHandler {
    public IEnumerable<ISprite> GetNodeSprites();
}
