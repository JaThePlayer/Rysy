using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO.Compression;

namespace Rysy.Graphics;

public static class GFX
{
    public static Atlas Atlas { get; private set; } = null!;

    public static SpriteBatch Batch { get; private set; } = null!;

    public static Texture2D Pixel { get; private set; } = null!;
    public static VirtTexture VirtPixel { get; private set; } = null!;

    internal static void Load(RysyEngine eng)
    {
        Atlas = new();
        using (ScopedStopwatch watch = new("Scanning vanilla atlas"))
            LoadVanillaAtlas();

        using (ScopedStopwatch watch = new("Scanning Rysy assets"))
            Atlas.LoadFromDirectory("Assets", "Rysy");


        using (ScopedStopwatch watch = new("Scanning mod dirs"))
        {
            Directory.GetDirectories(Settings.Instance.ModsDirectory)
            .Select(item => Task.Run(() => LoadModFromDir(item)))
            .AwaitAll();
        }

        using (ScopedStopwatch watch = new("Scanning mod .zips"))
        {
            Directory.EnumerateFiles(Settings.Instance.ModsDirectory, "*.zip")
                .Select(item => Task.Run(() => LoadModFromZip(item)))
                .AwaitAll();
        }

        Batch = new(eng.GraphicsDevice);

        Pixel = new(eng.GraphicsDevice, 1, 1);
        Pixel.SetData(new Color[1] { Color.White });
        VirtPixel = VirtTexture.FromTexture(Pixel);
    }

    internal static void LoadVanillaAtlas()
    {
        var path = $"{Settings.Instance.CelesteDirectory}/Content/Graphics/Atlases/Gameplay";
        Atlas.LoadFromPackerAtlas(path);
    }

    // TODO: MOVE
    internal static void LoadModFromDir(string modDir)
    {
        modDir = modDir.Replace('\\', '/').TrimEnd('/');

        var gameplayAtlasPath = $"{modDir}/Graphics/Atlases/Gameplay";
        if (Directory.Exists(gameplayAtlasPath))
        {
            Atlas.LoadFromDirectory(gameplayAtlasPath);
        }
    }

    internal static void LoadModFromZip(string modZipPath)
    {
        var stream = File.OpenRead(modZipPath);
        var zip = new ZipArchive(stream, ZipArchiveMode.Read, false);

        Atlas.LoadFromZip(modZipPath, zip);
    }

    public static string ToVirtPath(this string val, string prefix = "")
    {
        var ext = Path.GetExtension(val) switch { 
            "" or null => ".png",
            var other => other,
        };
        var v = val.Replace('\\', '/').Replace(ext, "").Trim('/');
        if (!string.IsNullOrWhiteSpace(prefix))
            v = $"{prefix}:{v}";

        return v;
    }
}
