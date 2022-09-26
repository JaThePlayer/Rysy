using Microsoft.Xna.Framework.Graphics;
using Rysy.Graphics.TextureTypes;
using System.IO.Compression;

namespace Rysy.Graphics;

public class VirtTexture : IDisposable
{
    public static VirtTexture FromFile(string filename)
    {
        return new FileVirtTexture(filename);
    }

    public static VirtTexture FromFile(string archiveName, ZipArchiveEntry zip)
    {
        return new ZipVirtTexture(archiveName, zip);
    }

    public static VirtTexture FromTexture(Texture2D text)
    {
        return new UndisposableVirtTexture()
        {
            texture = text,
            state = State.Loaded,
            ClipRect = new(0, 0, text.Width, text.Height)
        };
    }

    /// <summary>
    /// TODO: Something that's a bit cleaner for creating subtextures of VirtTextures
    /// </summary>
    internal static VirtTexture FromAtlasSubtexture(Texture2D parent, Rectangle clipRect, int width, int height)
    {
        return new VanillaTexture()
        {
            texture = parent,
            state = State.Loaded,
            ClipRect = clipRect,
            W = width,
            H = height,
        };
    }

    protected Task? LoadTask;
    protected Texture2D? texture;
    protected State state = State.Unloaded;

    private Rectangle? _clipRect;

    /// <summary>
    /// Gets the ClipRect used for this texture. If the texture is not loaded yet, this will trigger preloading to get the width/height, which will result in additional IO on the main thread.
    /// If only X,Y are needed, use <see cref="ClipRectPos"/> instead to avoid unneeded preloading.
    /// </summary>
    public Rectangle ClipRect
    {
        get
        {
            if (_clipRect.HasValue)
            {
                return _clipRect.Value;
            }

            Logger.Write("VirtTexture.Preload", LogLevel.Info, $"Requested ClipRect, preloading {this}");
            if (TryPreloadClipRect())
            {
                return _clipRect!.Value;
            }

            StartLoadingIfNeeded();
            if (LoadTask is { })
            {
                LoadTask.Wait();
                return _clipRect!.Value;
            }

            Logger.Write("VirtTexture.Preload", LogLevel.Warning, $"Returning null clip rect for {this}!");
            return default;
        }
        protected set
        {
            _clipRect = value;
        }
    }

    /// <summary>
    /// Returns ClipRect.Location if ClipRect is loaded, otherwise returns 0,0.
    /// Doesn't trigger preloading
    /// </summary>
    public Point ClipRectPos => _clipRect?.Location ?? new Point();

    /// <summary>
    /// Returns ClipRect.Width, triggering preloading if the texture is not yet loaded
    /// </summary>
    public virtual int Width => ClipRect.Width;

    /// <summary>
    /// Returns ClipRect.Height, triggering preloading if the texture is not yet loaded
    /// </summary>
    public virtual int Height => ClipRect.Height;

    public Vector2 DrawOffset;

    public Texture2D? Texture => state switch
    {
        State.Unloaded => StartLoadingIfNeeded(),
        State.Loaded => texture!,
        State.Loading => null,
        _ => null,
    };

    private Texture2D? StartLoadingIfNeeded()
    {
        if (state == State.Unloaded)
        {
            state = State.Loading;
            LoadTask = QueueLoad()?.ContinueWith((old) => LoadTask = null);
        }

        return null;
    }

    protected enum State
    {
        Unloaded,
        Loading,
        Loaded
    }

    protected virtual Task? QueueLoad()
    {
        throw new NotImplementedException();
    }

    public virtual void Dispose()
    {
        state = State.Unloaded;
        texture?.Dispose();
    }

    protected virtual bool TryPreloadClipRect() { return false; }

}
