using Microsoft.Xna.Framework.Graphics;
using Rysy.Extensions;

namespace Rysy.Graphics.TextureTypes;

internal sealed class FileVirtTexture : VirtTexture {
    public string Filename;

    public FileVirtTexture(string filename) {
        Filename = filename;
    }

    protected override Task? QueueLoad() {
        return Task.Run(() => {
            try {
                texture = Texture2D.FromFile(RysyEngine.GDM.GraphicsDevice, Filename);
                ClipRect = new(0, 0, texture.Width, texture.Height);
                state = State.Loaded;
            } catch (Exception e) {
                Logger.Write("FileVirtTexture", LogLevel.Error, $"Failed loading file texture {this}, {e}");
                throw;
            }

        });
    }

    protected override bool TryPreloadClipRect() {
        using var stream = File.OpenRead(Filename);

        if (PreloadSizeFromPNG(stream, Filename, out int w, out int h)) {
            ClipRect = new(0, 0, w, h);
            return true;
        } else {
            throw new Exception($"Invalid PNG for {this}");
        }
    }

    public static bool PreloadSizeFromPNG(Stream stream, string filename, out int w, out int h) {
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

        uint IHDRMarker = binaryReader.ReadUInt32();
        if (IHDRMarker != 1380206665u) {
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

    public override string ToString() => $"FileVirtTexture:{{{Filename.CorrectSlashes()}}}";
}