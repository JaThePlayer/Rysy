using Microsoft.Xna.Framework.Graphics;
using System.Collections.Concurrent;
using System.IO.Compression;

namespace Rysy.Graphics.TextureTypes;

public sealed class ZipVirtTexture : VirtTexture
{
    private string archivePath;
    private string entryName;

    public ZipVirtTexture(string archivePath, ZipArchiveEntry entry)
    {
        entryName = entry.FullName;
        this.archivePath = archivePath;
    }

    protected override Task? QueueLoad()
    {
        return Task.Run(() =>
        {
            try
            {

                var arch = SharedZipArchive.Get(archivePath);
#if DirectX
                // DirectX is super fast at loading textures it seems, so this is more performant than reading to an intermediate buffer
                lock (arch)
                {
                    using var stream = arch.GetEntry(entryName)!.Open();
                    texture = Texture2D.FromStream(RysyEngine.GDM.GraphicsDevice, stream);
                    SharedZipArchive.Release(archivePath);
                }
#else
                // Loading textures is super slow, so first read the zip file into an intermediate buffer, 
                // and then release the zip quicker to allow other threads to read the zip sooner
                byte[] buffer;
                lock (arch)
                {
                    var entry = arch.GetEntry(entryName)!;

                    using var stream = entry.Open();
                    buffer = new byte[entry.Length];
                    stream.ReadExactly(buffer);

                    SharedZipArchive.Release(archivePath);
                }
                using var memStr = new MemoryStream(buffer);
                texture = Texture2D.FromStream(RysyEngine.GDM.GraphicsDevice, memStr);
#endif
            }
            catch (Exception e)
            {
                Logger.Write("ZipVirtTexture", LogLevel.Error, $"Failed loading zip texture {this}, {e}");
                throw;
            }

            ClipRect = new(0, 0, texture.Width, texture.Height);
            state = State.Loaded;
        }
        );
    }

    protected override bool TryPreloadClipRect()
    {
        var arch = SharedZipArchive.Get(archivePath);
        lock (arch)
        {
            using var stream = arch.GetEntry(entryName)!.Open();
            if (FileVirtTexture.PreloadSizeFromPNG(stream, entryName, out int w, out int h))
            {
                ClipRect = new(0, 0, w, h);
                SharedZipArchive.Release(archivePath);
                return true;
            }
            else
            {
                SharedZipArchive.Release(archivePath);
                throw new Exception($"Invalid PNG for {entryName}");
            }
        }
    }

    public override string ToString() => $"ZipVirtTexture:{{{entryName}, {archivePath.CorrectSlashes()}}}";
}

/// <summary>
/// Supplies a ZipArchive shared between threads that require the same zip at once.
/// Once none of the threads require the zip anymore, it gets closed.
/// This is useful for lazy loaded zip assets, as constant closing and reopening of ZipArchives is really slow,
/// but keeping them open forever blocks mod updates.
/// </summary>
internal static class SharedZipArchive
{
    class SharedZip
    {
        public long UseCount;
        public ZipArchive Zip = null!;
    }

    static ConcurrentDictionary<string, SharedZip> Zips = new();

    /// <summary>
    /// Gets a shared ZipArchive object. Make sure to lock{} the object in your function, or you'll get crashes.
    /// Once you're done with the zip, call <see cref="Release(string)"/> so that it can get closed once it's not needed.
    /// DO NOT Dispose the archive, DO NOT use the `using var` construction.
    /// </summary>
    /// <param name="archPath"></param>
    /// <returns></returns>
    public static ZipArchive Get(string archPath)
    {
        lock (Zips)
        {
            if (Zips.TryGetValue(archPath, out var shared))
            {
                shared.UseCount++;
                return shared.Zip;
            }

            var stream = File.OpenRead(archPath);
            var arch = new ZipArchive(stream, ZipArchiveMode.Read, false);

            Zips[archPath] = new()
            {
                Zip = arch,
                UseCount = 1,
            };

            return arch;
        }

    }

    /// <summary>
    /// Marks the archive as no longer needed. Once all users of this zip call this function, the zip will get disposed automatically.
    /// </summary>
    /// <param name="archPath"></param>
    public static void Release(string archPath)
    {
        lock (Zips)
        {
            if (Zips.TryGetValue(archPath, out var z))
            {
                z.UseCount--;

                if (z.UseCount == 0)
                {
                    Zips.Remove(archPath, out _);
                    z.Zip.Dispose();
                }
            }
        }
    }
}
