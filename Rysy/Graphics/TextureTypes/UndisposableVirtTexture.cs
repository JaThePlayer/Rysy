namespace Rysy.Graphics.TextureTypes;


/// <summary>
/// A VirtTexture for which calling Dispose and QueueLoad does nothing.
/// Should only be used for very specific cases.
/// </summary>
internal class UndisposableVirtTexture : VirtTexture {
    #pragma warning disable CA2215 // Intentionally avoid calling base.Dispose
    public override void Dispose() {
    }
    #pragma warning restore CA2215

    protected override Task? QueueLoad() {
        return null;
    }
}
