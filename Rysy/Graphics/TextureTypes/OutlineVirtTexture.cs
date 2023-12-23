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