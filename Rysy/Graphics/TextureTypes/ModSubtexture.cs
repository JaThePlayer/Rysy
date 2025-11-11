namespace Rysy.Graphics.TextureTypes;

public sealed class ModSubtexture : VirtTexture {
    private readonly ModTexture _modTexture;
    private readonly Rectangle _rect;
    
    public int? RealWidth { get; init; }
    public int? RealHeight { get; init; }

    public Rectangle SubtextureRect => _rect;
    
    public ModTexture Parent => _modTexture;

    public ModSubtexture(ModTexture modTexture, Rectangle rect) {
        _modTexture = modTexture;
        _rect = rect;
        LoadedClipRect = rect;
    }

    protected override Task? QueueLoad() {
        return Task.Run(async () => {
            LoadedTexture = await _modTexture.ForceGetTexture();
            State = States.Loaded;
        });
    }

    public override int Width => RealWidth ?? _rect.Width;
    public override int Height => RealHeight ?? _rect.Height;

    // Make sure we don't dispose the base texture, there could be different subtextures still!
#pragma warning disable CA2215
    public override void Dispose() {
#pragma warning restore CA2215
    }
}