using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rysy.Scenes;
using System.IO.Compression;

namespace Rysy.Graphics;

public static class GFX
{
    public static IAtlas Atlas { get; private set; } = null!;

    public static SpriteBatch Batch { get; private set; } = null!;

    public static Texture2D Pixel { get; private set; } = null!;
    public static VirtTexture VirtPixel { get; private set; } = null!;

    /// <summary>
    /// Loads the bare minimum needed to render anything.
    /// </summary>
    /// <param name="eng"></param>
    internal static void LoadEssencials(RysyEngine eng) {
        Batch = new(eng.GraphicsDevice);

        Pixel = new(eng.GraphicsDevice, 1, 1);
        Pixel.SetData(new Color[1] { Color.White });
        VirtPixel = VirtTexture.FromTexture(Pixel);

        PicoFont.Init();
    }

    /// <summary>
    /// Loads all textures, including those from mods.
    /// </summary>
    /// <returns></returns>
    internal static async ValueTask LoadAsync()
    {
        LoadingScene? scene = RysyEngine.Scene as LoadingScene;

        scene?.SetText("Loading");

        Atlas = new Atlas();

        scene?.SetText("Scanning vanilla atlas");
        using (ScopedStopwatch watch = new("Scanning vanilla atlas"))
            await LoadVanillaAtlasAsync();

        scene?.SetText("Scanning Rysy atlas");
        using (ScopedStopwatch watch = new("Scanning Rysy atlas"))
            await Atlas.LoadFromDirectoryAsync("Assets", "Rysy");

        scene?.SetText("Scanning mod dirs");
        using (ScopedStopwatch watch = new("Scanning mod dirs"))
        {
            await Task.WhenAll(
                Directory.GetDirectories(Settings.Instance.ModsDirectory)
                .Select(item => LoadModFromDirAsync(item).AsTask()));
        }

        scene?.SetText("Scanning mod .zips");
        using (ScopedStopwatch watch = new("Scanning mod .zips"))
        {
            await Task.WhenAll(
                Directory.EnumerateFiles(Settings.Instance.ModsDirectory, "*.zip")
                .Select(item => Task.Run(() => LoadModFromZip(item))));
        }
    }

    internal static async ValueTask LoadVanillaAtlasAsync()
    {
        var path = $"{Settings.Instance.CelesteDirectory}/Content/Graphics/Atlases/Gameplay";
        await Atlas.LoadFromPackerAtlasAsync(path);
    }

    // TODO: MOVE
    internal static async ValueTask LoadModFromDirAsync(string modDir)
    {
        modDir = modDir.Replace('\\', '/').TrimEnd('/');

        var gameplayAtlasPath = $"{modDir}/Graphics/Atlases/Gameplay";
        if (Directory.Exists(gameplayAtlasPath))
        {
            await Atlas.LoadFromDirectoryAsync(gameplayAtlasPath);
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

    /// <summary>
    /// Begins the sprite batch with default settings.
    /// </summary>
    public static void BeginBatch() {
        Batch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone);
    }

    /// <summary>
    /// Ends the sprite batch.
    /// </summary>
    public static void EndBatch() { Batch.End(); }
}
