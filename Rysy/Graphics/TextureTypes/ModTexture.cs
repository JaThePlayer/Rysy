using Rysy.Mods;

namespace Rysy.Graphics.TextureTypes;

public sealed class ModTexture : VirtTexture, IModAsset {
    public ModMeta Mod { get; init; }
    public string VirtPath { get; init; }

    public ModTexture(ModMeta mod, string virtPath) {
        Mod = mod;
        VirtPath = virtPath;
    }

    public string? SourceModName => Mod.Name;

    public List<string>? DependencyModNames => null;

    protected override Task? QueueLoad() {
        return Task.Run(() => {
            try {
                Mod.Filesystem.TryWatchAndOpen(VirtPath, stream => {
                    lock (this) {
                        texture?.Dispose();
                        texture = Texture2D.FromStream(RysyEngine.GDM.GraphicsDevice, stream, DefaultColorProcessors.PremultiplyAlpha);
                    }
                });
            } catch (Exception e) {
                Logger.Write("ModTexture", LogLevel.Error, $"Failed loading mod texture {this}, {e}");
                throw;
            }

            ClipRect = new(0, 0, texture.Width, texture.Height);
            state = State.Loaded;
        }
        );
    }

    protected override bool TryPreloadClipRect() {
        lock (Mod.Filesystem) {
            return Mod.Filesystem.OpenFile(VirtPath, stream => {
                if (FileVirtTexture.PreloadSizeFromPNG(stream, VirtPath, out int w, out int h)) {
                    ClipRect = new(0, 0, w, h);
                    return true;
                } else {
                    throw new Exception($"Invalid PNG for {VirtPath}");
                }
            });
        }
    }

    public override string ToString() => $"ModTexture:{{{VirtPath}, [{Mod.Name}]}}";
}
