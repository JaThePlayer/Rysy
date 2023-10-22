﻿using Rysy.Graphics.TextureTypes;
using Rysy.Helpers;
using Rysy.Mods;
using Rysy.Scenes;

namespace Rysy.Graphics;

public static class GFX {
    public static IAtlas Atlas { get; private set; } = null!;

    public static SpriteBatch Batch { get; private set; } = null!;

    public static VirtTexture UnknownTexture { get; private set; } = null!;

    public static Texture2D Pixel { get; private set; } = null!;
    public static VirtTexture VirtPixel { get; private set; } = null!;
    public static DecalRegistry DecalRegistry { get; private set; } = null!;

    private static BasicEffect BasicEffect;

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

        if (File.Exists("Assets/Graphics/__fallback.png"))
            UnknownTexture = VirtTexture.FromFile("Assets/Graphics/__fallback.png");
        else
            UnknownTexture = VirtTexture.FromTexture(Pixel);

        PicoFont.Init();

        BasicEffect = new BasicEffect(eng.GraphicsDevice);
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
            Atlas.AddTexture("Rysy:missingTexture", UnknownTexture);
        }

        LoadingScene.Text = "Scanning mod assets";
        using (ScopedStopwatch watch = new("Scanning mods")) {
            await Parallel.ForEachAsync(ModRegistry.Mods.Values, (m, token) => {
                return LoadModAsync(m);
            });
        }

        LoadingScene.Text = "Loading Decal Registry";
        DecalRegistry?.Dispose();
        DecalRegistry = new();
        foreach (var mod in ModRegistry.Mods.Values) {
            DecalRegistry.ReadFileFromMod(mod.Filesystem);
        }
    }

    internal static async ValueTask LoadVanillaAtlasAsync() {
        await Atlas.LoadFromPackerAtlasAsync($"{Profile.Instance.CelesteDirectory}/Content/Graphics/Atlases/Gameplay", noAtlas: false);
        await Atlas.LoadFromPackerAtlasAsync($"{Profile.Instance.CelesteDirectory}/Content/Graphics/Atlases/Misc", noAtlas: true);
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
            BeginBatch(new SpriteBatchState());
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
        Batch.Begin(
            st.SortMode, 
            st.BlendState, 
            st.SamplerState, 
            st.DepthStencilState, 
            st.RasterizerState, 
            st.Effect, 
            st.TransformMatrix
        );
    }

    /// <summary>
    /// Ends the sprite batch, returning the old state
    /// </summary>
    public static SpriteBatchState? EndBatch() { 
        Batch.End();
        if (BatchStateHistory.TryPop(out var last))
            return last;
        return null;
    }

    public static void DrawVertices<T>(Matrix matrix, T[] vertices, int vertexCount, BasicEffect? effect = null, BlendState? blendState = null, RasterizerState? rasterizerState = null) where T : struct, IVertexType {
        effect ??= BasicEffect;
        blendState ??= BlendState.AlphaBlend;
        rasterizerState ??= RasterizerState.CullNone;

        Vector2 vector = new Vector2(RysyEngine.GDM.GraphicsDevice.Viewport.Width, RysyEngine.GDM.GraphicsDevice.Viewport.Height);
        matrix *= Matrix.CreateScale(1f / vector.X * 2f, -(1f / vector.Y) * 2f, 1f);
        matrix *= Matrix.CreateTranslation(-1f, 1f, 0f);

        RysyEngine.Instance.GraphicsDevice.RasterizerState = rasterizerState;
        RysyEngine.Instance.GraphicsDevice.BlendState = blendState;

        //effect.Parameters["World"].SetValue(matrix);
        effect.World = matrix;
        foreach (EffectPass effectPass in effect.CurrentTechnique.Passes) {
            effectPass.Apply();
            RysyEngine.Instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, vertexCount / 3);
        }
    }
}

public record struct SpriteBatchState(
    SpriteSortMode SortMode = SpriteSortMode.Deferred,
    BlendState? BlendState = null,
    SamplerState? SamplerState = null,
    DepthStencilState? DepthStencilState = null,
    RasterizerState? RasterizerState = null,
    Effect? Effect = null,
    Matrix? TransformMatrix = null
) {
    public SpriteBatchState MergeWith(SpriteBatchState last) => new(
        SortMode,
        BlendState ?? last.BlendState,
        SamplerState ?? last.SamplerState,
        DepthStencilState ?? last.DepthStencilState,
        RasterizerState ?? last.RasterizerState,
        Effect ?? last.Effect,
        TransformMatrix ?? last.TransformMatrix
    );
}