using Microsoft.Xna.Framework.Graphics;
using Rysy.Extensions;
using Rysy.Graphics.TextureTypes;
using Rysy.Helpers;
using Rysy.Mods;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace Rysy.Graphics;

public interface IAtlas {
    //public Dictionary<string, VirtTexture> Textures { get; }
    public static bool LogMissingTextures => Settings.UiEnabled && Settings.Instance.LogMissingTextures;
    
    public IEnumerable<(string virtPath, VirtTexture texture)> GetTextures();

    public VirtTexture this[string key] {
        get {
            if (key is null)
                return GFX.UnknownTexture;

            if (TryGet(key, out var texture))
                return texture;

            if (LogMissingTextures)
                Logger.Write("Atlas", LogLevel.Warning, $"Tried to access texture {key} that doesn't exist!");
        
            AddTexture(key, GFX.UnknownTexture);
            return GFX.UnknownTexture;
        }
    }

    public VirtTexture this[string key, int frame] {
        get {
            if (key is null)
                return GFX.UnknownTexture;

            if (TryGet(key, frame, out var texture))
                return texture;

            if (LogMissingTextures)
                Logger.Write("Atlas", LogLevel.Warning, $"Tried to access texture {key}, frame {frame} that doesn't exist!");
        
            AddTexture(key, GFX.UnknownTexture);
            return GFX.UnknownTexture;
        }
    }

    public bool TryGet(string key, [NotNullWhen(true)] out VirtTexture? texture);
    public bool TryGet(string key, int frame, [NotNullWhen(true)] out VirtTexture? texture);

    /// <summary>
    /// Equivalent to Celeste's Atlas.GetAtlasSubtextures
    /// Retrieve multiple textures stored under the same <paramref name="key" />.<br />
    /// Textures should be named in the following format
    /// <code>key0, key1, key2, key3</code>
    /// with up to six <c>0</c>s preceeding the index.
    /// </summary>
    /// <param name="key">The texture name.</param>
    public IReadOnlyList<VirtTexture> GetSubtextures(string key);

    public bool Exists(string key);
    public bool Exists(string key, int frame);

    /// <summary>
    /// Disposes all currently loaded textures. This does *not* clear textures, and instead just disposes of any native data.
    /// If a texture of the atlas gets requested after calling this, it'll get lazy-loaded again.
    /// </summary>
    public void DisposeTextures();

    public void AddTexture(string virtPath, VirtTexture texture);

    public void RemoveTextures(List<string> paths);

    public event Action<string> OnTextureLoad;
    public event Action OnUnload;
    public event Action OnChanged;
}

/// <summary>
/// An object representing a return value from <see cref="IAtlasExt.FindTextures"/>
/// </summary>
/// <param name="Path">The path of this texture, in full</param>
/// <param name="Captured">A part of the path captured by the regex passed into <see cref="IAtlasExt.FindTextures"/></param>
public record class FoundPath(string Path, string Captured);

public static class IAtlasExt {
    public static Cache<List<FoundPath>> FindTextures(this IAtlas atlas, Regex regex, 
        Func<FoundPath, VirtTexture, bool>? where = null) {
        var token = new CacheToken();
        var cache = new Cache<List<FoundPath>>(token, () => {
            var list = new List<FoundPath>();

            foreach (var (path, texture) in atlas.GetTextures()) {
                // ignore internal textures
                if (texture is ModTexture modTex && modTex.Mod == ModRegistry.RysyMod)
                    continue;
                if (path.StartsWith("Rysy:", StringComparison.Ordinal) ||
                    path.StartsWith("@Internal@", StringComparison.Ordinal))
                    continue;
                
                if (regex.Match(path) is { Success: true, Groups: [_, var secondGroup, ..] } match) {
                    var foundPath = new FoundPath(path, secondGroup.Value);
                    if (where?.Invoke(foundPath, texture) ?? true)
                        list.Add(foundPath);
                }
            }

            token.Reset();

            return list;
        });

        atlas.OnChanged += cache.Token.Invalidate;

        return cache;
    }

    public static async Task LoadFromDirectoryAsync(this IAtlas self, string dir, string prefix = "") {
        await Task.WhenAll(
                Directory.EnumerateFiles(dir, "*.png", SearchOption.AllDirectories)
                .Select(item => Task.Run(() => {
                    var virtPath = item.AsSpan()[(dir.Length + 1)..].ToVirtPath(prefix);//item.Replace(dir, "").ToVirtPath(prefix);
                    var texture = VirtTexture.FromFile(item);
                    lock (self) {
                        self.AddTexture(virtPath, texture);
                    }
                })));
    }

    public static async ValueTask LoadFromZip(this IAtlas self, string zipName, ZipArchive zip) {
        await Parallel.ForEachAsync(zip.Entries, (entry, token) => {
            var name = entry.FullName;
            if (name.StartsWith("Graphics/Atlases/Gameplay", StringComparison.Ordinal) 
            && name.EndsWith(".png", StringComparison.Ordinal)
            ) {
                var virtPath = name.AsSpan()["Graphics/Atlases/Gameplay/".Length..].ToVirtPath();
                var texture = VirtTexture.FromFile(zipName, entry);
                lock (self) {
                    self.AddTexture(virtPath, texture);
                }
            }

            return ValueTask.CompletedTask;
        });
    }

