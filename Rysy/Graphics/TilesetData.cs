using Rysy.Extensions;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Rysy.Graphics;

public sealed class TilesetMask {
    public string StringValue { get; }

    public int Length => StringValue.Length;

    public TilesetMask(string value) {
        value = value.Replace("-", "", StringComparison.Ordinal);
            
        StringValue = value;
    }

    public char this[int index] => StringValue[index];
    
    public MaskType TypeAt(int index) => StringValue[index] switch {
        '0' => MaskType.Empty,
        '1' => MaskType.Tile,
        'x' or 'X' => MaskType.Any,
        'y' or 'Y' => MaskType.NotThis,
        _ => MaskType.Custom
    };

    public enum MaskType {
        /// <summary>
        /// The tile should be empty or ignored
        /// </summary>
        Empty = 0,
        /// <summary>
        /// The tile should be a non-ignored tile
        /// </summary>
        Tile = 1,
        /// <summary>
        /// The tile should be ignored
        /// </summary>
        Any = 2,
        /// <summary>
        /// The tile should not be the same as this tile
        /// </summary>
        NotThis = 3,
        /// <summary>
        /// User-defined filter
        /// </summary>
        Custom
    }
}

public sealed class TilesetData {
    public string Filename;

    public char Id { get; init; }
        
    public Autotiler Autotiler { get; init; }

    [JsonIgnore]
    public VirtTexture Texture = null!;
    public List<(TilesetMask mask, AutotiledSprite[] tiles)> Tiles = new();

    public AutotiledSprite[] Center = null!;
    public AutotiledSprite[] Padding = null!;

    public char[]? Ignores;

    public int ScanWidth = 3;
    public int ScanHeight = 3;

    public bool IgnoreAll;
    internal string? DisplayName;

    private AutotiledSpriteList? _preview;
    public AutotiledSpriteList GetPreview(int previewSizePixels) {
        if (_preview is { } cached && cached.Sprites.GetLength(0) == previewSizePixels / 8)
            return cached;
            
        var tileGrid = new char[previewSizePixels / 8, previewSizePixels / 8];
        tileGrid.Fill(Id);
        _preview = Autotiler.GetSprites(Vector2.Zero, tileGrid, Color.White, tilesOOB: false);

        return _preview;
    }

    public string GetDisplayName() 
        => DisplayName ??= Filename.Split('/').Last().TrimStart("bg").Humanize();

    /// <summary>
    /// Stores a tilegrid bitmask -> possible tiles.
    /// Used for speeding up GetFirstMatch
    /// </summary>
    private readonly Dictionary<Int128, AutotiledSprite[]?> _fastTileDataToTiles = new();

    public bool Validate() {
        bool valid = true;
        
        if (ScanWidth <= 0 || ScanWidth % 2 == 0) {
            Logger.Write("Autotiler", LogLevel.Error, $"Tileset scanWidth must be a positive, odd integer, but tileset {Id} defined it as {ScanWidth}");
            valid = false;
        }
        if (ScanHeight <= 0 || ScanHeight % 2 == 0) {
            Logger.Write("Autotiler", LogLevel.Error, $"Tileset scanHeight must be a positive, odd integer, but tileset {Id} defined it as {ScanHeight}");
            valid = false;
        }


        return valid;
    }

    private struct NineBytes {
        private ulong _00;
        private byte _08;

        public Int128 ToInt128() => new(_08,_00);
    }
    
