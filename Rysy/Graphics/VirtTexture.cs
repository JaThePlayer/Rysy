using Rysy.Graphics.TextureTypes;
using System.Diagnostics;
using System.IO.Compression;

namespace Rysy.Graphics;

public class VirtTexture : IDisposable {
    protected Task? LoadTask;
    protected Texture2D? _texture;
    protected State _state = State.Unloaded;

    public Vector2 DrawOffset { get; set; }
    
    protected Rectangle? _clipRect;

    private OutlineVirtTexture? _outlineTexture;
    
    public static VirtTexture FromFile(string filename) {
        return new FileVirtTexture(filename);
    }

    public static VirtTexture FromFile(string archiveName, ZipArchiveEntry zip) {
        return new ZipVirtTexture(archiveName, zip);
    }

    public static VirtTexture FromTexture(Texture2D text) {
        return new UndisposableVirtTexture() {
            _texture = text,
            _state = State.Loaded,
            ClipRect = new(0, 0, text.Width, text.Height)
        };
    }

    /// <summary>
    /// TODO: Something that's a bit cleaner for creating subtextures of VirtTextures
    /// </summary>
    public static VirtTexture FromAtlasSubtexture(Texture2D parent, Rectangle clipRect, int width, int height) {
        return new VanillaTexture() {
            _texture = parent,
            _state = State.Loaded,
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
            if (_clipRect.HasValue) {
                return _clipRect.Value;
            }

            if (Settings.Instance?.LogPreloadingTextures ?? false) {
                Logger.Write("VirtTexture.Preload", LogLevel.Info, $"Preloading {this}");
                // Logger.Write("VirtTexture.Preload", LogLevel.Debug, new StackTrace().ToString());
            }
            if (TryPreloadClipRect()) {
                return _clipRect!.Value;
            }

            StartLoadingIfNeeded();
            if (LoadTask is { }) {
                LoadTask.Wait();
                return _clipRect!.Value;
            }

            Logger.Write("VirtTexture.Preload", LogLevel.Warning, $"Returning null clip rect for {this}!");
            return default;
        }
        protected set => _clipRect = value;
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

    public Texture2D? Texture => _state switch {
        State.Unloaded => StartLoadingIfNeeded(),
        State.Loaded => _texture!,
        State.Loading => null,
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
        return _outlineTexture ??= new OutlineVirtTexture(this);
    }

    private Texture2D? StartLoadingIfNeeded() {
        if (_state == State.Unloaded) {
            _state = State.Loading;
            LoadTask = QueueLoad()?.ContinueWith((old) => LoadTask = null);
        }

        return null;
    }

    protected enum State {
        Unloaded,
        Loading,
        Loaded
    }

    protected virtual Task? QueueLoad() {
        throw new NotImplementedException();
    }

    public virtual void Dispose() {
        _state = State.Unloaded;
        _texture?.Dispose();
        _outlineTexture?.Dispose();

        GC.SuppressFinalize(this);
    }

    protected virtual bool TryPreloadClipRect() { return false; }


    /// <summary>
    /// Forcefully loads the texture and waits for it to finish loading before returning it
    /// </summary>
    /// <returns></returns>
    public async ValueTask<Texture2D> ForceGetTexture() {

        switch (_state) {
            case State.Loaded:
                return _texture!;
            case State.Unloaded:
                StartLoadingIfNeeded();
                goto loading;
            case State.Loading:
            loading:

                if (LoadTask is { } && (!LoadTask.IsCompleted))
                    await LoadTask;

                return _texture!;
        }
        throw new Exception($"Unknown state: {_state}");
    }
}
