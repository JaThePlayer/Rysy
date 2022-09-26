namespace Rysy.Graphics.TextureTypes;


/// <summary>
/// A VirtTexture for which calling Dispose and QueueLoad does nothing.
/// Should only be used for very specific cases.
/// </summary>
internal class UndisposableVirtTexture : VirtTexture
{
    public override void Dispose()
    {
    }

    protected override Task? QueueLoad()
    {
        return null;
    }
}
