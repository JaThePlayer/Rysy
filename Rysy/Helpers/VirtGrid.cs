using Rysy.Extensions;
using System.Collections;
using System.Runtime.CompilerServices;

namespace Rysy.Helpers;

public sealed class VirtGrid<T> : IEnumerable<T?> where T : IEquatable<T> {
    public const int ChunkSize = 64;
    
    public int Width { get; }
    public int Height { get; }

    private int ChunkGridWidth { get; }
    
    public T? FillValue { get; }

    private Chunk?[] Chunks { get; }

    public VirtGrid(int w, int h, T? fill) {
        Width = w;
        Height = h;
        FillValue = fill;

        ChunkGridWidth = ((w + ChunkSize - 1) / ChunkSize).AtLeast(1);
        Chunks = new Chunk[
            ((w + ChunkSize - 1) / ChunkSize * (h + ChunkSize - 1) / ChunkSize).AtLeast(1)
            //w * h / ChunkSize / ChunkSize
        ];


        //Console.WriteLine($"CHUNK {w} {h} -> {Chunks.Length} [{Chunks.Length * ChunkSize * ChunkSize} vs {w * h}]");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetLength(int rank) => rank switch {
        0 => Width,
        _ => Height
    };

    private bool Inbounds(int x, int y) {
        return !(x < 0 || x >= Width || y < 0 || y >= Height);
    }

    public Chunk? GetChunk(int x, int y) {
        var cx = x / ChunkSize;
        var cy = y / ChunkSize;
        
        var idx = cy * ChunkGridWidth + cx;
        if (idx >= Chunks.Length)
            return null;
        return Chunks[idx];
    }
    
    public Chunk? GetChunk(int x, int y, out int chunkInLoc) {
        var (cx, cix) = int.DivRem(x, ChunkSize);
        var (cy, ciy) = int.DivRem(y, ChunkSize);
        //var cx = x / ChunkSize;
        //var cy = y / ChunkSize;
        
        var idx = cy * ChunkGridWidth + cx;
        if (idx >= Chunks.Length) {
            chunkInLoc = 0;
            return null;
        }
        chunkInLoc = ciy * ChunkSize + cix;
        return Chunks[idx];
    }
    
    private Chunk? GetOrCreateChunk(int x, int y) {
        var cx = x / ChunkSize;
        var cy = y / ChunkSize;

        var idx = cy * ChunkGridWidth + cx;
        if (idx >= Chunks.Length)
            return null;
        
        ref var ch = ref Chunks[idx];
        ch ??= new();

        return ch;
    }

    public T this[int x, int y] {
        get => SafeGet(x, y, FillValue);
        set => SafeSet(x, y, value);
    }

    public T SafeGet(int x, int y, T def) {
        if (!Inbounds(x, y)) {
            return def;
        }

        var chunk = GetChunk(x, y);
        if (chunk is null || chunk.Empty) {
            return def;
        }

        return chunk.Get(x, y);
    }
    
    public bool SafeSet(int x, int y, T? val) {
        if (!Inbounds(x, y)) {
            return false;
        }

        if (FillValue?.Equals(val) ?? val is null) {
            // Don't create chunks if we're just going to set stuff to default
            var ch = GetChunk(x, y);
            if (ch is null)
                return false;
            //return ch.Set(x, y, val);
        }

        return GetOrCreateChunk(x, y)?.Set(x, y, val) ?? false;
    }

    public bool ChunkExistsAt(int x, int y) => GetChunk(x, y) is { };
    
    internal T? DirectGet(int chunkIdx, int inChunkIdx) {
        if (Chunks[chunkIdx] is not { } ch)
            return default!;
        
        return ch.Get(inChunkIdx);
    }

    public sealed class Chunk {
        private T?[]? _data;

        public bool Empty => _data is null;

        public T? Get(int inChunkIdx) => _data is {} data ? data[inChunkIdx] : default!;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? Get(int x, int y) {
            if (_data is null)
                return default!;
            
            var cx = x % ChunkSize;
            var cy = y % ChunkSize;

            var idx = cy * ChunkSize + cx;
            if (idx >= _data.Length)
                return default!;
            
            return _data[idx];
        }
        
        public bool Set(int x, int y, T? val) {
            _data ??= new T[ChunkSize * ChunkSize];
            
            var cx = x % ChunkSize;
            var cy = y % ChunkSize;
            var idx = cy * ChunkSize + cx;
            if (idx >= _data.Length)
                return false;
            
            ref var stored = ref _data[idx];

            stored = val;
            return true;
        }
    }

    public Enumerator GetEnumerator() => new(this);

    IEnumerator<T> IEnumerable<T?>.GetEnumerator() {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    public struct Enumerator : IEnumerator<T?> {
        private readonly VirtGrid<T> _grid;
        private int _chunkIdx;
        private int _i;
        private byte _mode;

        public Enumerator(VirtGrid<T> grid) {
            _grid = grid;
            Reset();
        }
        
        public bool MoveNext() {
            var chunks = _grid.Chunks;
            switch (_mode) {
                case 0: // move to next chunk
                    while (++_chunkIdx < chunks.Length && chunks[_chunkIdx] is null) {
                        
                    }

                    if (_chunkIdx == chunks.Length) {
                        // ran out of chunks
                        return false;
                    }
                    
                    _i = -1;
                    _mode = 1;
                    goto case 1; // fallthrough
                case 1: // move to next item in chunk
                    _i++;
                    if (_i >= ChunkSize * ChunkSize - 1) {
                        // ran out of tiles
                        _mode = 0;
                    }

                    return true;
            }

            return false;
        }

        public void Reset() {
            _chunkIdx = -1;
            _i = -1;
            _mode = 0;
        }

        public readonly T? Current => _grid.DirectGet(_chunkIdx, _i);

        object IEnumerator.Current => Current!;

        public readonly void Dispose() {
        }
    }
}