    /// <summary>
    /// Implements the Packer format
    /// </summary>
    public static async Task LoadFromPackerAtlasAsync(this IAtlas self, string path, bool noAtlas) {
        await Task.CompletedTask;
        using var metaStream = File.OpenRead($"{path}.meta");
        using var metaReader = new BinaryReader(metaStream);

        // unneded info
        metaReader.ReadInt32();
        metaReader.ReadString();
        metaReader.ReadInt32();

        int textureCount = metaReader.ReadInt16();
        for (int m = 0; m < textureCount; m++) {
#pragma warning disable CA2000 // Dispose objects before losing scope - the scope is not lost
            Texture2D? texture = null;
            var baseTexturePath = metaReader.ReadString();
            if (!noAtlas)
                texture = ReadVanillaAtlasDataFile(path, baseTexturePath);

            int subtextureCount = metaReader.ReadInt16();
            for (int n = 0; n < subtextureCount; n++) {
                string subtextPath = metaReader.ReadString().Replace('\\', '/');
                if (noAtlas) {
                    texture = ReadVanillaAtlasDataFile(path, Path.Combine(baseTexturePath, subtextPath));
                }
                short clipX = metaReader.ReadInt16();
                short clipY = metaReader.ReadInt16();
                short clipWidth = metaReader.ReadInt16();
                short clipHeight = metaReader.ReadInt16();

                short offsetX = metaReader.ReadInt16();
                short offsetY = metaReader.ReadInt16();
                short width = metaReader.ReadInt16();
                short height = metaReader.ReadInt16();

                var vTexture = VirtTexture.FromAtlasSubtexture(texture!, new(clipX, clipY, clipWidth, clipHeight), width, height);
#pragma warning restore CA2000
                vTexture.DrawOffset = new Vector2(offsetX, offsetY);

                self.AddTexture(subtextPath, vTexture);
            }
        }
        // TODO: LINKS support (only if needed)

        _sharedTextureBuffer = null;
    }

    private static byte[]? _sharedTextureBuffer;
    
    // TODO: cleanup
    private static unsafe Texture2D ReadVanillaAtlasDataFile(string path, string textureIndex) {
        Texture2D? texture = null;

        var fullPath = $"{Path.GetDirectoryName(path)}/{textureIndex}.data";
        if (!File.Exists(fullPath)) {
            texture = new Texture2D(RysyState.GraphicsDevice, 1, 1);
            texture.SetData(new Color[] { Color.White });
            Console.WriteLine(fullPath);
            return texture;
        }

        const int bytesSize = 524288;
        byte[] readDataBytes = new byte[bytesSize];
        byte[] textureBufferBytes = _sharedTextureBuffer ??= new byte[64 * 1024 * 1024];
        using var stream = File.OpenRead(fullPath);
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
        fixed (byte* textureBuffer = &textureBufferBytes[0]) {
            while (index < size) {
                int runLenEncodingSize = readDataBytes[pos++] * 4;

                byte alpha = hasTransparency ? readDataBytes[pos++] : byte.MaxValue;
                if (index + 3 >= textureBufferBytes.Length) {
                    throw new Exception("Atlas Texture is too big!");
                }
                
                if (alpha > 0) {
                    textureBuffer[index] = readDataBytes[pos + 2];
                    textureBuffer[index + 1] = readDataBytes[pos + 1];
                    textureBuffer[index + 2] = readDataBytes[pos];
                    textureBuffer[index + 3] = alpha;
                    pos += 3;
                } else {
                    // 0-fill the buffer, as it can be shared between multiple texture reads
                    *(int*) &textureBuffer[index] = 0;
                }

                if (runLenEncodingSize > 4) {
                    int nextPixel = index + 4;
                    int endRLE = index + runLenEncodingSize;

                    // weird pointer shenanigans to read/write a i32 from a byte[]
                    int col = *(int*) &textureBuffer[index];
                    if (endRLE - 4 >= textureBufferBytes.Length) {
                        throw new Exception("Atlas Texture is too big!");
                    }
                    
                    while (nextPixel < endRLE) {
                        *(int*) &textureBuffer[nextPixel] = col;
                        nextPixel += 4;
                    }
                }

                index += runLenEncodingSize;
                if (pos > 524256) {
                    int reset = bytesSize - pos;
                    for (int l = 0; l < reset; l++) {
                        readDataBytes[l] = readDataBytes[pos + l];
                    }
                    stream.Read(readDataBytes, reset, bytesSize - reset);
                    pos = 0;
                }
            }
        }

        texture = new Texture2D(RysyState.GraphicsDevice, width, height);
        texture.SetData(textureBufferBytes, 0, size);

        return texture;
    }

}
