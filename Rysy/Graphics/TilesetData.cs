using Rysy.Extensions;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Rysy.Graphics; 

public sealed class TilesetData {
    public string Filename;

    public char Id { get; init; }
        
    public Autotiler Autotiler { get; init; }

    [JsonIgnore]
    public VirtTexture Texture = null!;
    public List<(string mask, AutotiledSprite[] tiles)> Tiles = new();

    public AutotiledSprite[] Center = null!;
    public AutotiledSprite[] Padding = null!;

    public char[]? Ignores;

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
    private readonly Dictionary<long, AutotiledSprite[]?> _fastTileDataToTiles = new();

    private bool TryFindFirstMaskMatch(Span<bool> tileData, [NotNullWhen(true)] out AutotiledSprite[]? tiles) {
        long bitmask = 0;
        bool hasBitmask = tileData.Length == 9;
        if (hasBitmask) {
            bitmask =
                tileData[0].AsByte() +
                (tileData[1].AsByte() << 1) +
                (tileData[2].AsByte() << 2) +
                (tileData[3].AsByte() << 3) +
                (tileData[4].AsByte() << 4) +
                (tileData[5].AsByte() << 5) +
                (tileData[6].AsByte() << 6) +
                (tileData[7].AsByte() << 7) +
                (tileData[8].AsByte() << 8);

            if (_fastTileDataToTiles.TryGetValue(bitmask, out tiles)) {
                return tiles is { };
            }
        }
            
        var allTiles = Tiles;
        for (int i = 0; i < allTiles.Count; i++) {
            if (MatchingMask(allTiles[i].mask, tileData)) {
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
        Span<bool> tileData = stackalloc bool[9];
        tileData[0] = checker.IsConnectedTileAt(x - 1, y - 1, this);
        tileData[1] = checker.IsConnectedTileAt(x, y - 1, this);
        tileData[2] = checker.IsConnectedTileAt(x + 1, y - 1, this);

        tileData[3] = checker.IsConnectedTileAt(x - 1, y, this);
        tileData[4] = true;
        tileData[5] = checker.IsConnectedTileAt(x + 1, y, this);

        tileData[6] = checker.IsConnectedTileAt(x - 1, y + 1, this);
        tileData[7] = checker.IsConnectedTileAt(x, y + 1, this);
        tileData[8] = checker.IsConnectedTileAt(x + 1, y + 1, this);

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

    internal static bool MatchingMask(string mask, Span<bool> tileData) {
        var tl = tileData.Length;
        var sl = mask.Length;

        // handle the common case of a 3x3 mask
        if (sl == 9 && tl == 9) {
            // for an explanation, see comment in the below for loop
            if (mask[0] + Unsafe.As<bool, byte>(ref tileData[0]) == '1')
                return false;
            if (mask[1] + Unsafe.As<bool, byte>(ref tileData[1]) == '1')
                return false;
            if (mask[2] + Unsafe.As<bool, byte>(ref tileData[2]) == '1')
                return false;
            if (mask[3] + Unsafe.As<bool, byte>(ref tileData[3]) == '1')
                return false;
            // skip mask[4] - that's the tile we're in!
            if (mask[5] + Unsafe.As<bool, byte>(ref tileData[5]) == '1')
                return false;
            if (mask[6] + Unsafe.As<bool, byte>(ref tileData[6]) == '1')
                return false;
            if (mask[7] + Unsafe.As<bool, byte>(ref tileData[7]) == '1')
                return false;
            if (mask[8] + Unsafe.As<bool, byte>(ref tileData[8]) == '1')
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
            var r = mask[i] + Unsafe.As<bool, byte>(ref tileData[i]);
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
}

public readonly struct HollowRectTileChecker(int w, int h, char id) : ITileChecker {
    public bool IsInBounds(int x, int y) => x >= 0 && x < w && y < h && y >= 0;
    public bool IsConnectedTileAt(int x, int y, TilesetData data) => x == 0 || x == w - 1 || y == h - 1 || y == 0;
    public char GetTileAt(int x, int y) => IsConnectedTileAt(x, y, null!) ? id : '0';
}
        
public readonly struct FilledRectTileChecker(int w, int h, char id) : ITileChecker {
    public bool IsInBounds(int x, int y) => x >= 0 && x < w && y < h && y >= 0;
    
    public bool IsConnectedTileAt(int x, int y, TilesetData data) => IsInBounds(x, y);
    
    public char GetTileAt(int x, int y) => id;
}
        
public readonly struct TilegridTileChecker(char[,] t, bool tilesOob)
    : ITileChecker {
    public bool IsInBounds(int x, int y) {
        return x >= 0 & x < t.GetLength(0) & y < t.GetLength(1) & y >= 0;
    }

    public bool IsConnectedTileAt(int x, int y, TilesetData data) {
        if (x >= 0 & x < t.GetLength(0) & y < t.GetLength(1) & y >= 0) {
            var tile = t[x, y];
            return tile != '0' && (!data.IgnoreAll || tile == data.Id) && (!data.Ignores?.Contains(tile) ?? true);
        }
        return tilesOob;
    }

    public char GetTileAt(int x, int y) {
        if (x >= 0 & x < t.GetLength(0) & y < t.GetLength(1) & y >= 0) {
            var tile = t[x, y];
            return tile;
        }
        return '0';
    }
}