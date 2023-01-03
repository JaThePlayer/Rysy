using Microsoft.Xna.Framework.Graphics;
using System.IO.Compression;

namespace Rysy.Graphics;

public interface IAtlas
{
    //public Dictionary<string, VirtTexture> Textures { get; }

    public IEnumerable<(string virtPath, VirtTexture texture)> GetTextures();

    public VirtTexture this[string key] { get; }

    /// <summary>
    /// Disposes all currently loaded textures. This does *not* clear <see cref="Textures"/>, and instead just disposes of any native data.
    /// If a texture of the atlas gets requested after calling this, it'll get lazy-loaded again.
    /// </summary>
    public void DisposeTextures();

    public void AddTexture(string virtPath, VirtTexture texture);
}

public static class IAtlasExt
{
    public static async Task LoadFromDirectoryAsync(this IAtlas self, string dir, string prefix = "")
    {
        /*
            foreach (var item in Directory.EnumerateFiles(dir, "*.png", SearchOption.AllDirectories))
            {
                var virtPath = item.Replace(dir, "").ToVirtPath(prefix);
                var texture = VirtTexture.FromFile(item);
                self.AddTexture(virtPath, texture);
            }*/
        await Task.WhenAll(
                Directory.EnumerateFiles(dir, "*.png", SearchOption.AllDirectories)
                .Select(item => Task.Run(() => {
                    var virtPath = item.Replace(dir, "").ToVirtPath(prefix);
                    var texture = VirtTexture.FromFile(item);
                    lock (self)
                    {
                        self.AddTexture(virtPath, texture);
                    }
                })));
    }

    public static void LoadFromZip(this IAtlas self, string zipName, ZipArchive zip)
    {
        foreach (var item in zip.Entries)
        {
            var name = item.FullName;
            if (name.StartsWith("Graphics/Atlases/Gameplay"))
            {
                var virtPath = name.Replace("Graphics/Atlases/Gameplay", "").ToVirtPath();
                var texture = VirtTexture.FromFile(zipName, item);
                self.AddTexture(virtPath, texture);
            }
        }
    }

    /// <summary>
    /// Implements the Packer format
    /// </summary>
    public static async Task LoadFromPackerAtlasAsync(this IAtlas self, string path)
    {
        await Task.CompletedTask;
        using var metaStream = File.OpenRead($"{path}.meta");
        using var metaReader = new BinaryReader(metaStream);

        // unneded info
        metaReader.ReadInt32();
        metaReader.ReadString();
        metaReader.ReadInt32();

        int textureCount = metaReader.ReadInt16();
        for (int m = 0; m < textureCount; m++)
        {
            var texture = ReadVanillaAtlasDataFile(path, metaReader.ReadString());

            int subtextureCount = metaReader.ReadInt16();
            for (int n = 0; n < subtextureCount; n++)
            {
                string subtextPath = metaReader.ReadString().Replace('\\', '/');
                short clipX = metaReader.ReadInt16();
                short clipY = metaReader.ReadInt16();
                short clipWidth = metaReader.ReadInt16();
                short clipHeight = metaReader.ReadInt16();

                short offsetX = metaReader.ReadInt16();
                short offsetY = metaReader.ReadInt16();
                short width = metaReader.ReadInt16();
                short height = metaReader.ReadInt16();

                var vTexture = VirtTexture.FromAtlasSubtexture(texture, new(clipX, clipY, clipWidth, clipHeight), width, height);
                vTexture.DrawOffset = new Vector2(offsetX, offsetY);

                self.AddTexture(subtextPath, vTexture);
            }
        }
        // TODO: LINKS support (only if needed)

    }

    // TODO: cleanup
    private static unsafe Texture2D ReadVanillaAtlasDataFile(string path, string textureIndex)
    {
        const int bytesSize = 524288;
        byte[] readDataBytes = new byte[bytesSize];
        byte[] textureBufferBytes = new byte[67108864];

        using var stream = File.OpenRead($"{Path.GetDirectoryName(path)}/{textureIndex}.data");
        using var dataReader = new BinaryReader(stream);

        stream.Read(readDataBytes, 0, bytesSize);

        int pos = 0;

        int width = BitConverter.ToInt32(readDataBytes, pos);
        int height = BitConverter.ToInt32(readDataBytes, pos + 4);
        bool hasTransparency = readDataBytes[pos + 8] == 1;

        pos += 9;
        int size = width * height * 4;
        int index = 0;

        // use unsafe access to bypass bounds checks for performance
        fixed (byte* readData = &readDataBytes[0])
        fixed (byte* textureBuffer = &textureBufferBytes[0])
        {
            while (index < size)
            {
                int runLenEncodingSize = readDataBytes[pos++] * 4;

                byte alpha = hasTransparency ? readDataBytes[pos++] : byte.MaxValue;
                if (alpha > 0)
                {
                    textureBuffer[index] = readDataBytes[pos + 2];
                    textureBuffer[index + 1] = readDataBytes[pos + 1];
                    textureBuffer[index + 2] = readDataBytes[pos];
                    textureBuffer[index + 3] = alpha;
                    pos += 3;
                }

                if (runLenEncodingSize > 4)
                {
                    int nextPixel = index + 4;
                    int endRLE = index + runLenEncodingSize;

                    // weird pointer schenanigans to read/write a i32 from a byte[]
                    int col = *(int*)&textureBuffer[index];

                    while (nextPixel < endRLE)
                    {
                        *(int*)&textureBuffer[nextPixel] = col;
                        nextPixel += 4;
                    }
                }

                index += runLenEncodingSize;
                if (pos > 524256)
                {
                    int reset = bytesSize - pos;
                    for (int l = 0; l < reset; l++)
                    {
                        readDataBytes[l] = readDataBytes[pos + l];
                    }
                    stream.Read(readDataBytes, reset, bytesSize - reset);
                    pos = 0;
                }
            }
        }

        var texture = new Texture2D(RysyEngine.GDM.GraphicsDevice, width, height);
        texture.SetData(textureBufferBytes, 0, size);

        return texture;
    }

}
