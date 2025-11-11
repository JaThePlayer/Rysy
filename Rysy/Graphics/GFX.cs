using Rysy.Graphics.TextureTypes;
using Rysy.Helpers;
using Rysy.Loading;
using Rysy.Mods;
using Rysy.Platforms;
using Rysy.Scenes;

namespace Rysy.Graphics;

public static class Gfx {
    public static IAtlas Atlas { get; set; } = new Atlas();

    public static SpriteBatch Batch { get; private set; } = null!;

    public static VirtTexture UnknownTexture { get; private set; } = null!;

    public static Texture2D Pixel { get; private set; } = null!;
    public static VirtTexture VirtPixel { get; private set; } = null!;
    public static DecalRegistry DecalRegistry { get; private set; } = null!;

    private static BasicEffect BasicEffect;

    /// <summary>
    /// Sets up the bare minimum for running headless
    /// </summary>
    internal static void HeadlessSetup() {
        Atlas = new Atlas();
        DecalRegistry = new();
        UnknownTexture = new();
    }
    
    /// <summary>
    /// Loads the bare minimum needed to render anything.
    /// </summary>
    public static void LoadEssencials() {
        if (Batch is not null)
            return;

        Batch = new(RysyState.GraphicsDevice);

        Pixel = new(RysyState.GraphicsDevice, 1, 1);
        Pixel.SetData(new Color[1] { Color.White });
        VirtPixel = VirtTexture.FromTexture(Pixel);


        if (!RysyPlatform.Current.GetRysyFilesystem().TryOpenFile("Graphics/__fallback.png", s => {
            UnknownTexture = VirtTexture.FromTexture(Texture2D.FromStream(RysyState.GraphicsDevice, s));
        })) {
            UnknownTexture = VirtTexture.FromTexture(Pixel);
        }

        PicoFont.Init();

        BasicEffect = new BasicEffect(RysyState.GraphicsDevice);
        BasicEffect.World = Matrix.Identity;
        BasicEffect.View = Matrix.Identity;
        BasicEffect.Projection = Matrix.Identity;
        BasicEffect.TextureEnabled = false;
        BasicEffect.VertexColorEnabled = true;
    }

    /// <summary>
    /// Loads all textures, including those from mods.
    /// </summary>
    /// <returns></returns>
    public static async ValueTask LoadAsync(SimpleLoadTask? task) {
        if (Atlas is { } oldAtlas) {
            oldAtlas.DisposeTextures();
        }

        task?.SetMessage("Loading");

        Atlas = new Atlas();

        task?.SetMessage("Reading vanilla atlas");
        using (ScopedStopwatch watch = new("Reading vanilla atlas"))
            await LoadVanillaAtlasAsync();

        task?.SetMessage("Scanning Rysy assets");
        await LoadRysyTextures();

        task?.SetMessage("Scanning mod assets");
        using (ScopedStopwatch watch = new("Scanning mod assets")) {
            await Parallel.ForEachAsync(ModRegistry.Mods.Values, (m, token) => {
                if (m == ModRegistry.RysyMod)
                    return ValueTask.CompletedTask;
                return LoadModAsync(m);
            });
        }
    }

    public static void LoadDecalRegistry(SimpleLoadTask? task) {
        task?.SetMessage("Loading Decal Registry");
        DecalRegistry?.Dispose();
        DecalRegistry = new();
        using ScopedStopwatch watch = new("Loading Decal Registry");
        
        foreach (var mod in ModRegistry.Mods.Values) {
            DecalRegistry.ReadFileFromMod(mod);
        }
    }

