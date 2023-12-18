using Rysy.Extensions;
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
                        _texture?.Dispose();

#if FNA
                        if (Mod.Filesystem is FolderModFilesystem) {
                            _texture = Texture2D.FromStream(RysyEngine.GDM.GraphicsDevice, stream);
                            return;
                        }

                        using var memStr = new MemoryStream();
                        //buffer = new byte[stream.Length];
                        stream.CopyTo(memStr);
                        _texture = Texture2D.FromStream(RysyEngine.GDM.GraphicsDevice, memStr);

#else
                            _texture = Texture2D.FromStream(RysyEngine.GDM.GraphicsDevice, stream, DefaultColorProcessors.PremultiplyAlpha);
#endif
                    }
                });
            } catch (Exception e) {
                Logger.Write("ModTexture", LogLevel.Error, $"Failed loading mod texture {this}, {e}");
                throw;
            }

            ClipRect = new(0, 0, _texture!.Width, _texture.Height);
            _state = State.Loaded;
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
