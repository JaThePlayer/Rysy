namespace Rysy.Graphics;

public class DynamicAtlas : IAtlas {
    public Dictionary<string, VirtTexture> Textures { get; private set; } = new(StringComparer.InvariantCultureIgnoreCase);

    protected RenderTarget2D? _packed;

    public int Width, Height;

    private List<Rectangle> Areas = new();

    public event Action<string> OnTextureLoad;
    public event Action OnUnload;

    public DynamicAtlas(int width, int height) {
        (Width, Height) = (width, height);
        Areas.Add(new Rectangle(0, 0, Width, Height));
    }

    internal async ValueTask<bool> PackTexture(string key, VirtTexture texture) {
        var needsClear = false;
        var rTexture = await texture.ForceGetTexture();

        var w = texture.Width;
        var h = texture.Height;
        Point? pos = default;

        lock (Areas) {
            foreach (var area in Areas) {
                var aw = area.Width;
                var ah = area.Height;
                if (w <= aw && h <= ah) {
                    pos = area.Location;
                    Areas.Remove(area);
                    // cut up the area
                    if (w == aw) {
                        // took up all horizontal space, so we only need 1 vertical area
                        Areas.Add(new Rectangle(area.Location.X, area.Location.Y + h, aw, ah - h));
                    } else if (h == ah) {
                        // took up all vertical space, so we only need 1 horizontal area
                        Areas.Add(new Rectangle(area.Location.X + w, area.Location.Y, aw - w, ah));
                    } else {
                        Areas.Add(new Rectangle(area.Location.X + w, area.Location.Y, aw - w, h));
                        Areas.Add(new Rectangle(area.Location.X, area.Location.Y + h, aw, ah - h));
                    }


                    break;
                }
            }

            if (pos is null) {
                Console.WriteLine("NOT ENOUGH SPACE");
                return false;
            }

            Areas = Areas.OrderBy(r => r.Width * r.Height).ToList();

            if (_packed is null) {
                _packed = new(RysyEngine.GDM.GraphicsDevice, Width, Height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
                needsClear = true;
            }

            Textures.Add(key, VirtTexture.FromAtlasSubtexture(_packed, new(pos.Value.X, pos.Value.Y, rTexture.Width, rTexture.Height), rTexture.Width, rTexture.Height));
        }

        RysyEngine.OnFrameEnd += () => PackTextureCallback(texture, pos.Value.ToVector2(), needsClear);

        return true;
    }

    private void PackTextureCallback(VirtTexture texture, Vector2 pos, bool needsClear) {
        var b = GFX.Batch;
        var gd = RysyEngine.GDM.GraphicsDevice;

        lock (_packed) {
            gd.SetRenderTarget(_packed);
            if (needsClear) {
                gd.Clear(Color.Transparent);
            }
            b.Begin();

            ISprite.FromTexture(pos, texture).Render();


            b.End();
            gd.SetRenderTarget(null);
        }


    }

    public VirtTexture this[string key] {
        get {
            if (Textures.TryGetValue(key, out var texture)) {
                return texture;
            }

            Logger.Write("DynamicAtlas", LogLevel.Warning, $"Tried to access texture {key} that doesn't exist!");
            return GFX.VirtPixel;
        }
    }

    public void DisposeTextures() {
        _packed?.Dispose();
        _packed = null;

        OnUnload?.Invoke();
    }

    internal void DebugRender() {
        if (_packed is null) {
            return;
        }

        var pos = Vector2.Zero;
        ISprite.Rect(pos, _packed.Width, _packed.Height, Color.Gray * 0.8f).Render();
        GFX.Batch.Draw(_packed, pos, Color.White);

        foreach (var item in Areas) {
            ISprite.OutlinedRect(item.Location.ToVector2(), item.Width, item.Height, Color.White, Color.Pink * 0.8f).Render();
        }
    }

    public void AddTexture(string virtPath, VirtTexture texture) {
        PackTexture(virtPath, texture);

        OnTextureLoad?.Invoke(virtPath);
    }

    public IEnumerable<(string virtPath, VirtTexture texture)> GetTextures() => Textures.Select(t => (t.Key, t.Value));
}