    public static async Task LoadRysyTextures() {
        using (ScopedStopwatch watch = new("Scanning Rysy assets")) {
            //await Atlas.LoadFromDirectoryAsync("Assets/Graphics", "Rysy");
            await LoadModAsync(ModRegistry.RysyMod, registerFilewatch: false, folder: "Graphics", prefix: "Rysy");
            Atlas.AddTexture("Rysy:1x1-tinting-pixel", VirtPixel);
            Atlas.AddTexture("tilesets/subfolder/betterTemplate", Atlas["Rysy:tilesets/subfolder/betterTemplate"]);
            Atlas.AddTexture("Rysy:missingTexture", UnknownTexture);
            
            // provide shims for lonn plugins that hardcode @Internal@/ paths. We'll do this here instead of
            // lonn plugin handling code, to avoid running extra string conversions on all texture paths for this one edge case
            ProvideLonnShim("core_message");
            ProvideLonnShim("cutscene_node");
            ProvideLonnShim("lava_sandwich");
            ProvideLonnShim("northern_lights");
            ProvideLonnShim("rising_lava");
            ProvideLonnShim("sound_source");
            ProvideLonnShim("summit_background_manager");
            ProvideLonnShim("summit_gem_manager");
            ProvideLonnShim("tentacles");
            ProvideLonnShim("dust_creature_outlines/base00");
            ProvideLonnShim("dust_creature_outlines/base01");
            ProvideLonnShim("dust_creature_outlines/base02");
            ProvideLonnShim("dust_creature_outlines/center00");
            ProvideLonnShim("1x1-tinting-pixel");
            Atlas.AddTexture("@Internal@/missing_image", Atlas["Rysy:__fallback"]);

            void ProvideLonnShim(string virtPathNoPrefix) {
                Atlas.AddTexture($"@Internal@/{virtPathNoPrefix}", Atlas[$"Rysy:{virtPathNoPrefix}"]);
            }
        }
    }

    internal static async ValueTask LoadVanillaAtlasAsync() {
        await Atlas.LoadFromPackerAtlasAsync($"{Profile.Instance.CelesteDirectory}/Content/Graphics/Atlases/Gameplay", noAtlas: false);
        await Atlas.LoadFromPackerAtlasAsync($"{Profile.Instance.CelesteDirectory}/Content/Graphics/Atlases/Misc", noAtlas: true);
    }

    internal static ValueTask LoadModAsync(ModMeta mod, bool registerFilewatch = true, string folder = "Graphics/Atlases/Gameplay", string? prefix = null) {
        var files = mod.Filesystem.FindFilesInDirectoryRecursive(folder, "png");
        var atlas = Atlas;

        foreach (var file in files) {
            AddTexture(file);
        }

        if (registerFilewatch)
            mod.Filesystem.RegisterFilewatch(folder, new() {
                // ModTexture has its own filewatch for handling updates, we just need to look for new and deleted textures.
                OnCreated = AddTexture,
                OnRemoved = newPath => {
                    atlas.RemoveTextures(newPath[(folder.Length+1)..^".png".Length]);
                }
            });

        return ValueTask.CompletedTask;

        void AddTexture(string path) {
            var virt = path[(folder.Length+1)..^".png".Length];

            if (prefix is { }) {
                virt = $"{prefix}:{virt}";
            }
            
#pragma warning disable CA2000 // Dispose objects before losing scope - scope is not lost here
            atlas!.AddTexture(virt, new ModTexture(mod, path));
#pragma warning restore CA2000
        }
    }

    /// <summary>
    /// Begins the sprite batch with default settings.
    /// </summary>
    public static void BeginBatch() {
        BeginBatch(new SpriteBatchState(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone));
    }

    public static void BeginBatch(Camera? camera) {
        if (camera is { })
            BeginBatch(new SpriteBatchState(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, camera.Matrix));
        else
            BeginBatch(new SpriteBatchState(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone));
    }

    private static Stack<SpriteBatchState> BatchStateHistory = new(2);

    public static SpriteBatchState GetCurrentBatchState() {
        return BatchStateHistory.Peek();
    }

    public static void BeginBatch(SpriteBatchState? state) {
        var st = state ?? new();

        BeginBatchNoPush(ref st);
        //SpriteBatchState? last = BatchStateHistory.TryPeek(out var l) ? l : null;
        BatchStateHistory.Push(st);

        //return last;
    }

    private static void BeginBatchNoPush(ref SpriteBatchState st) {
        if (st.ScissorRect is { } scissor)
            Batch.GraphicsDevice.ScissorRectangle = scissor;
        else
            Batch.GraphicsDevice.ScissorRectangle = default;

        Batch.Begin(
            st.SortMode, 
            st.BlendState, 
            st.SamplerState, 
            st.DepthStencilState, 
            st.RasterizerState, 
            st.Effect, 
            #if FNA
            st.TransformMatrix ?? Matrix.Identity
            #else
            st.TransformMatrix
            #endif
        );
    }

