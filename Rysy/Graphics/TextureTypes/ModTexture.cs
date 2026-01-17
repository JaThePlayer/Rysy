using Rysy.Extensions;
using Rysy.Mods;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Rysy.Graphics.TextureTypes;

public sealed class ModTexture : VirtTexture, IModAsset {
    private IDisposable? _watcher;
    
    public ModMeta Mod { get; init; }
    public string VirtPath { get; init; }

    public ModTexture(ModMeta mod, string virtPath) {
        Mod = mod;
        VirtPath = virtPath;
    }

    public string? SourceModName => Mod.Name;

    public List<string>? DependencyModNames => null;

    public override void Dispose() {
        _watcher?.Dispose();
        base.Dispose();
    }

    private Task _QueueLoad() {
        return Task.Run(() => {
                try {
                    Mod.Filesystem.TryWatchAndOpen(VirtPath, stream => {
                        lock (this) {
                            LoadedTexture?.Dispose();

                            Texture2D? texture;
#if FNA
                            if (Mod.Filesystem is FolderModFilesystem) {
                                texture = Premultiply(Texture2D.FromStream(RysyState.GraphicsDevice, stream));
                            } else {
                                using var memStr = new MemoryStream();
                                stream.CopyTo(memStr);
                                texture = Premultiply(Texture2D.FromStream(RysyState.GraphicsDevice, memStr));
                            }
#else
                        texture = Texture2D.FromStream(RysyEngine.GDM.GraphicsDevice, stream, DefaultColorProcessors.PremultiplyAlpha);
#endif
                            ClipRect = new(0, 0, texture.Width, texture.Height);
                            LoadedTexture = texture;
                        }
                    }, out _watcher);
                } catch (Exception e) {
                    Logger.Write("ModTexture", LogLevel.Error, $"Failed loading mod texture {this}, {e}");
                    throw;
                }

                State = States.Loaded;
            }
        );
    }

    protected override Task QueueLoad() => _QueueLoad();

    protected override bool TryPreloadClipRect() {
        lock (Mod.Filesystem) {
            return Mod.Filesystem.OpenFile(VirtPath, stream => {
                if (PreloadSizeFromPng(stream, VirtPath, out int w, out int h)) {
                    ClipRect = new(0, 0, w, h);
                    return true;
                } else {
                    throw new Exception($"Invalid PNG for {VirtPath}");
                }
            });
        }
    }

    public override string ToString() => $"ModTexture:{{{VirtPath}, [{Mod.Name}]}}";
    
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private unsafe Texture2D Premultiply(Texture2D texture) {
        Color[] data = new Color[texture.Width * texture.Height];
        texture.GetData(data);
        
        //using (var _w = new ScopedStopwatch($"Premultiply: {VirtPath}")) {
            fixed (Color* raw = data) {
                var i = 0;
            
                #if NET8_0_OR_GREATER
                var rawInt = (int*) raw;
                if (Vector256.IsHardwareAccelerated) {
                    for (; i + Vector256<int>.Count < data.Length; i += Vector256<int>.Count) {
                        var colorsInt = Vector256.Load(&rawInt[i]);
                        // If all colors are fully transparent, there's nothing to do
                        if (colorsInt == Vector256.Create(0))
                            continue;
                    
                        unchecked {
                            var alpha = colorsInt & Vector256.Create((int)0xff000000);
                            if (alpha == Vector256.Create((int)0xff000000)) {
                                continue;
                            }
                            var alphaMult = Vector256.ConvertToSingle(alpha >>> 24) / 255f;

                            var packed =  Vector256.ConvertToInt32(Vector256.ConvertToSingle( colorsInt & Vector256.Create(0x000000ff)) * alphaMult)
                                       | (Vector256.ConvertToInt32(Vector256.ConvertToSingle((colorsInt & Vector256.Create(0x0000ff00)) >>> 8)  * alphaMult) << 8)
                                       | (Vector256.ConvertToInt32(Vector256.ConvertToSingle((colorsInt & Vector256.Create(0x00ff0000)) >>> 16) * alphaMult) << 16)
                                       | alpha;
                            packed.CopyTo(new Span<int>(&rawInt[i], Vector256<int>.Count));
                        }
                    }
                }
                #endif
            
                for (; i < data.Length; i++) {
                    ref Color c = ref raw[i];
                    if (c.A is 255)
                        continue;
                    byte r = (byte)float.Round(c.R * c.A / 255f);
                    byte g = (byte)float.Round(c.G * c.A / 255f);
                    byte b = (byte)float.Round(c.B * c.A / 255f);

                    c.PackedValue = (uint)(r | (g << 8) | (b << 16) | (c.A << 24));
                }
            }
        //}
        

        texture.SetData(data);
        return texture;
    }
    
    private static bool PreloadSizeFromPng(Stream stream, string filename, out int w, out int h) {
        w = 0;
        h = 0;
        using var binaryReader = new BinaryReader(stream);


        ulong magic = binaryReader.ReadUInt64();
        if (magic != 727905341920923785UL) {
            Logger.Write("VirtTexture.Preload", LogLevel.Warning, $"PNG magic mismatch for {filename.CorrectSlashes()} - got {magic:0xX16}, expected 0x0A1A0A0D474E5089");
            return false;
        }

        uint firstChunkLen = binaryReader.ReadUInt32();
        if (firstChunkLen != 218103808u) {
            Logger.Write("VirtTexture.Preload", LogLevel.Warning, $"PNG first chunk length mismatch for {filename.CorrectSlashes()} - got {firstChunkLen:0xX8}, expected 0x0D000000");
            return false;
        }

        uint ihdrMarker = binaryReader.ReadUInt32();
        if (ihdrMarker != 1380206665u) {
            Logger.Write("VirtTexture.Preload", LogLevel.Warning, $"PNG IHDR marker mismatch for {filename.CorrectSlashes()} - got {firstChunkLen:0xX8}, expected 0x52444849");
            return false;
        }

        w = SwapEndian(binaryReader.ReadInt32());
        h = SwapEndian(binaryReader.ReadInt32());

        return true;
    }
    
    private static int SwapEndian(int data) {
        return (data & 255) << 24 | (data >> 8 & 255) << 16 | (data >> 16 & 255) << 8 | (data >> 24 & 255);
    }
}