    private unsafe bool TryFindFirstMaskMatch(Span<byte> tileDataSpan, [NotNullWhen(true)] out AutotiledSprite[]? tiles) {
        if (tileDataSpan.Length == 0) {
            tiles = null;
            return false;
        }
        
        Int128 bitmask = 0;
        bool hasBitmask = tileDataSpan.Length <= 32;
        if (hasBitmask) {
            if (tileDataSpan.Length == 9) {
                // all tiles fit unpacked within a Int128. This fits 3x3 and 5x3/3x5 masks cleanly.
                bitmask = Unsafe.As<byte, NineBytes>(ref tileDataSpan[0]).ToInt128();
            } else if (tileDataSpan.Length == 15) {
                bitmask = Unsafe.As<byte, Int128>(ref tileDataSpan[0]);
            } else if (tileDataSpan.Length == 25) {
                var lower = Unsafe.As<byte, Int128>(ref tileDataSpan[0]);
                var upper = Unsafe.As<byte, NineBytes>(ref tileDataSpan[16]).ToInt128();

                // pack the bytes together a bit
                bitmask = lower + (upper << 1);
            } else { 
                var len = tileDataSpan.Length;
                var bitmaskInt = 0;
                /*
                // Larger masks, may benefit from this, but I haven't really seen a significant difference:
                if (Vector128.IsHardwareAccelerated && tileDataSpan.Length > Vector128<byte>.Count) {
                    Vector128<byte> b = -Vector128.Create<byte>(tileDataSpan[^Vector128<byte>.Count..]);
                    bitmask = (int)b.ExtractMostSignificantBits() << Vector128<byte>.Count;
                    len -= Vector128<byte>.Count;
                }*/

                fixed (byte* tileData = &tileDataSpan[0]) // unsafe access to get rid of bound checks
                    switch (len) { // unroll loop, this creates a jump table, and all gotos are removed.
                        case 32: bitmaskInt |= tileData[31] << 31; goto case 31;
                        case 31: bitmaskInt |= tileData[30] << 30; goto case 30;
                        case 30: bitmaskInt |= tileData[29] << 29; goto case 29;
                        case 29: bitmaskInt |= tileData[28] << 28; goto case 28;
                        case 28: bitmaskInt |= tileData[27] << 27; goto case 27;
                        case 27: bitmaskInt |= tileData[26] << 26; goto case 26;
                        case 26: bitmaskInt |= tileData[25] << 25; goto case 25;
                        case 25: bitmaskInt |= tileData[24] << 24; goto case 24;
                        case 24: bitmaskInt |= tileData[23] << 23; goto case 23;
                        case 23: bitmaskInt |= tileData[22] << 22; goto case 22;
                        case 22: bitmaskInt |= tileData[21] << 21; goto case 21;
                        case 21: bitmaskInt |= tileData[20] << 20; goto case 20;
                        case 20: bitmaskInt |= tileData[19] << 19; goto case 19;
                        case 19: bitmaskInt |= tileData[18] << 18; goto case 18;
                        case 18: bitmaskInt |= tileData[17] << 17; goto case 17;
                        case 17: bitmaskInt |= tileData[16] << 16; goto case 16;
                        case 16: bitmaskInt |= tileData[15] << 15; goto case 15;
                        case 15: bitmaskInt |= tileData[14] << 14; goto case 14;
                        case 14: bitmaskInt |= tileData[13] << 13; goto case 13;
                        case 13: bitmaskInt |= tileData[12] << 12; goto case 12;
                        case 12: bitmaskInt |= tileData[11] << 11; goto case 11;
                        case 11: bitmaskInt |= tileData[10] << 10; goto case 10;
                        case 10: bitmaskInt |= tileData[9] << 9; goto case 9;
                        case 9: bitmaskInt |= tileData[8] << 8; goto case 8;
                        case 8: bitmaskInt |= tileData[7] << 7; goto case 7;
                        case 7: bitmaskInt |= tileData[6] << 6; goto case 6;
                        case 6: bitmaskInt |= tileData[5] << 5; goto case 5;
                        case 5: bitmaskInt |= tileData[4] << 4; goto case 4;
                        case 4: bitmaskInt |= tileData[3] << 3; goto case 3;
                        case 3: bitmaskInt |= tileData[2] << 2; goto case 2;
                        case 2: bitmaskInt |= tileData[1] << 1; goto case 1;
                        case 1: bitmaskInt |= tileData[0] << 0; break;
                    }

                bitmask = bitmaskInt;
            }

            
            if (_fastTileDataToTiles.TryGetValue(bitmask, out tiles)) {
                return tiles is { };
            }
        }

        var allTiles = Tiles;
        for (int i = 0; i < allTiles.Count; i++) {
            if (MatchingMask(allTiles[i].mask, tileDataSpan)) {
                tiles = allTiles[i].tiles;
                if (hasBitmask)
                    _fastTileDataToTiles[bitmask] = tiles;
                return true;
            }
        }
            
        tiles = null;
        if (hasBitmask)
            _fastTileDataToTiles[bitmask] = tiles;
        return false;
    }
    
