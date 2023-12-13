using Rysy.Extensions;
using Rysy.Helpers;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Xml;

namespace Rysy.Graphics;

/// <summary>
/// Represents a 'define' element inside a tileset definition
/// </summary>
public sealed class TilesetDefine {
    /// <summary>
    /// The id of this define.
    /// </summary>
    public char Id { get; }
    
    /// <summary>
    /// The 'filter' provided. What it means is dictated by the <see cref="Mode"/> property.
    /// </summary>
    public char[] Filter { get; }

    private SearchValues<char> FilterSearchValues;

    /// <summary>
    /// Defines how the 'filter' property should be treated.
    /// </summary>
    public Modes Mode { get; }

    public TilesetDefine(char id, char[] filter, Modes mode) {
        Id = id;
        Filter = filter;
        Mode = mode;
    }

    public TilesetDefine(XmlNode xml, char tilesetId) {
        var idString = xml.Attributes?["id"]?.InnerText ?? throw new Exception($"<define> missing 'id' in tileset {tilesetId}");

        if (idString is not [var id]) {
            throw new Exception($"<define> has id that's not exactly 1 character ({idString}) in tileset {tilesetId}");
        }

        Id = id;
        
        var filterText = xml.Attributes?["filter"]?.InnerText ?? throw new Exception($"<set id=\"{Id}\"> is missing 'filter' for tileset {id}");
        Filter = filterText
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(str => str is [var only] ? only : '0')
            .ToArray();

        if (!bool.TryParse(xml.Attributes["ignore"]?.InnerText ?? "false", out var ignore)) {
            throw new Exception($"<set id=\"{Id}\"> has invalid 'ignore' ({xml.Attributes["ignore"]?.InnerText}) for tileset {id}");
        }

        Mode = ignore ? Modes.Blacklist : Modes.Whitelist;
    }
    
    public enum Modes {
        Whitelist,
        Blacklist,
    }

