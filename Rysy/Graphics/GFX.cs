using Rysy.Extensions;
using Rysy.Graphics.TextureTypes;
using Rysy.Mods;
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
            _ValidDecalPaths = Atlas.GetTextures().Where(p => p.virtPath.StartsWith("decals/", StringComparison.Ordinal)).Select(p => {
                return p.virtPath["decals/".Length..].RegexReplace(Decal.NumberTrimEnd, string.Empty);


            }).Distinct().ToList();

            Atlas.OnTextureLoad += (a) => _ValidDecalPaths = null!;
            Atlas.OnUnload += () => _ValidDecalPaths = null!;

            return _ValidDecalPaths;
        }
    }

    /// <summary>
    /// Loads the bare minimum needed to render anything.
    /// </summary>
    /// <param name="eng"></param>
    public static void LoadEssencials(RysyEngine eng) {
        if (Batch is not null)
            return;

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
    public static async ValueTask LoadAsync() {
        if (Atlas is { } oldAtlas) {
            oldAtlas.DisposeTextures();
        }

        LoadingScene.Text = "Loading";

        Atlas = new Atlas();

        LoadingScene.Text = "Reading vanilla atlas";
        using (ScopedStopwatch watch = new("Reading vanilla atlas"))
            await LoadVanillaAtlasAsync();

        LoadingScene.Text = "Scanning Rysy assets";
        using (ScopedStopwatch watch = new("Scanning Rysy assets")) {
            await Atlas.LoadFromDirectoryAsync("Assets/Graphics", "Rysy");
            Atlas.AddTexture("Rysy:1x1-tinting-pixel", VirtPixel);
            Atlas.AddTexture("tilesets/subfolder/betterTemplate", Atlas["Rysy:tilesets/subfolder/betterTemplate"]);
        }

        LoadingScene.Text = "Scanning mod assets";
        using (ScopedStopwatch watch = new("Scanning mods")) {
            await Parallel.ForEachAsync(ModRegistry.Mods.Values, (m, token) => {
                return LoadModAsync(m);
            });
        }
    }

    internal static async ValueTask LoadVanillaAtlasAsync() {
        var path = $"{Profile.Instance.CelesteDirectory}/Content/Graphics/Atlases/Gameplay";
        await Atlas.LoadFromPackerAtlasAsync(path);
    }

    internal static ValueTask LoadModAsync(ModMeta mod, bool registerFilewatch = true) {
        var files = mod.Filesystem.FindFilesInDirectoryRecursive("Graphics/Atlases/Gameplay", "png");
        var atlas = Atlas;

        foreach (var file in files) {
            AddTexture(file);
        }

        if (registerFilewatch)
        mod.Filesystem.RegisterFilewatch("Graphics/Atlases/Gameplay", new() {
            OnChanged = (string newPath) => {
                var list = atlas.GetTextures()
                    .Where(p => p.texture is ModTexture { Mod: var textureMod } && textureMod == mod)
                    .Select(p => p.virtPath)
                    .ToList();

                // todo: make this smarter
                atlas.RemoveTextures(list);
                LoadModAsync(mod, registerFilewatch: false).AsTask().Wait();
                /*
                if (newPath.FileExtension() is ".png") {
                    AddTexture(newPath);
                }*/
            }
        });

        return ValueTask.CompletedTask;

        void AddTexture(string path) {
            var virt = path["Graphics/Atlases/Gameplay/".Length..(^".png".Length)];

            atlas!.AddTexture(virt, new ModTexture(mod, path));
        }
    }

    /// <summary>
    /// Begins the sprite batch with default settings.
    /// </summary>
    public static void BeginBatch() {
        Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone);
    }

    public static void BeginBatch(Camera? camera) {
        if (camera is { })
            Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, camera.Matrix);
        else
            BeginBatch();
    }

    /// <summary>
    /// Ends the sprite batch.
    /// </summary>
    public static void EndBatch() { Batch.End(); }
}
