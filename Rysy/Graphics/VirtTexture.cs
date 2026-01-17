using Rysy.Graphics.TextureTypes;
using System.Diagnostics;
using System.IO.Compression;

namespace Rysy.Graphics;

public class VirtTexture : IDisposable {
    protected Task? LoadTask;
    
    /// <summary>
    /// If the texture is already loaded, contains the loaded texture.
    /// </summary>
    protected Texture2D? LoadedTexture;
    
    protected States State = States.Unloaded;

    public Vector2 DrawOffset { get; set; }
    
    /// <summary>
    /// If the texture is already loaded, contains its ClipRect.
    /// </summary>
    protected Rectangle? LoadedClipRect;

    protected OutlineVirtTexture? OutlineTexture;

    public static VirtTexture FromTexture(Texture2D text) {
        return new UndisposableVirtTexture() {
            LoadedTexture = text,
            State = States.Loaded,
            ClipRect = new(0, 0, text.Width, text.Height)
        };
    }

    /// <summary>
    /// TODO: Something that's a bit cleaner for creating subtextures of VirtTextures
    /// </summary>
    public static VirtTexture FromAtlasSubtexture(Texture2D parent, Rectangle clipRect, int width, int height) {
        return new VanillaTexture() {
            LoadedTexture = parent,
            State = States.Loaded,
            ClipRect = clipRect,
            W = width,
            H = height,
        };
    }

    /// <summary>
    /// Gets the ClipRect used for this texture. If the texture is not loaded yet, this will trigger preloading to get the width/height, which will result in additional IO on the main thread.
    /// If only X,Y are needed, use <see cref="ClipRectPos"/> instead to avoid unneeded preloading.
    /// </summary>
    public Rectangle ClipRect {
        get {
            if (LoadedClipRect.HasValue) {
                return LoadedClipRect.Value;
            }

            if (Settings.Instance?.LogPreloadingTextures ?? false) {
                Logger.Write("VirtTexture.Preload", LogLevel.Info, $"Preloading {this}");
                // Logger.Write("VirtTexture.Preload", LogLevel.Debug, new StackTrace().ToString());
            }
            if (TryPreloadClipRect()) {
                return LoadedClipRect!.Value;
            }

            StartLoadingIfNeeded();
            if (LoadTask is { }) {
                LoadTask.Wait();
                return LoadedClipRect!.Value;
            }

            Logger.Write("VirtTexture.Preload", LogLevel.Warning, $"Returning null clip rect for {this}!");
            return default;
        }
        protected set => LoadedClipRect = value;
    }

    /// <summary>
    /// Returns ClipRect.Location if ClipRect is loaded, otherwise returns 0,0.
    /// Doesn't trigger preloading
    /// </summary>
    public Point ClipRectPos => LoadedClipRect?.Location ?? new Point();

    /// <summary>
    /// Returns ClipRect.Width, triggering preloading if the texture is not yet loaded
    /// </summary>
    public virtual int Width => ClipRect.Width;

    /// <summary>
    /// Returns ClipRect.Height, triggering preloading if the texture is not yet loaded
    /// </summary>
    public virtual int Height => ClipRect.Height;

    public Texture2D? Texture => State switch {
        States.Unloaded => StartLoadingIfNeeded(),
        States.Loaded => LoadedTexture!,
        States.Loading => null,
        _ => null,
    };

    public virtual Rectangle GetSubtextureRect(int x, int y, int w, int h, out Vector2 drawOffset, Rectangle? clipRect = null) {
        drawOffset = Vector2.Zero;
        var clipRectPos = clipRect?.Location ?? ClipRectPos;

        var newY = clipRectPos.Y + y;
        var newX = clipRectPos.X + x;

        return new(newX, newY, w, h);
    }

    /// <summary>
    /// Gets a cached texture that can be used to render an outline for this texture.
    /// </summary>
    public VirtTexture GetOutlineTexture() {
        return OutlineTexture ??= new OutlineVirtTexture(this);
    }

    private Texture2D? StartLoadingIfNeeded() {
        if (Interlocked.CompareExchange(ref State, States.Loading, States.Unloaded) == States.Unloaded) {
            LoadTask = QueueLoad()?.ContinueWith((old) => LoadTask = null);
        }

        return null;
    }

    protected enum States {
        Unloaded,
        Loading,
        Loaded
    }

    protected virtual Task? QueueLoad() {
        throw new NotImplementedException();
    }

    public virtual void Dispose() {
        State = States.Unloaded;
        Texture?.Dispose();
        OutlineTexture?.Dispose();
    }

    protected virtual bool TryPreloadClipRect() { return false; }


    /// <summary>
    /// Forcefully loads the texture and waits for it to finish loading before returning it
    /// </summary>
    /// <returns></returns>
    public async ValueTask<Texture2D> ForceGetTexture() {

        switch (State) {
            case States.Loaded:
                return LoadedTexture!;
            case States.Unloaded:
                StartLoadingIfNeeded();
                goto loading;
            case States.Loading:
            loading:

                if (LoadTask is { } && (!LoadTask.IsCompleted))
                    await LoadTask;

                return LoadedTexture!;
        }
        throw new Exception($"Unknown state: {State}");
    }
}
