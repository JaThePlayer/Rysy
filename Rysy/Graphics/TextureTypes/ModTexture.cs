using Rysy.Helpers;
using Rysy.Mods;
using System.Diagnostics;
using System.Runtime.Intrinsics;

namespace Rysy.Graphics.TextureTypes;

public sealed class ModTexture : VirtTexture, IModAsset {
    private IDisposable? _watcher;

    private readonly Lock _loadingLock = new();
    
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
        _watcher = null;
        base.Dispose();
    }

    private delegate IntPtr Fna3DReadImageStreamDelegate(Stream stream, out int width, out int height, out int len, int forceW = -1, int forceH = -1, bool zoom = false);
    private static readonly Fna3DReadImageStreamDelegate Fna3dReadImageStream =
        typeof(Game).Assembly
            .GetType("Microsoft.Xna.Framework.Graphics.FNA3D")
            ?.GetMethod("ReadImageStream")
            ?.CreateDelegate<Fna3DReadImageStreamDelegate>()
        ?? throw new Exception("Microsoft.Xna.Framework.Graphics.FNA3D.ReadImageStream does not exist or has different signature, cannot proceed with texture loading!");
    
    private delegate void Fna3dImageFreeDelegate(IntPtr mem);
    private static readonly Fna3dImageFreeDelegate Fna3dImageFree =
        typeof(Game).Assembly
            .GetType("Microsoft.Xna.Framework.Graphics.FNA3D")
            ?.GetMethod("FNA3D_Image_Free")
            ?.CreateDelegate<Fna3dImageFreeDelegate>() 
        ?? throw new Exception("Microsoft.Xna.Framework.Graphics.FNA3D.FNA3D_Image_Free does not exist or has different signature, cannot proceed with texture loading!");
        
    
    private async Task _QueueLoad(CancellationToken ct) {
        _watcher ??= Mod.Filesystem.RegisterFilewatch(VirtPath, new WatchedAsset { OnChanged = _ => Dispose() });
        
        var success = Mod.Filesystem.TryOpenFile(VirtPath, stream => {
            try {
                lock (_loadingLock) {
                    int w, h, len;
                    nint ptr;
                    if (stream.CanSeek) {
                        ptr = ReadPremultipliedTextureDataFromStream(stream, out w, out h, out len);
                    } else {
                        using var memStr = new PooledMemoryStream();
                        stream.CopyTo(memStr);
                        memStr.Seek(0, SeekOrigin.Begin);
                        ptr = ReadPremultipliedTextureDataFromStream(memStr, out w, out h, out len);
                    }

                    ClipRect = new Rectangle(0, 0, w, h);

                    if (Settings.Instance.AllowMultithreadedTextureCreation) {
                        SendDataToGpu(w, h, ptr, len);
                    } else {
                        RysyState.OnEndOfThisFrame += () => {
                            SendDataToGpu(w, h, ptr, len);
                        };
                    }
                }
            } catch (Exception ex) {
                SwitchToFallbackTexture();
                Logger.Write("ModTexture", LogLevel.Error, $"Failed loading mod texture {this}, {ex}");
            }
        });

        if (success) {
            // Wait for our callback registered in RysyState.OnEndOfThisFrame to trigger.
            while (State == States.Loading) {
                await Task.Delay(18, ct);
            }
        } else {
            Logger.Write("ModTexture", LogLevel.Error, $"Failed to find mod texture {this} - file does not exist anymore or could not be opened.");
            SwitchToFallbackTexture();
        }
    }

    private void SendDataToGpu(int w, int h, IntPtr ptr, int len) {
        if (ptr == 0) {
            Logger.Write("ModTexture", LogLevel.Error, $"Failed loading mod texture {this} - File was not a valid .png file.");
            SwitchToFallbackTexture();
            return;
        }
        
        try {
            var texture = new Texture2D(RysyState.GraphicsDevice, w, h);
            texture.SetDataPointerEXT(0, null, ptr, len);
            
            ClipRect = new(0, 0, texture.Width, texture.Height);
            LoadedTexture = texture;
            State = States.Loaded;
        } catch (Exception ex) {
            Logger.Write("ModTexture", LogLevel.Error,
                $"Failed loading mod texture {this}\n{ex}");
            SwitchToFallbackTexture();
        } finally {
            Fna3dImageFree(ptr);
        }
    }

    private void SwitchToFallbackTexture() {
        var texture = Gfx.UnknownTexture.Texture ?? Gfx.Pixel;
        ClipRect = new(0, 0, texture.Width, texture.Height);
        LoadedTexture = texture;
        State = States.Loaded;
    }

    private nint ReadPremultipliedTextureDataFromStream(Stream stream, out int w, out int h, out int len)
    {
        var ptr = Fna3dReadImageStream(stream, out w, out h, out len);
        unsafe {
            Debug.Assert(sizeof(Color) == 4);
            Span<Color> data = new Span<Color>((void*)ptr, w * h);
            Premultiply(data);
        }

        return ptr;
    }

    protected override Task QueueLoad(CancellationToken ct) => Task.Run(() => _QueueLoad(ct), ct);

    protected override bool TryPreloadClipRect() {
        // If we're currently loading, try waiting a bit to try to get the clip rect from the loading thread.
        // Don't wait around forever though, the texture loading code needs to be able to run code on the main thread!
        if (_loadingLock.TryEnter(TimeSpan.FromSeconds(0.25f))) {
            _loadingLock.Exit();
        }

        if (LoadedClipRect != null) {
            return true;
        }

        return Mod.Filesystem.OpenFile(VirtPath, stream => {
            if (PreloadSizeFromPng(stream, VirtPath, out int w, out int h)) {
                ClipRect = new(0, 0, w, h);
                return true;
            }

            throw new Exception($"Invalid PNG for {VirtPath}");
        });
    }

    public override string ToString() => $"ModTexture:{{{VirtPath}, [{Mod.Name}]}}";
    
    private unsafe void Premultiply(Span<Color> data) {
        
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