    public bool GetFirstMatch(char[,] t, int x, int y, bool tilesOob, [NotNullWhen(true)] out AutotiledSprite[]? tiles) {
        //char middleTile = t[x, y];
        var checker = new TilegridTileChecker(t, tilesOob);

        return GetFirstMatch(checker, x, y, out tiles);
    }
        
    public bool GetFirstMatch<T>(T checker, int x, int y, [NotNullWhen(true)] out AutotiledSprite[]? tiles) 
        where T : struct, ITileChecker {
        Span<byte> tileData = stackalloc byte[ScanWidth * ScanHeight];

        // prepare tile data
        if (tileData.Length == 9) {
            // fast path for the most common 3x3 scan size
            var tilesOob = checker.ExtendOutOfBounds().AsByte();
            var topExists = checker.IsInBounds(x, y - 1);
            var botExists = checker.IsInBounds(x, y + 1);
            
            var leftExists = checker.IsInBounds(x - 1, y);
            var rightExists = checker.IsInBounds(x + 1, y);

            if (topExists) {
                tileData[0] = leftExists ? checker.IsConnectedTileAt(x - 1, y - 1, this).AsByte() : tilesOob;
                tileData[1] = checker.IsConnectedTileAt(x, y - 1, this).AsByte();
                tileData[2] = rightExists ? checker.IsConnectedTileAt(x + 1, y - 1, this).AsByte() : tilesOob;
            } else {
                tileData[0] = tilesOob;
                tileData[1] = tilesOob;
                tileData[2] = tilesOob;
            }

            tileData[3] = leftExists ? checker.IsConnectedTileAt(x - 1, y, this).AsByte() : tilesOob;
            tileData[4] = true.AsByte();
            tileData[5] = rightExists ? checker.IsConnectedTileAt(x + 1, y, this).AsByte() : tilesOob;

            if (botExists) {
                tileData[6] = leftExists ? checker.IsConnectedTileAt(x - 1, y + 1, this).AsByte() : tilesOob;
                tileData[7] = checker.IsConnectedTileAt(x, y + 1, this).AsByte();
                tileData[8] = rightExists ? checker.IsConnectedTileAt(x + 1, y + 1, this).AsByte() : tilesOob;
            } else {
                tileData[6] = tilesOob;
                tileData[7] = tilesOob;
                tileData[8] = tilesOob;
            }
        } else {
            var i = 0;
            for (int oy = -ScanHeight / 2; oy <= ScanHeight / 2; oy++) {
                for (int ox = -ScanWidth / 2; ox <= ScanWidth / 2; ox++) {
                    tileData[i++] = checker.IsConnectedTileAt(x + ox, y + oy, this).AsByte();
                }
            }
        }

        if (TryFindFirstMaskMatch(tileData, out tiles)) {
            return true;
        }

        if (!checker.IsConnectedTileAt(x - 2, y, this)
            || !checker.IsConnectedTileAt(x + 2, y, this)
            || !checker.IsConnectedTileAt(x, y - 2, this)
            || !checker.IsConnectedTileAt(x, y + 2, this)) {
            tiles = Padding;
        } else {
            tiles = Center;
        }

        return true;
    }

