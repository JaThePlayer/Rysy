namespace Rysy.Graphics;

public struct ISpriteDepthComparer : IComparer<ISprite>
{
    public int Compare(ISprite? x, ISprite? y)
    {
        return (y?.Depth ?? 0) - (x?.Depth ?? 0);
    }
}
