namespace Rysy.Graphics;

/// <summary>
/// Allows customising the way Rysy draws paths between nodes of your entity. Not usable if your entity implements <see cref="ICustomNodeHandler"/>!
/// Typically, you should simply call one of the static functions available in the <see cref="NodePathTypes"/> class.
/// If not implemented, Rysy will default to calling <see cref="NodePathTypes.Line(Rysy.Entity)"/>.
/// </summary>
public interface INodePathProvider
{
    public IEnumerable<ISprite> NodePathSprites { get; }
}
