namespace Rysy.Graphics.TextureTypes;

internal sealed class VanillaTexture : UndisposableVirtTexture {
    public int W, H;

    public override int Width => W;
    public override int Height => H;
}
