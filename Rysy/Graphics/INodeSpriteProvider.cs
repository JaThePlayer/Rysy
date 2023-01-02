namespace Rysy.Graphics;

/// <summary>
/// Provides a method GetNodeSprites, which allows changing how Rysy picks sprites used for rendering the node.
/// Rysy will still automatically handle some aspects of node rendering for you, like rendering paths between nodes.
/// </summary>
public interface INodeSpriteProvider
{
    public IEnumerable<ISprite> GetNodeSprites(int nodeIndex);
}
