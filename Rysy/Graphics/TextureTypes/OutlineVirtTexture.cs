using System.Runtime.CompilerServices;

namespace Rysy.Graphics.TextureTypes;

/// <summary>
/// A texture which can be used to render an outline over an existing texture.
/// Always pitch white.
/// </summary>
public sealed class OutlineVirtTexture : VirtTexture {
    private readonly VirtTexture _parent;
    
    public OutlineVirtTexture(VirtTexture parent) {
        _parent = parent;
        DrawOffset = parent.DrawOffset;
    }

    public override int Width => _parent.Width + 2;

    public override int Height => _parent.Height + 2;

    /*
    private static BlendState _blendState => new BlendState() {
        ColorBlendFunction = BlendFunction.Min,
        ColorSourceBlend = Blend.One,
        ColorDestinationBlend = Blend.One,
        AlphaSourceBlend = Blend.SourceAlpha,
        AlphaDestinationBlend = Blend.DestinationAlpha,
    };
    
    private static BlendState _blendState2 => new BlendState() {
        ColorBlendFunction = BlendFunction.Max,
        ColorSourceBlend = Blend.Zero,
        ColorDestinationBlend = Blend.InverseSourceColor,
        AlphaSourceBlend = Blend.SourceAlpha,
        AlphaDestinationBlend = Blend.DestinationAlpha,
    };

    protected override Task? QueueLoad() {
        return Task.Run(async () => {
            var parent = _parent;

            var t = await parent.ForceGetTexture().ConfigureAwait(false);
            var tw = parent.ClipRect.Width;
            var th = parent.ClipRect.Height;
            
            var dataLoaded = false;
            
            RysyEngine.OnEndOfThisFrame += () => {
                try {
                    var renderCtx = SpriteRenderCtx.Default();
                    var gd = t.GraphicsDevice;
                    var outline = new RenderTarget2D(gd, tw + 2, th + 2, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
                    var outline2 = new RenderTarget2D(gd, tw + 2, th + 2, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
                    gd.SetRenderTarget(outline);
                    GFX.BeginBatch(new SpriteBatchState(BlendState: _blendState));

                    var pos = parent.DrawOffset + Vector2.One;
                    ISprite.FromTexture(pos + new Vector2(-1f, 0f), parent).RenderWithColor(renderCtx, Color.White);
                    ISprite.FromTexture(pos + new Vector2(1f, 0f), parent).RenderWithColor(renderCtx, Color.White);
                    ISprite.FromTexture(pos + new Vector2(0f, 1f), parent).RenderWithColor(renderCtx, Color.White);
                    ISprite.FromTexture(pos + new Vector2(0f, -1f), parent).RenderWithColor(renderCtx, Color.White);
                    
                    GFX.EndBatch();
                    
                    gd.SetRenderTarget(outline2);
                    GFX.BeginBatch(new SpriteBatchState(BlendState: _blendState2));
                    GFX.Batch.Draw(outline, Vector2.Zero, Color.White);
                    GFX.EndBatch();
                    
                    gd.SetRenderTarget(null);
                    dataLoaded = true;
                    
                    lock (this) {
                        _texture = outline2;
                        ClipRect = new(0, 0, _texture!.Width, _texture.Height);
                        _state = State.Loaded;
                    }
                } catch (Exception ex) {
                    Logger.Write("OutlineSprite", LogLevel.Error, ex.ToString());
                    dataLoaded = true;
                }
            };
            
            while (!dataLoaded) {
                await Task.Delay(20);
            }
        });
    }*/
    
    
    protected override Task? QueueLoad() {
        return Task.Run(async () => {
            var parent = _parent;

            var t = await parent.ForceGetTexture().ConfigureAwait(false);
            var tw = parent.ClipRect.Width;
            var th = parent.ClipRect.Height;
            var tData = new Color[tw * th];
            var dataLoaded = false;

            RysyEngine.OnEndOfThisFrame += () => {
                try {
                    t.GetData(0, parent.ClipRect, tData, 0, tData.Length);
                    dataLoaded = true;
                } catch (Exception ex) {
                    Logger.Write("OutlineSprite", LogLevel.Error, ex.ToString());
                    dataLoaded = true;
                }
            };

            while (!dataLoaded) {
                await Task.Delay(20);
            }

            var outHeight = th + 2;
            var outWidth = tw + 2;
            var outData = new Color[outWidth * outHeight];

            for (int x = 0; x < tw; x++) {
                for (int y = 0; y < th; y++) {
                    var inPos = x + y * tw;

                    if (inPos > 0 && inPos < tData.Length && tData[inPos] != Color.Transparent) {
                        TrySet(x + 1, y, outWidth, outData);
                        TrySet(x - 1, y, outWidth, outData);
                        TrySet(x, y + 1, outWidth, outData);
                        TrySet(x, y - 1, outWidth, outData);

                        [MethodImpl(MethodImplOptions.AggressiveInlining)]
                        static void TrySet(int x, int y, int w, Color[] outData) {
                            var outPos = (x + 1) + (y + 1) * w;

                            if (outPos > 0 && outPos < outData.Length)
                                outData[outPos] = Color.White;
                        }
                    }
                }
            }

            try {
                var outline = new Texture2D(t.GraphicsDevice, tw + 2, th + 2);
                outline.SetData(outData);

                lock (this) {
                    _texture = outline;
                    ClipRect = new(0, 0, _texture!.Width, _texture.Height);
                    _state = State.Loaded;
                }
            } catch (Exception ex) {
                Logger.Write("OutlineSprite", LogLevel.Error, ex.ToString());
            }
        });
    }
}