    /// <summary>
    /// Ends the sprite batch, returning the old state
    /// </summary>
    public static SpriteBatchState? EndBatch() { 
        Batch.End();
        if (BatchStateHistory.TryPop(out var last)) {
            if (last.ScissorRect is { } scissor)
                Batch.GraphicsDevice.ScissorRectangle = default;
            return last;
        }
        return null;
    }

    public static void DrawVertices<T>(Matrix matrix, T[] vertices, int vertexCount, BasicEffect? effect = null, BlendState? blendState = null, RasterizerState? rasterizerState = null) where T : struct, IVertexType {
        effect ??= BasicEffect;
        blendState ??= BlendState.AlphaBlend;
        rasterizerState ??= RasterizerState.CullNone;

        Vector2 vector = new Vector2(RysyState.GraphicsDevice.Viewport.Width, RysyState.GraphicsDevice.Viewport.Height);
        matrix *= Matrix.CreateScale(1f / vector.X * 2f, -(1f / vector.Y) * 2f, 1f);
        matrix *= Matrix.CreateTranslation(-1f, 1f, 0f);

        RysyState.GraphicsDevice.RasterizerState = rasterizerState;
        RysyState.GraphicsDevice.BlendState = blendState;

        //effect.Parameters["World"].SetValue(matrix);
        effect.World = matrix;
        foreach (EffectPass effectPass in effect.CurrentTechnique.Passes) {
            effectPass.Apply();
            RysyState.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, vertexCount / 3);
        }
    }

    private static readonly Dictionary<Uri, Task<VirtTexture>> WebTextures = new();

    /// <summary>
    /// Gets a cached texture from the given <paramref name="uri"/>.
    /// If the texture hasn't been downloaded yet, null is returned, and a background task to download it will be started if needed.
    /// </summary>
    public static VirtTexture? GetTextureFromWebIfReady(Uri uri) {
        lock (WebTextures) {
            if (WebTextures.TryGetValue(uri, out var cached)) {
                if (!cached.IsCompletedSuccessfully)
                    return null;
                return cached.Result;
            }

            WebTextures[uri] = Task.Run(async () => await CacheWebTexture(uri, new CancellationToken()));
        }

        return null;
    }
    
    /// <summary>
    /// Asynchronously get a texture from a web location. Cached. 
    /// </summary>
    public static Task<VirtTexture> GetTextureFromWebAsync(Uri uri, CancellationToken token) {
        lock (WebTextures) {
            if (WebTextures.TryGetValue(uri, out var cached)) {
                return cached;
            }

            return WebTextures[uri] = CacheWebTexture(uri, token);
        }
    }
    
    private static async Task<VirtTexture> CacheWebTexture(Uri uri, CancellationToken token) {
        Logger.Write("GFX.WebTexture", LogLevel.Info, $"Loading texture from {uri}");
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        MemoryStream bytesRead = new();
        try {
            var bytes = await client.GetStreamAsync(uri, token);
            await bytes.CopyToAsync(bytesRead, token);
        } catch (Exception e) {
            Logger.Write("GFX.WebTexture", LogLevel.Warning, $"Failed to download texture from {uri}: {e}");
            return VirtPixel;
        }

        Texture2D? texture = null;
        RysyState.OnEndOfThisFrame += () => {
            try {
                texture = Texture2D.FromStream(Batch.GraphicsDevice, bytesRead);
                Logger.Write("GFX.WebTexture", LogLevel.Info, $"Successfully loaded texture from {uri}");
            } catch (Exception e) {
                Logger.Write("GFX.WebTexture", LogLevel.Warning, $"Failed to load texture from {uri}: {e}");
                texture = VirtPixel.Texture;
            } finally {
                bytesRead.Dispose();
            }
        };

        while (texture is null) {
            await Task.Delay(16, token);
        }
        
        return VirtTexture.FromTexture(texture);
    }
}

public record struct SpriteBatchState(
    SpriteSortMode SortMode = SpriteSortMode.Deferred,
    BlendState? BlendState = null,
    SamplerState? SamplerState = null,
    DepthStencilState? DepthStencilState = null,
    RasterizerState? RasterizerState = null,
    Effect? Effect = null,
    Matrix? TransformMatrix = null,
    Rectangle? ScissorRect = null
) {
}