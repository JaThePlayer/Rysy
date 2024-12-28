using Rysy.Extensions;
using Rysy.Gui;
using Rysy.Helpers;
using Rysy.History;
using System.Buffers;
using System.Diagnostics;
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

    #if NET8_0_OR_GREATER
    private SearchValues<char> _filterSearchValues;
    #endif

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
#if NET8_0_OR_GREATER
        _filterSearchValues ??= SearchValues.Create(Filter);
        
        return Mode switch {
            Modes.Whitelist => _filterSearchValues.Contains(tileId),
            Modes.Blacklist => !_filterSearchValues.Contains(tileId),
            _ => throw new UnreachableException()
        };
#else
        return Mode switch {
            Modes.Whitelist => Filter.Contains(tileId),
            Modes.Blacklist => !Filter.Contains(tileId),
            _ => throw new UnreachableException()
        };
#endif
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

public sealed class TilesetData : IXmlBackedEntityData {
    public string Filename;

    public char Id { get; set; }
        
    public Autotiler Autotiler { get; set; }

    [JsonIgnore]
    public VirtTexture Texture { get; set; } = null!;
    public List<TilesetSet> Tiles { get; set; } = new();

    public AutotiledSprite[] Center { get; set; } = [];
    public AutotiledSprite[] Padding { get; set; } = [];

    public char[] Ignores { get; set; } = [];
    public char[] IgnoreExceptions { get; set; } = [];

    // Ignores, but without any of the values contained by IgnoreExceptions
    internal char[] IgnoresExceptExceptions = [];
    
    public Dictionary<char, TilesetDefine> Defines { get; set; } = new();

    public int ScanWidth { get; set; } = 3;
    public int ScanHeight { get; set; } = 3;

    public bool IgnoreAll { get; set; }
    internal string? DisplayName { get; set; }
    
    public bool Rainbow { get; set; }
    
    public bool IsTemplate => Filename.Contains("template", StringComparison.OrdinalIgnoreCase);

    public char? CopyFrom => Xml?.Attributes?["copy"]?.Value is [var first, ..] ? first : null;

    public XmlNode? Xml { get; set; }

    string IXmlBackedEntityData.EntityDataName => Filename;

    void IXmlBackedEntityData.OnXmlChanged() {
        Autotiler.ReadTilesetNode(Xml!, into: this);
    }

    private AutotiledSpriteList? _preview;
    private XnaWidgetDef? _xnaWidgetDef;
    private char[]? _tileDataSharedBuffer;
    private EntityData? _fakeData;
    private readonly Dictionary<StringRef, AutotiledSprite[]?> _fastTileDataToTiles = new();
    
    public EntityData FakeData => _fakeData ??= this.CreateFakeData();

    public FieldList GetFields(bool bg) {
        var fields = new FieldList(new {
            displayName = Fields.TilesetDisplayName("", () => bg, selfIsTileset: true).AllowNull().ConvertEmptyToNull(),
            sound = Fields.Dropdown(-1, CelesteEnums.SurfaceSounds, editable: false),
            path = Fields.AtlasPath("", @"^tilesets/(.*)$"),
            copy = Fields.TileDropdown('\0', bg, addDontCopyOption: true),
            ignores = Fields.List("", Fields.TileDropdown('1', bg, addWildcardOption: true)).WithMinElements(0),
            ignoreExceptions = Fields.List("", Fields.TileDropdown('1', bg)).WithMinElements(0),
            debris = Fields.AtlasPath("", @"^debris/(.*?)(?:00)?$"),
        });

        return fields;
    }
    
    public AutotiledSpriteList GetPreview(int previewSizePixels) {
        if (_preview is { } cached && cached.Sprites.GetLength(0) == previewSizePixels / 8)
            return cached;
            
        var tileGrid = new char[previewSizePixels / 8, previewSizePixels / 8];
        tileGrid.Fill(Id);
        _preview = Autotiler.GetSprites(Vector2.Zero, tileGrid, Color.White, tilesOOB: false);

        return _preview;
    }

    public XnaWidgetDef GetPreviewWidget(int previewSizePixels) {
        if (_xnaWidgetDef is { } existing) {
            if (existing.W != previewSizePixels)
                _xnaWidgetDef = null;
        }

        return _xnaWidgetDef ??= CreateWidget(previewSizePixels);
    }
    
    private XnaWidgetDef CreateWidget(int previewSizePixels) => new($"tile_{Id}_{GetDisplayName()}", previewSizePixels, previewSizePixels, () => {
        GetPreview(previewSizePixels).Render(SpriteRenderCtx.Default(true));
    });

    public string GetDisplayName() 
        => DisplayName ??= Filename.Split('/').Last().TrimPrefix("bg").Humanize();

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
            switch (maskData.TypeAt(i)) {
                case TilesetMask.MaskType.Any:
                    continue;
                case TilesetMask.MaskType.Empty:
                    if (IsTileConnected(tileData[i]))
                        return false;
                    break;
                case TilesetMask.MaskType.Tile:
                    if (!IsTileConnected(tileData[i]))
                        return false;
                    break;
                case TilesetMask.MaskType.NotThis:
                    if (tileData[i] == Id)
                        return false;
                    break;
                case TilesetMask.MaskType.Custom:
                    var maskTile = mask[i];

                    if (!Defines.TryGetValue(maskTile, out var define)) {
                        break;
                    }

                    if (!define.Match(tileData[i]))
                        return false;
                    
                    break;
            }
        }

        return true;
    }

    public bool IsTileConnected(char tile) {
        if (tile == '0')
            return false;

        if (tile == Id)
            return true;
        
        if (IgnoreAll)
            return IgnoreExceptions.Contains(tile);
        
        return !IgnoresExceptExceptions.Contains(tile);
    }

    public void ClearCaches() {
        _tileDataSharedBuffer = null;
        _xnaWidgetDef = null;
        _preview = null;
       // _fakeData = null;
        _fastTileDataToTiles.Clear();
    }
}

public interface ITileChecker {
    /// <summary>
    /// Returns whether the given location is within the bounds of the tilegrid
    /// </summary>
    bool IsInBounds(int x, int y);
    
    /// <summary>
    /// Checks whether the tileset stored in <paramref name="data" /> should connect to a tile located at x, y.
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
            return data.IsTileConnected(t[x, y]);
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