    internal static bool MatchingMask(TilesetMask maskData, Span<byte> tileData) {
        var tl = tileData.Length;
        var mask = maskData.StringValue;
        var sl = mask.Length;

        // handle the common case of a 3x3 mask
        if (sl == 9 && tl == 9) {
            // for an explanation, see comment in the below for loop
            if (mask[0] + tileData[0] == '1')
                return false;
            if (mask[1] + tileData[1] == '1')
                return false;
            if (mask[2] + tileData[2] == '1')
                return false;
            if (mask[3] + tileData[3] == '1')
                return false;
            // skip mask[4] - that's the tile we're in!
            if (mask[5] + tileData[5] == '1')
                return false;
            if (mask[6] + tileData[6] == '1')
                return false;
            if (mask[7] + tileData[7] == '1')
                return false;
            if (mask[8] + tileData[8] == '1')
                return false;

            return true;
        }

        // matches a mask of any size
        for (int i = 0; i < tl && i < sl; i++) {
            //if ((mask[i], tileData[i]) is ('0', true) or ('1', false))
            //    return false;

            // The two states in which a mask doesn't match are:
            // '0', true
            // '1', false
            // since '0' + (byte)true == '1', and '1' + (byte)false == '1',
            // we can simply add the two values together and check against '1'
            // instead of checking all 4 conditions
            var r = mask[i] + tileData[i];
            if (r == '1')
                return false;
        }

        return true;
    }
}

public interface ITileChecker {
    /// <summary>
    /// Returns whether the given location is within the bounds of the tilegrid
    /// </summary>
    bool IsInBounds(int x, int y);
    
    /// <summary>
    /// Checks whether the tileset stored in <see cref="data" /> should connect to a tile located at x, y.
    /// Mask size has already been taken into account.
    /// </summary>
    bool IsConnectedTileAt(int x, int y, TilesetData data);

    /// <summary>
    /// Returns the tileset id at the given location. The given location might be out of bounds!
    /// </summary>
    char GetTileAt(int x, int y);

    /// <summary>
    /// Whether to treat tiles out of bounds as existing or not.
    /// </summary>
    bool ExtendOutOfBounds();
}

public readonly struct HollowRectTileChecker(int w, int h, char id) : ITileChecker {
    public bool IsInBounds(int x, int y) => x >= 0 && x < w && y < h && y >= 0;
    public bool IsConnectedTileAt(int x, int y, TilesetData data) => x == 0 || x == w - 1 || y == h - 1 || y == 0;

    public char GetTileAt(int x, int y) => IsConnectedTileAt(x, y, null!) ? id : '0';
    
    public bool ExtendOutOfBounds() {
        return false;
    }
}
        
public readonly struct FilledRectTileChecker(int w, int h, char id) : ITileChecker {
    public bool IsInBounds(int x, int y) => x >= 0 && x < w && y < h && y >= 0;
    
    public bool IsConnectedTileAt(int x, int y, TilesetData data) => IsInBounds(x, y);
    
    public char GetTileAt(int x, int y) => id;
    
    public bool ExtendOutOfBounds() {
        return false;
    }
}
        
public readonly struct TilegridTileChecker(char[,] t, bool tilesOob)
    : ITileChecker {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsInBounds(int x, int y) {
        return x >= 0 && x < t.GetLength(0) && y < t.GetLength(1) && y >= 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsConnectedTileAt(int x, int y, TilesetData data) {
        if (x >= 0 && x < t.GetLength(0) && y < t.GetLength(1) && y >= 0) {
            var tile = t[x, y];
            return tile != '0' && (!data.IgnoreAll || tile == data.Id) && (!data.Ignores?.Contains(tile) ?? true);
        }
        return tilesOob;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public char GetTileAt(int x, int y) {
        if (x >= 0 && x < t.GetLength(0) && y < t.GetLength(1) && y >= 0) {
            var tile = t[x, y];
            return tile;
        }
        return '0';
    }

    public bool ExtendOutOfBounds()
        => tilesOob;
}