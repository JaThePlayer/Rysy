namespace Rysy.Graphics;

/// <summary>
/// Provides a method GetNodeSprites, which allows changing how Rysy picks sprites used for rendering the node.
/// Rysy will still automatically handle some aspects of node rendering for you.
/// </summary>
public interface INodeSpriteProvider
{
    public IEnumerable<ISprite> GetNodeSprites(int nodeIndex);
}
