using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming
// ReSharper disable FieldCanBeMadeReadOnly.Local

namespace Rysy.Graphics;

/// <summary>
/// A copy-paste of FNA's SpriteBatch implementation, that's more efficient than Monogame's
/// </summary>
public sealed class FnaSpriteBatch : IDisposable {
    private const int MAX_SPRITES = 2048;
    private const int MAX_VERTICES = 8192;
    private const int MAX_INDICES = 12288;
    
    private static readonly float[] axisDirectionX = new float[4] { -1f, 1f, -1f, 1f };
    private static readonly float[] axisDirectionY = new float[4] { -1f, -1f, 1f, 1f };
    private static readonly float[] axisIsMirroredX = new float[4] { 0.0f, 1f, 0.0f, 1f };
    private static readonly float[] axisIsMirroredY = new float[4] { 0.0f, 0.0f, 1f, 1f };
    private static readonly float[] CornerOffsetX = new float[4] { 0.0f, 1f, 0.0f, 1f };
    private static readonly float[] CornerOffsetY = new float[4] { 0.0f, 0.0f, 1f, 1f };
    
    private DynamicVertexBuffer _vertexBuffer;
    private IndexBuffer _indexBuffer;
    private SpriteInfo[] _spriteInfos;
    private IntPtr[] _sortedSpriteInfos;
    private VertexPositionColorTexture4[] _vertexInfo;
    private Texture2D[] _textureInfo;
    private SpriteEffect _spriteEffect;
    private EffectPass _spriteEffectPass;
    private bool _beginCalled;
    private SpriteSortMode _sortMode;
    private BlendState _blendState;
    private SamplerState _samplerState;
    private DepthStencilState _depthStencilState;
    private RasterizerState _rasterizerState;
    private int _numSprites;
    private int bufferOffset;
    private bool supportsNoOverwrite;
    private Matrix? _transformMatrix;
    private Effect? _customEffect;
    public GraphicsDevice GraphicsDevice { get; }

    private static readonly short[] indexData = GenerateIndexArray();
    private static readonly TextureComparer TextureCompare = new();
    private static readonly BackToFrontComparer BackToFrontCompare = new();
    private static readonly FrontToBackComparer FrontToBackCompare = new();

