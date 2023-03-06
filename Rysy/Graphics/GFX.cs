using Rysy.Scenes;
using System.IO.Compression;

namespace Rysy.Graphics;

public static class GFX {
    public static IAtlas Atlas { get; private set; } = null!;

    public static SpriteBatch Batch { get; private set; } = null!;

    public static Texture2D Pixel { get; private set; } = null!;
    public static VirtTexture VirtPixel { get; private set; } = null!;

    private static List<string> _ValidDecalPaths;
    public static List<string> ValidDecalPaths {
        get {
            if (_ValidDecalPaths is { } p)
                return p;
            _ValidDecalPaths = Atlas.GetTextures().Where(p => p.virtPath.StartsWith("decals/", StringComparison.Ordinal)).Select(p => p.virtPath["decals/".Length..]).ToList();

            Atlas.OnTextureLoad += (a) => _ValidDecalPaths = null!;
            Atlas.OnUnload += () => _ValidDecalPaths = null!;

            return _ValidDecalPaths;
        }
    }

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
    internal static async ValueTask LoadAsync() {
        LoadingScene? scene = RysyEngine.Scene as LoadingScene;

        if (Atlas is { } oldAtlas) {
            oldAtlas.DisposeTextures();
        }

        scene?.SetText("Loading");

        Atlas = new Atlas();

        scene?.SetText("Reading vanilla atlas");
        using (ScopedStopwatch watch = new("Reading vanilla atlas"))
            await LoadVanillaAtlasAsync();

        scene?.SetText("Scanning Rysy assets");
        using (ScopedStopwatch watch = new("Scanning Rysy assets")) {
            await Atlas.LoadFromDirectoryAsync("Assets/Graphics", "Rysy");
            Atlas.AddTexture("Rysy:1x1-tinting-pixel", VirtPixel);
        }

        scene?.SetText("Scanning mod dirs");
        using (ScopedStopwatch watch = new("Scanning mod dirs")) {
            await Parallel.ForEachAsync(Directory.GetDirectories(Profile.Instance.ModsDirectory), (dir, token) => {
                return LoadModFromDirAsync(dir);
            });
        }

        scene?.SetText("Scanning mod .zips");
        using (ScopedStopwatch watch = new("Scanning mod .zips")) {
            await Parallel.ForEachAsync(Directory.EnumerateFiles(Profile.Instance.ModsDirectory, "*.zip"), (zip, token) => {
                return LoadModFromZip(zip);
            });

            /*
                await Task.WhenAll(
                    Directory.EnumerateFiles(Profile.Instance.ModsDirectory, "*.zip")
                    .Select(item => Task.Run(() => LoadModFromZip(item))));*/
        }
    }

    internal static async ValueTask LoadVanillaAtlasAsync() {
        var path = $"{Profile.Instance.CelesteDirectory}/Content/Graphics/Atlases/Gameplay";
        await Atlas.LoadFromPackerAtlasAsync(path);
    }

    // TODO: MOVE
    internal static async ValueTask LoadModFromDirAsync(string modDir) {
        modDir = modDir.Replace('\\', '/').TrimEnd('/');

        var gameplayAtlasPath = $"{modDir}/Graphics/Atlases/Gameplay";
        if (Directory.Exists(gameplayAtlasPath)) {
            await Atlas.LoadFromDirectoryAsync(gameplayAtlasPath);
        }
    }

    internal static async ValueTask LoadModFromZip(string modZipPath) {
        var stream = File.OpenRead(modZipPath);
        var zip = new ZipArchive(stream, ZipArchiveMode.Read, false);

        await Atlas.LoadFromZip(modZipPath, zip);
    }

    /*
    public static string ToVirtPath(this string val, string prefix = "") {
        var ext = Path.GetExtension(val) switch {
            "" or null => ".png",
            var other => other,
        };
        var v = val.Replace('\\', '/').Replace(ext, "").Trim('/');
        if (!string.IsNullOrWhiteSpace(prefix))
            v = $"{prefix}:{v}";

        return v;
    }*/

    public static string ToVirtPath(this ReadOnlySpan<char> val, string prefix = "") {
        var ext = Path.GetExtension(val);
        if (ext.IsEmpty)
            return val.ToString();

        // Trim trailing slash
        var vLen = val.Length - ext.Length;
        if (val.Length > 1 && val[vLen - 1] is '\\' or '/') {
            vLen--;
        }

        Span<char> b = stackalloc char[vLen];
        val[0..vLen].CopyTo(b);
        b.Replace('\\', '/');

        if (!string.IsNullOrWhiteSpace(prefix))
            return $"{prefix}:{b}";

        return b.ToString().Replace('\\', '/');
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