    /// <summary>
    /// Checks whether the given tile id matches this define.
    /// </summary>
    public bool Match(char tileId) {
        FilterSearchValues ??= SearchValues.Create(Filter);
        
        return Mode switch {
            Modes.Whitelist => FilterSearchValues.Contains(tileId),
            Modes.Blacklist => !FilterSearchValues.Contains(tileId),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}

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

/// <summary>
/// Represents a 'set' xml element within a tileset
/// </summary>
public sealed record TilesetSet(TilesetMask Mask, AutotiledSprite[] Tiles);

public sealed class TilesetData {
    public string Filename;

    public char Id { get; init; }
        
    public Autotiler Autotiler { get; init; }

    [JsonIgnore]
    public VirtTexture Texture = null!;
    public List<TilesetSet> Tiles = new();

    public AutotiledSprite[] Center = null!;
    public AutotiledSprite[] Padding = null!;
    
    public char[]? Ignores;
    
    public Dictionary<char, TilesetDefine> Defines { get; set; } = new();

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

    private char[]? _tileDataSharedBuffer;
    
    public bool GetFirstMatch<T>(T checker, int x, int y, [NotNullWhen(true)] out AutotiledSprite[]? tiles)
    where T : struct, ITileChecker {
        _tileDataSharedBuffer ??= new char[ScanWidth * ScanHeight];
        var tileData = _tileDataSharedBuffer;

        var currentTile = Id;
        var oobTile = checker.ExtendOutOfBounds() ? currentTile : '0';

        // prepare tile data
        if (tileData.Length == 9) {
            // fast path for the most common 3x3 scan size
            
            var topExists = checker.IsInBounds(x, y - 1);
            var botExists = checker.IsInBounds(x, y + 1);
            
            var leftExists = checker.IsInBounds(x - 1, y);
            var rightExists = checker.IsInBounds(x + 1, y);

            if (topExists) {
                tileData[0] = leftExists ? checker.GetTileAt(x - 1, y - 1, oobTile) : oobTile;
                tileData[1] = checker.GetTileAt(x, y - 1, oobTile);
                tileData[2] = rightExists ? checker.GetTileAt(x + 1, y - 1, oobTile) : oobTile;
            } else {
                tileData[0] = oobTile;
                tileData[1] = oobTile;
                tileData[2] = oobTile;
            }

            tileData[3] = leftExists ? checker.GetTileAt(x - 1, y, oobTile) : oobTile;
            tileData[4] = currentTile;
            tileData[5] = rightExists ? checker.GetTileAt(x + 1, y, oobTile) : oobTile;

            if (botExists) {
                tileData[6] = leftExists ? checker.GetTileAt(x - 1, y + 1, oobTile) : oobTile;
                tileData[7] = checker.GetTileAt(x, y + 1, oobTile);
                tileData[8] = rightExists ? checker.GetTileAt(x + 1, y + 1, oobTile) : oobTile;
            } else {
                tileData[6] = oobTile;
                tileData[7] = oobTile;
                tileData[8] = oobTile;
            }
        } else {
            var i = 0;
            for (int oy = -ScanHeight / 2; oy <= ScanHeight / 2; oy++) {
                for (int ox = -ScanWidth / 2; ox <= ScanWidth / 2; ox++) {
                    tileData[i++] = checker.GetTileAt(x + ox, y + oy, oobTile);
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

    private readonly Dictionary<StringRef, AutotiledSprite[]?> _fastTileDataToTiles = new();
    
    private bool TryFindFirstMaskMatch(char[] tileData,
        [NotNullWhen(true)] out AutotiledSprite[]? tiles) {
        var tileDataRef = StringRef.FromSharedBuffer(tileData);

        if (_fastTileDataToTiles.TryGetValue(tileDataRef, out var cached)) {
            tiles = cached;
            return tiles is { };
        }
        
        // This combination of tiles is not yet known
        
        var allTiles = Tiles;
        foreach (var t in allTiles) {
            if (!MatchingMask(t.Mask, tileData))
                continue;
            
            tiles = t.Tiles;
            _fastTileDataToTiles[tileDataRef.CloneIntoReadOnly()] = tiles;
            return true;
        }
        
        tiles = null;
        _fastTileDataToTiles[tileDataRef.CloneIntoReadOnly()] = tiles;
        return false;
    }
    
    internal bool MatchingMask(TilesetMask maskData, ReadOnlySpan<char> tileData) {
        var tl = tileData.Length;
        var mask = maskData.StringValue;
        var sl = mask.Length;

        // matches a mask of any size
        for (int i = 0; i < tl && i < sl; i++) {
            var realTile = tileData[i];

            switch (maskData.TypeAt(i)) {
                case TilesetMask.MaskType.Any:
                    continue;
                case TilesetMask.MaskType.Empty:
                    if (IsTileConnected(realTile))
                        return false;
                    break;
                case TilesetMask.MaskType.Tile:
                    if (!IsTileConnected(realTile))
                        return false;
                    break;
                case TilesetMask.MaskType.NotThis:
                    if (realTile == Id)
                        return false;
                    break;
                case TilesetMask.MaskType.Custom:
                    var maskTile = mask[i];

                    if (!Defines.TryGetValue(maskTile, out var define)) {
                        break;
                    }

                    if (!define.Match(realTile))
                        return false;
                    
                    break;
            }
        }

        return true;
    }

    private bool IsTileConnected(char tile) {
        return tile != '0' && (!IgnoreAll || tile == Id) && (!Ignores?.Contains(tile) ?? true);
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
    /// If the location is out of bounds, always return 'def'!
    /// </summary>
    char GetTileAt(int x, int y, char def);

    /// <summary>
    /// Whether to treat tiles out of bounds as existing or not.
    /// </summary>
    bool ExtendOutOfBounds();
}

public readonly struct HollowRectTileChecker(int w, int h, char id) : ITileChecker {
    public bool IsInBounds(int x, int y) => x >= 0 && x < w && y < h && y >= 0;
    public bool IsConnectedTileAt(int x, int y, TilesetData data) => x == 0 || x == w - 1 || y == h - 1 || y == 0;

    public char GetTileAt(int x, int y, char def) => IsConnectedTileAt(x, y, null!) ? id : def;
    
    public bool ExtendOutOfBounds() {
        return false;
    }
}
        
public readonly struct FilledRectTileChecker(int w, int h, char id) : ITileChecker {
    public bool IsInBounds(int x, int y) => x >= 0 && x < w && y < h && y >= 0;
    
    public bool IsConnectedTileAt(int x, int y, TilesetData data) => IsInBounds(x, y);
    
    public char GetTileAt(int x, int y, char def) => id;
    
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
    public char GetTileAt(int x, int y, char def) {
        if (x >= 0 && x < t.GetLength(0) && y < t.GetLength(1) && y >= 0) {
            var tile = t[x, y];
            return tile;
        }
        return def;
    }

    public bool ExtendOutOfBounds()
        => tilesOob;
}