    public FnaSpriteBatch(GraphicsDevice graphicsDevice) {
        GraphicsDevice = graphicsDevice != null
            ? graphicsDevice
            : throw new ArgumentNullException(nameof(graphicsDevice));
        _vertexInfo = new VertexPositionColorTexture4[MAX_SPRITES];
        _textureInfo = new Texture2D[MAX_SPRITES];
        _spriteInfos = new SpriteInfo[MAX_SPRITES];
        _sortedSpriteInfos = new IntPtr[MAX_SPRITES];
        _vertexBuffer = new DynamicVertexBuffer(graphicsDevice, typeof(VertexPositionColorTexture), MAX_VERTICES,
            BufferUsage.WriteOnly);
        _indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, MAX_INDICES,
            BufferUsage.WriteOnly);
        _indexBuffer.SetData(indexData);
        _spriteEffect = new SpriteEffect(graphicsDevice);
        _spriteEffectPass = _spriteEffect.CurrentTechnique.Passes[0];
        _beginCalled = false;
        _numSprites = 0;
        supportsNoOverwrite = false;
    }

    private bool IsDisposed;

    private void Dispose(bool disposing) {
        if (!IsDisposed) {
            IsDisposed = true;
            _spriteEffect.Dispose();
            _indexBuffer.Dispose();
            _vertexBuffer.Dispose();
        }
    }

    public void Dispose() {
        Dispose(true);
    }

    public void Begin() {
        Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None,
            RasterizerState.CullCounterClockwise, null, Matrix.Identity);
    }

    public void Begin(SpriteSortMode sortMode, BlendState blendState) {
        Begin(sortMode, blendState, SamplerState.LinearClamp, DepthStencilState.None,
            RasterizerState.CullCounterClockwise, null, Matrix.Identity);
    }

    public void Begin(
        SpriteSortMode sortMode,
        BlendState? blendState,
        SamplerState? samplerState,
        DepthStencilState? depthStencilState,
        RasterizerState? rasterizerState) {
        Begin(sortMode, blendState, samplerState, depthStencilState, rasterizerState, null, Matrix.Identity);
    }

    public void Begin(
        SpriteSortMode sortMode,
        BlendState? blendState,
        SamplerState? samplerState,
        DepthStencilState? depthStencilState,
        RasterizerState? rasterizerState,
        Effect? effect) {
        Begin(sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, Matrix.Identity);
    }

    public void Begin(
        SpriteSortMode sortMode,
        BlendState? blendState,
        SamplerState? samplerState,
        DepthStencilState? depthStencilState,
        RasterizerState? rasterizerState,
        Effect? effect,
        Matrix? transformationMatrix) {
        _beginCalled = !_beginCalled
            ? true
            : throw new InvalidOperationException(
                "Begin has been called before calling End after the last call to Begin. Begin cannot be called again until End has been successfully called.");
        _sortMode = sortMode;
        _blendState = blendState ?? BlendState.AlphaBlend;
        _samplerState = samplerState ?? SamplerState.LinearClamp;
        _depthStencilState = depthStencilState ?? DepthStencilState.None;
        _rasterizerState = rasterizerState ?? RasterizerState.CullCounterClockwise;
        _customEffect = effect;
        _transformMatrix = transformationMatrix;
        if (sortMode != SpriteSortMode.Immediate)
            return;
        PrepRenderState();
    }

    public void End() {
        _beginCalled = _beginCalled
            ? false
            : throw new InvalidOperationException(
                "End was called, but Begin has not yet been called. You must call Begin  successfully before you can call End.");
        if (_sortMode != SpriteSortMode.Immediate)
            FlushBatch();
        _customEffect = null;
    }

    public void Draw(Texture2D texture, Vector2 position, Color color) {
        CheckBegin(nameof(Draw));
        PushSprite(texture, 0.0f, 0.0f, 1f, 1f, position.X, position.Y, texture.Width, texture.Height, color, 0.0f,
            0.0f, 0.0f, 1f, 0.0f, 0);
    }

    public void Draw(Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color) {
        float sourceX;
        float sourceY;
        float sourceW;
        float sourceH;
        float width;
        float height;
        if (sourceRectangle is {} src) {
            sourceX = src.X / (float) texture.Width;
            sourceY = src.Y / (float) texture.Height;
            sourceW = src.Width / (float) texture.Width;
            sourceH = src.Height / (float) texture.Height;
            width = src.Width;
            height = src.Height;
        } else {
            sourceX = 0.0f;
            sourceY = 0.0f;
            sourceW = 1f;
            sourceH = 1f;
            width = texture.Width;
            height = texture.Height;
        }

        CheckBegin(nameof(Draw));
        PushSprite(texture, sourceX, sourceY, sourceW, sourceH, position.X, position.Y, width, height, color, 0.0f,
            0.0f, 0.0f, 1f, 0.0f, 0);
    }

    private static float GetMachineEpsilonFloat() {
        float machineEpsilonFloat = 1f;
        do {
            machineEpsilonFloat *= 0.5f;
        } while (1f + machineEpsilonFloat > 1.0);

        return machineEpsilonFloat;
    }

    private static float MachineEpsilonFloat = GetMachineEpsilonFloat();

    public void Draw(
        Texture2D texture,
        Vector2 position,
        Rectangle? sourceRectangle,
        Color color,
        float rotation,
        Vector2 origin,
        float scale,
        SpriteEffects effects,
        float layerDepth) {
        CheckBegin(nameof(Draw));
        float num1 = scale;
        float num2 = scale;
        float sourceX;
        float sourceY;
        float sourceW;
        float sourceH;
        float destinationW;
        float destinationH;
        if (sourceRectangle is {} src) {
            sourceX = src.X / (float) texture.Width;
            sourceY = src.Y / (float) texture.Height;
            sourceW = Math.Sign(src.Width) *
                Math.Max(Math.Abs(src.Width), MachineEpsilonFloat) / texture.Width;
            sourceH = Math.Sign(src.Height) *
                Math.Max(Math.Abs(src.Height), MachineEpsilonFloat) / texture.Height;
            destinationW = num1 * src.Width;
            destinationH = num2 * src.Height;
        } else {
            sourceX = 0.0f;
            sourceY = 0.0f;
            sourceW = 1f;
            sourceH = 1f;
            destinationW = num1 * texture.Width;
            destinationH = num2 * texture.Height;
        }

        PushSprite(texture, sourceX, sourceY, sourceW, sourceH, position.X, position.Y, destinationW, destinationH,
            color, origin.X / sourceW / texture.Width, origin.Y / sourceH / texture.Height,
            (float) Math.Sin(rotation), (float) Math.Cos(rotation), layerDepth,
            (byte) (effects & (SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically)));
    }

    public void Draw(
        Texture2D texture,
        Vector2 position,
        Rectangle? sourceRectangle,
        Color color,
        float rotation,
        Vector2 origin,
        Vector2 scale,
        SpriteEffects effects,
        float layerDepth) {
        CheckBegin(nameof(Draw));
        float sourceX;
        float sourceY;
        float sourceW;
        float sourceH;
        if (sourceRectangle is {} src) {
            sourceX = src.X / (float) texture.Width;
            sourceY = src.Y / (float) texture.Height;
            sourceW = Math.Sign(src.Width) *
                Math.Max(Math.Abs(src.Width), MachineEpsilonFloat) / texture.Width;
            sourceH = Math.Sign(src.Height) *
                Math.Max(Math.Abs(src.Height), MachineEpsilonFloat) / texture.Height;
            scale.X *= src.Width;
            scale.Y *= src.Height;
        } else {
            sourceX = 0.0f;
            sourceY = 0.0f;
            sourceW = 1f;
            sourceH = 1f;
            scale.X *= texture.Width;
            scale.Y *= texture.Height;
        }

        PushSprite(texture, sourceX, sourceY, sourceW, sourceH, position.X, position.Y, scale.X, scale.Y, color,
            origin.X / sourceW / texture.Width, origin.Y / sourceH / texture.Height, (float) Math.Sin(rotation),
            (float) Math.Cos(rotation), layerDepth,
            (byte) (effects & (SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically)));
    }

    public void Draw(Texture2D texture, Rectangle destinationRectangle, Color color) {
        CheckBegin(nameof(Draw));
        PushSprite(texture, 0.0f, 0.0f, 1f, 1f, destinationRectangle.X, destinationRectangle.Y,
            destinationRectangle.Width, destinationRectangle.Height, color, 0.0f, 0.0f, 0.0f, 1f, 0.0f, 0);
    }

    public void Draw(
        Texture2D texture,
        Rectangle destinationRectangle,
        Rectangle? sourceRectangle,
        Color color) {
        CheckBegin(nameof(Draw));
        float sourceX;
        float sourceY;
        float sourceW;
        float sourceH;
        if (sourceRectangle is {} src) {
            sourceX = src.X / (float) texture.Width;
            sourceY = src.Y / (float) texture.Height;
            sourceW = src.Width / (float) texture.Width;
            sourceH = src.Height / (float) texture.Height;
        } else {
            sourceX = 0.0f;
            sourceY = 0.0f;
            sourceW = 1f;
            sourceH = 1f;
        }

        PushSprite(texture, sourceX, sourceY, sourceW, sourceH, destinationRectangle.X, destinationRectangle.Y,
            destinationRectangle.Width, destinationRectangle.Height, color, 0.0f, 0.0f, 0.0f, 1f, 0.0f, 0);
    }

    public void Draw(
        Texture2D texture,
        Rectangle destinationRectangle,
        Rectangle? sourceRectangle,
        Color color,
        float rotation,
        Vector2 origin,
        SpriteEffects effects,
        float layerDepth) {
        CheckBegin(nameof(Draw));
        float sourceX;
        float sourceY;
        float sourceW;
        float sourceH;
        if (sourceRectangle is {} src) {
            sourceX = src.X / (float) texture.Width;
            sourceY = src.Y / (float) texture.Height;
            sourceW = Math.Sign(src.Width) *
                Math.Max(Math.Abs(src.Width), MachineEpsilonFloat) / texture.Width;
            sourceH = Math.Sign(src.Height) *
                Math.Max(Math.Abs(src.Height), MachineEpsilonFloat) / texture.Height;
        } else {
            sourceX = 0.0f;
            sourceY = 0.0f;
            sourceW = 1f;
            sourceH = 1f;
        }

        PushSprite(texture, sourceX, sourceY, sourceW, sourceH, destinationRectangle.X, destinationRectangle.Y,
            destinationRectangle.Width, destinationRectangle.Height, color, origin.X / sourceW / texture.Width,
            origin.Y / sourceH / texture.Height, (float) Math.Sin(rotation), (float) Math.Cos(rotation), layerDepth,
            (byte) (effects & (SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically)));
    }


    private unsafe void PushSprite(
        Texture2D texture,
        float sourceX,
        float sourceY,
        float sourceW,
        float sourceH,
        float destinationX,
        float destinationY,
        float destinationW,
        float destinationH,
        Color color,
        float originX,
        float originY,
        float rotationSin,
        float rotationCos,
        float depth,
        byte effects) {
        if (_numSprites >= _vertexInfo.Length) {
            int newSize = _vertexInfo.Length + MAX_SPRITES;
            Array.Resize(ref _vertexInfo, newSize);
            Array.Resize(ref _textureInfo, newSize);
            Array.Resize(ref _spriteInfos, newSize);
            Array.Resize(ref _sortedSpriteInfos, newSize);
        }

        if (_sortMode == SpriteSortMode.Immediate) {
            int baseSprite;
            fixed (VertexPositionColorTexture4* positionColorTexture4Ptr = &_vertexInfo[0]) {
                GenerateVertexInfo(positionColorTexture4Ptr, sourceX, sourceY, sourceW, sourceH, destinationX,
                    destinationY, destinationW, destinationH, color, originX, originY, rotationSin, rotationCos,
                    depth, effects);
                if (supportsNoOverwrite) {
                    baseSprite = UpdateVertexBuffer(0, 1);
                } else {
                    baseSprite = 0;
                    _vertexBuffer.SetData(_vertexInfo, 0, 1);
                }
            }

            DrawPrimitives(texture, baseSprite, 1);
        } else if (_sortMode == SpriteSortMode.Deferred) {
            fixed (VertexPositionColorTexture4* sprite = &_vertexInfo[_numSprites])
                GenerateVertexInfo(sprite, sourceX, sourceY, sourceW, sourceH, destinationX, destinationY,
                    destinationW, destinationH, color, originX, originY, rotationSin, rotationCos, depth, effects);
            _textureInfo[_numSprites] = texture;
            ++_numSprites;
        } else {
            fixed (SpriteInfo* spriteInfoPtr = &_spriteInfos[_numSprites]) {
                spriteInfoPtr->textureHash = texture.GetHashCode();
                spriteInfoPtr->sourceX = sourceX;
                spriteInfoPtr->sourceY = sourceY;
                spriteInfoPtr->sourceW = sourceW;
                spriteInfoPtr->sourceH = sourceH;
                spriteInfoPtr->destinationX = destinationX;
                spriteInfoPtr->destinationY = destinationY;
                spriteInfoPtr->destinationW = destinationW;
                spriteInfoPtr->destinationH = destinationH;
                spriteInfoPtr->color = color;
                spriteInfoPtr->originX = originX;
                spriteInfoPtr->originY = originY;
                spriteInfoPtr->rotationSin = rotationSin;
                spriteInfoPtr->rotationCos = rotationCos;
                spriteInfoPtr->depth = depth;
                spriteInfoPtr->effects = effects;
            }

            _textureInfo[_numSprites] = texture;
            ++_numSprites;
        }
    }

    private unsafe void FlushBatch() {
        PrepRenderState();
        if (_numSprites == 0)
            return;
        if (_sortMode != SpriteSortMode.Deferred) {
            IComparer<IntPtr> comparer = _sortMode != SpriteSortMode.Texture
                ? _sortMode != SpriteSortMode.BackToFront ? FrontToBackCompare : BackToFrontCompare
                : TextureCompare;
            fixed (SpriteInfo* spriteInfoPtr1 = &_spriteInfos[0])
            fixed (IntPtr* numPtr = &_sortedSpriteInfos[0])
            fixed (VertexPositionColorTexture4* positionColorTexture4Ptr = &_vertexInfo[0]) {
                for (int index = 0; index < _numSprites; ++index)
                    numPtr[index] = (IntPtr) (spriteInfoPtr1 + index);
                Array.Sort(_sortedSpriteInfos, _textureInfo, 0, _numSprites, comparer);
                for (int index = 0; index < _numSprites; ++index) {
                    SpriteInfo* spriteInfoPtr2 = (SpriteInfo*) (void*) numPtr[index];
                    GenerateVertexInfo(positionColorTexture4Ptr + index, spriteInfoPtr2->sourceX,
                        spriteInfoPtr2->sourceY, spriteInfoPtr2->sourceW, spriteInfoPtr2->sourceH,
                        spriteInfoPtr2->destinationX, spriteInfoPtr2->destinationY, spriteInfoPtr2->destinationW,
                        spriteInfoPtr2->destinationH, spriteInfoPtr2->color, spriteInfoPtr2->originX,
                        spriteInfoPtr2->originY, spriteInfoPtr2->rotationSin, spriteInfoPtr2->rotationCos,
                        spriteInfoPtr2->depth, spriteInfoPtr2->effects);
                }
            }
        }

        int start = 0;
        while (true) {
            int count = Math.Min(_numSprites, MAX_SPRITES);
            int num1 = UpdateVertexBuffer(start, count);
            int num2 = 0;
            var textureInfo = _textureInfo;
            
            Texture2D texture = textureInfo[start];
            for (int index = 1; index < count; ++index) {
                Texture2D otherTexture = textureInfo[start + index];
                if (otherTexture != texture) {
                    DrawPrimitives(texture, num1 + num2, index - num2);
                    texture = otherTexture;
                    num2 = index;
                }
            }

            DrawPrimitives(texture, num1 + num2, count - num2);
            if (_numSprites > MAX_SPRITES) {
                _numSprites -= MAX_SPRITES;
                start += MAX_SPRITES;
            } else
                break;
        }

        _numSprites = 0;
    }

    private int UpdateVertexBuffer(int start, int count) {
        int num;
        SetDataOptions options;
        if (bufferOffset + count > MAX_SPRITES || !supportsNoOverwrite) {
            num = 0;
            options = SetDataOptions.Discard;
        } else {
            num = bufferOffset;
            options = SetDataOptions.NoOverwrite;
        }

        _vertexBuffer.SetData(_vertexInfo, start, count, options);
        bufferOffset = num + count;
        return num;
    }

    private static unsafe void GenerateVertexInfo(
        VertexPositionColorTexture4* sprite,
        float sourceX,
        float sourceY,
        float sourceW,
        float sourceH,
        float destinationX,
        float destinationY,
        float destinationW,
        float destinationH,
        Color color,
        float originX,
        float originY,
        float rotationSin,
        float rotationCos,
        float depth,
        byte effects) {
        float num1 = -originX * destinationW;
        float num2 = -originY * destinationH;
        float num3 = (1f - originX) * destinationW;
        float num6 = (1f - originY) * destinationH;
        
        sprite->Position0.X = -rotationSin * num2 + rotationCos * num1 + destinationX;
        sprite->Position0.Y = rotationCos * num2 + rotationSin * num1 + destinationY;
        
        sprite->Position1.X = -rotationSin * num2 + rotationCos * num3 + destinationX;
        sprite->Position1.Y = rotationCos * num2 + rotationSin * num3 + destinationY;
        
        sprite->Position2.X = -rotationSin * num6 + rotationCos * num1 + destinationX;
        sprite->Position2.Y = rotationCos * num6 + rotationSin * num1 + destinationY;
        
        sprite->Position3.X = -rotationSin * num6 + rotationCos * num3 + destinationX;
        sprite->Position3.Y = rotationCos * num6 + rotationSin * num3 + destinationY;
        
        fixed (float* xOffsets = &CornerOffsetX[0])
        fixed (float* yOffsets = &CornerOffsetY[0]) {
            sprite->TextureCoordinate0.X = xOffsets[0 ^ effects] * sourceW + sourceX;
            sprite->TextureCoordinate0.Y = yOffsets[0 ^ effects] * sourceH + sourceY;
            sprite->TextureCoordinate1.X = xOffsets[1 ^ effects] * sourceW + sourceX;
            sprite->TextureCoordinate1.Y = yOffsets[1 ^ effects] * sourceH + sourceY;
            sprite->TextureCoordinate2.X = xOffsets[2 ^ effects] * sourceW + sourceX;
            sprite->TextureCoordinate2.Y = yOffsets[2 ^ effects] * sourceH + sourceY;
            sprite->TextureCoordinate3.X = xOffsets[3 ^ effects] * sourceW + sourceX;
            sprite->TextureCoordinate3.Y = yOffsets[3 ^ effects] * sourceH + sourceY;
        }

        if (sprite->Position0.Z != depth) {
            sprite->Position0.Z = depth;
            sprite->Position1.Z = depth;
            sprite->Position2.Z = depth;
            sprite->Position3.Z = depth;
        }


        if (sprite->Color0 != color) {
            sprite->Color0 = color;
            sprite->Color1 = color;
            sprite->Color2 = color;
            sprite->Color3 = color;
        }
    }

    private void PrepRenderState() {
        GraphicsDevice.BlendState = _blendState;
        GraphicsDevice.SamplerStates[0] = _samplerState;
        GraphicsDevice.DepthStencilState = _depthStencilState;
        GraphicsDevice.RasterizerState = _rasterizerState;
        GraphicsDevice.SetVertexBuffer(_vertexBuffer);
        GraphicsDevice.Indices = _indexBuffer;

        _spriteEffect.TransformMatrix = _transformMatrix;
        _spriteEffectPass.Apply();
    }

    private void DrawPrimitives(Texture texture, int baseSprite, int batchSize) {
        if (_customEffect != null) {
            foreach (EffectPass pass in _customEffect.CurrentTechnique.Passes) {
                pass.Apply();
                GraphicsDevice.Textures[0] = texture;
                GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, baseSprite * 4, 0, batchSize * 2);
            }
        } else {
            GraphicsDevice.Textures[0] = texture;
            GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, baseSprite * 4, 0, batchSize * 2);
        }
    }

    private void CheckBegin(string method) {
        if (!_beginCalled)
            throw new InvalidOperationException(method +
                                                " was called, but Begin has not yet been called. Begin must be called successfully before you can call " +
                                                method + ".");
    }

    private static short[] GenerateIndexArray() {
        short[] indexArray = new short[MAX_INDICES];
        int index = 0;
        int num = 0;
        while (index < MAX_INDICES) {
            indexArray[index] = (short) num;
            indexArray[index + 1] = (short) (num + 1);
            indexArray[index + 2] = (short) (num + 2);
            indexArray[index + 3] = (short) (num + 3);
            indexArray[index + 4] = (short) (num + 2);
            indexArray[index + 5] = (short) (num + 1);
            index += 6;
            num += 4;
        }

        return indexArray;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct VertexPositionColorTexture4 : IVertexType {
        public Vector3 Position0;
        public Color Color0;
        public Vector2 TextureCoordinate0;
        public Vector3 Position1;
        public Color Color1;
        public Vector2 TextureCoordinate1;
        public Vector3 Position2;
        public Color Color2;
        public Vector2 TextureCoordinate2;
        public Vector3 Position3;
        public Color Color3;
        public Vector2 TextureCoordinate3;

        VertexDeclaration IVertexType.VertexDeclaration => throw new NotImplementedException();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct SpriteInfo {
        public int textureHash;
        public float sourceX;
        public float sourceY;
        public float sourceW;
        public float sourceH;
        public float destinationX;
        public float destinationY;
        public float destinationW;
        public float destinationH;
        public Color color;
        public float originX;
        public float originY;
        public float rotationSin;
        public float rotationCos;
        public float depth;
        public byte effects;
    }

    private class TextureComparer : IComparer<IntPtr> {
        public unsafe int Compare(IntPtr i1, IntPtr i2) {
            return ((SpriteInfo*) (void*) i1)->textureHash.CompareTo(((SpriteInfo*) (void*) i2)->textureHash);
        }
    }

    private class BackToFrontComparer : IComparer<IntPtr> {
        public unsafe int Compare(IntPtr i1, IntPtr i2) {
            SpriteInfo* spriteInfoPtr = (SpriteInfo*) (void*) i1;
            return ((SpriteInfo*) (void*) i2)->depth.CompareTo(spriteInfoPtr->depth);
        }
    }

    private class FrontToBackComparer : IComparer<IntPtr> {
        public unsafe int Compare(IntPtr i1, IntPtr i2) {
            return ((SpriteInfo*) (void*) i1)->depth.CompareTo(((SpriteInfo*) (void*) i2)->depth);
        }
    }
}