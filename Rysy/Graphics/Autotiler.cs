using Rysy.Extensions;
using Rysy.Helpers;
using Rysy.Selections;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Xml;

namespace Rysy.Graphics;

public sealed class Autotiler {
    public Autotiler() {
        MissingTileset = new() {
            Id = '0',
            Autotiler = this,
            DisplayName = "MISSING",
            Filename = "MISSING",
            IgnoreAll = true,
            Texture = AutotiledSprite.Missing.Texture,
            Center = [ AutotiledSprite.Missing ],
            Padding = [ AutotiledSprite.Missing ],
            Tiles = new(),
        };
    }
    
    public TilesetData MissingTileset { get; private set; }

    public Dictionary<char, TilesetData> Tilesets = new();

    private bool _Loaded = false;
    public bool Loaded => _Loaded;

    public CacheToken TilesetDataCacheToken { get; set; } = new();

    // padding tiles need 5 scan width, even for 3x3 tileset masks!
    public int MaxScanWidth { get; private set; } = 5;
    public int MaxScanHeight { get; private set; } = 5;

    public void ReadFromXml(Stream stream) {
        Tilesets.Clear();

        var xml = new XmlDocument();
        xml.Load(stream);

        var data = xml["Data"] ?? throw new Exception("Tileset .xml missing starting <Data> tag");
        foreach (var child in data.ChildNodes) {
            if (child is XmlNode { Name: "Tileset" } tileset) {
                var id = tileset.Attributes?["id"]?.InnerText.FirstOrDefault() ?? throw new Exception($"<Tileset> node missing id");
                var path = tileset.Attributes?["path"]?.InnerText ?? throw new Exception($"<Tileset> node missing path");

                var ignores = tileset.Attributes?["ignores"]?.InnerText?.Split(',')?.Select(t => t.FirstOrDefault())?.ToArray();
                var ignoresAll = ignores?.Contains('*') ?? false;

                TilesetData tilesetData = new() {
                    Id = id,
                    Autotiler = this,
                    Filename = path,
                    Texture = GFX.Atlas[$"tilesets/{path}"],
                    Ignores = ignoresAll ? null : ignores,
                    IgnoreAll = ignoresAll,
                    DisplayName = tileset.Attributes?["displayName"]?.InnerText,
                    ScanWidth = tileset.Attributes?["scanWidth"]?.InnerText.ToInt() ?? 3,
                    ScanHeight = tileset.Attributes?["scanHeight"]?.InnerText.ToInt() ?? 3,
                };

                if (tileset.Attributes?["copy"]?.InnerText is [var copy]) {
                    var copied = Tilesets[copy];
                    tilesetData.Tiles = copied.Tiles.Select(t => t with { Tiles = t.Tiles.Select(x => x.WithTexture(tilesetData.Texture)).ToArray() }).ToList();
                    tilesetData.Padding = copied.Padding.Select(x => x.WithTexture(tilesetData.Texture)).ToArray();
                    tilesetData.Center = copied.Center.Select(x => x.WithTexture(tilesetData.Texture)).ToArray();
                }

                var tiles = tileset.ChildNodes.OfType<XmlNode>().Where(n => n.Name == "set").SelectWhereNotNull(n => {
                    var mask = n.Attributes?["mask"]?.InnerText ?? throw new Exception($"<set> missing mask for tileset {id}");
                    var tiles = n.Attributes?["tiles"]?.InnerText ?? throw new Exception($"<set> missing tiles for tileset {id}");

                    switch (mask) {
                        case "padding":
                            tilesetData.Padding = ParseTiles(tiles, tilesetData.Texture);
                            return null;
                        case "center":
                            tilesetData.Center = ParseTiles(tiles, tilesetData.Texture);
                            return null;
                        default:
                            return new TilesetSet(new TilesetMask(mask), ParseTiles(tiles, tilesetData.Texture));
                    }
                }).ToList();

                tiles.Sort((a, b) => {
                    // From Everest: https://github.com/EverestAPI/Everest/pull/241/files#diff-99921ff7c00e4bb7b7f2fb8dc659b13960215d3656dfafe8c466daccb229f86dR65
                    // Sorts the masks to give preference to more specific masks.
                    // Order is Custom Filters -> "Not This" -> "Any" -> Everything else
                    int aFilters = 0;
                    int bFilters = 0;
                    int aNots = 0;
                    int bNots = 0;
                    int aAnys = 0;
                    int bAnys = 0;

                    var aMask = a.Mask;
                    var bMask = b.Mask;
                    
                    for (int i = 0; i < aMask.Length && i < bMask.Length; i++) {
                        var aType = aMask.TypeAt(i);
                        var bType = bMask.TypeAt(i);

                        switch (aType) {
                            case TilesetMask.MaskType.Any:
                                aAnys++;
                                break;
                            case TilesetMask.MaskType.NotThis:
                                aNots++;
                                break;
                            case TilesetMask.MaskType.Custom:
                                aFilters++;
                                break;
                        }
                        
                        switch (bType) {
                            case TilesetMask.MaskType.Any:
                                bAnys++;
                                break;
                            case TilesetMask.MaskType.NotThis:
                                bNots++;
                                break;
                            case TilesetMask.MaskType.Custom:
                                bFilters++;
                                break;
                        }
                    }
                    if (aFilters > 0 || bFilters > 0)
                        return aFilters - bFilters;
                    if (aNots > 0 || bNots > 0)
                        return aNots - bNots;
                    return aAnys - bAnys;
                });

                tilesetData.Tiles.AddRange(tiles);

                if (!tilesetData.Validate()) {
                    Logger.Write("Autotiler", LogLevel.Error, $"Tileset {tilesetData.Id} has validation errors, not adding it to the tileset list!");
                    continue;
                }

                MaxScanWidth = int.Max(MaxScanWidth, tilesetData.ScanWidth);
                MaxScanHeight = int.Max(MaxScanHeight, tilesetData.ScanHeight);
                
                Tilesets[id] = tilesetData;
            }
        }

        TilesetDataCacheToken.Invalidate();
        TilesetDataCacheToken.Reset();
        _Loaded = true;
    }

    private static AutotiledSprite[] ParseTiles(string tiles, VirtTexture baseTexture) {
        return tiles.Split(';').Select(x => {
            var split = x.Split(',');
            return new Point(int.Parse(split[0], CultureInfo.InvariantCulture) * 8, int.Parse(split[1], CultureInfo.InvariantCulture) * 8);
        }).Select(p => AutotiledSprite.Create(baseTexture, p)).ToArray();
    }

    public string GetTilesetDisplayName(char c) {
        if (!Tilesets.TryGetValue(c, out var data)) {
            return $"Unknown: {c}";
        }

        return data.GetDisplayName();
    }

    public TilesetData? GetTilesetData(char c) {
        if (Tilesets.TryGetValue(c, out var data)) {
            return data;
        }
        return null;
    }

    public AutotiledSpriteList GetSprites<T>(Vector2 position, T tileChecker, int tileWidth, int tileHeight, Color color) where T : struct, ITileChecker {
        if (!Loaded) {
            return new();
        }

        //using var watch = tileWidth * tileHeight > 300 ? new ScopedStopwatch($"Autotiler.GetSprites - {tileWidth}x{tileHeight} [{tileChecker.GetType().Name}]") : null;
        
        List<char>? unknownTilesetsUsed = null;

        AutotiledSpriteList l = new() {
            Sprites = new AutotiledSprite[tileWidth, tileHeight],
            Pos = position,
            Color = color,
        };

        var sprites = l.Sprites;
        for (int x = 0; x < tileWidth; x++) {
            for (int y = 0; y < tileHeight; y++) {
                sprites[x, y] = GetSprite(tileChecker, x, y, ref unknownTilesetsUsed)!;
            }
        }
        return l;
    }
    
    /// <summary>
    /// Generates sprites needed to render a rectangular tile grid fully made up of a specified id
    /// </summary>
    public AutotiledSpriteList GetFilledRectSprites(Vector2 position, char id, int tileWidth, int tileHeight, Color color) {
        return GetSprites(position, new FilledRectTileChecker(tileWidth, tileHeight, id), tileWidth, tileHeight, color);
    }
    
    /// <summary>
    /// Generates sprites needed to render a hollow rectangular tile grid fully made up of a specified id
    /// </summary>
    public AutotiledSpriteList GetHollowRectSprites(Vector2 position, char id, int tileWidth, int tileHeight, Color color) {
        return GetSprites(position, new HollowRectTileChecker(tileWidth, tileHeight, id), tileWidth, tileHeight, color);
    }

    /// <summary>
    /// Generates sprites needed to render a tile grid
    /// </summary>
    public AutotiledSpriteList GetSprites(Vector2 position, char[,] tileGrid, Color color, bool tilesOOB = true) {
        return GetSprites(position, new TilegridTileChecker(tileGrid, tilesOOB), tileGrid.GetLength(0), tileGrid.GetLength(1), color);
    }
    
    public AutotiledSprite? GetSprite<T>(T tileChecker, int x, int y, ref List<char>? unknownTilesetsUsed)
    where T : struct, ITileChecker {
        var tilesetId = tileChecker.GetTileAt(x, y);
        if (tilesetId == '0') {
            return null;
        }
        
        if (!Tilesets.TryGetValue(tilesetId, out var tileset)) {
            unknownTilesetsUsed ??= new(1);
            if (!unknownTilesetsUsed.Contains(tilesetId)) {
                unknownTilesetsUsed.Add(tilesetId);
                LogUnknownTileset(x, y, tilesetId);
            }
            tileset = MissingTileset;
        }
        
        if (!tileset.GetFirstMatch(tileChecker, x, y, out var tiles) || tiles.Length == 0) {
            return AutotiledSprite.Invalid;
        }

        return tiles.Length == 1 ? tiles[0] : tiles[RandomExt.SeededRandom(x, y) % (uint) tiles.Length];
    }

    private static void LogUnknownTileset(int x, int y, char c) {
        Logger.Write("Autotiler", LogLevel.Warning, $"Unknown tileset {c} ({(int) c}) at {{{x},{y}}} (and possibly more)");
    }
    
    internal void UpdateSpriteList(AutotiledSpriteList toUpdate, char[,] tileGrid, int changedX, int changedY, bool tilesOOB) {
        var sprites = toUpdate.Sprites;
        int offsetX = MaxScanWidth / 2;
        int offsetY = MaxScanHeight / 2;

        var checker = new TilegridTileChecker(tileGrid, tilesOOB);
        var endX = (changedX + offsetX).AtMost(tileGrid.GetLength(0) - 1);
        var endY = (changedY + offsetY).AtMost(tileGrid.GetLength(1) - 1);
        for (int x = (changedX - offsetX).AtLeast(0); x <= endX; x++) {
            for (int y = (changedY - offsetY).AtLeast(0); y <= endY; y++) {
                sprites[x, y] = GetSprite(checker, x, y, ref toUpdate.UnknownTilesetsUsed);
            }
        }
    }
    
    /// <summary>
    /// Updates previously autotiled sprite lists to reflect changes done to all tiles pointed at by <paramref name="changed"/>.
    /// Also updates nearby tiles as needed by mask size.
    /// More efficient than individually calling <see cref="UpdateSpriteList"/> on each point.
    /// </summary>
    internal void BulkUpdateSpriteList<T>(AutotiledSpriteList toUpdate, char[,] tileGrid, T changed, bool tilesOOB)
        where T : IEnumerator<Point> {
        var sprites = toUpdate.Sprites;
        int offsetX = MaxScanWidth / 2;
        int offsetY = MaxScanHeight / 2;

        BitArray changeMask = new(tileGrid.Length);
        var checker = new TilegridTileChecker(tileGrid, tilesOOB);
        
        while (changed.MoveNext()) {
            var (changedX, changedY) = changed.Current;
            
            var endX = (changedX + offsetX).AtMost(tileGrid.GetLength(0) - 1);
            var endY = (changedY + offsetY).AtMost(tileGrid.GetLength(1) - 1);
            for (int x = (changedX - offsetX).AtLeast(0); x <= endX; x++) {
                for (int y = (changedY - offsetY).AtLeast(0); y <= endY; y++) {
                    var changeMaskLoc = changeMask.Get1dLoc(x, y, tileGrid.GetLength(0));
                    
                    if (changeMask.Get(changeMaskLoc)) {
                        continue;
                    }

                    changeMask.Set(changeMaskLoc, true);
                    
                    sprites[x, y] = GetSprite(checker, x, y, ref toUpdate.UnknownTilesetsUsed);
                }
            }
        }
    }
    
    /// <summary>
    /// Updates previously autotiled sprite lists to reflect changes done to all tiles pointed at by true values in <paramref name="changed"/>.
    /// Also updates nearby tiles as needed by mask size.
    /// More efficient than individually calling <see cref="UpdateSpriteList"/> on each point.
    /// </summary>
    internal void BulkUpdateSpriteList(AutotiledSpriteList toUpdate, char[,] tileGrid, BitArray changed, bool tilesOOB) {
        BulkUpdateSpriteList(toUpdate, tileGrid, changed.EnumerateTrue2dLocations(tileGrid.GetLength(0)).GetEnumerator(), tilesOOB);
    }
}

/// <summary>
/// Represents a sprite for a specific autotiled sprite.
/// For memory efficiency, one instance of this class should be created and re-used for each tile inside a given tileset.
/// </summary>
public sealed class AutotiledSprite {
    internal readonly VirtTexture Texture;
    internal readonly Rectangle Subtexture;
        
    /// <summary>
    /// The location as stored in the xml, as Subtext is different for atlased images.
    /// We need to store it for copying into different VirtTextures.
    /// </summary>
    internal readonly Point RelativeLocation;

    public AutotiledSprite WithTexture(VirtTexture newTexture) 
        => new(newTexture, RelativeLocation);

    public static AutotiledSprite Create(VirtTexture texture, Point location) => new(texture, location);

    private AutotiledSprite(VirtTexture texture, Point location) {
        Texture = texture;
        RelativeLocation = location;
        Subtexture = texture.GetSubtextureRect(RelativeLocation.X, RelativeLocation.Y, 8, 8, out _);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RenderAt(SpriteBatch b, Vector2 pos, Color color) {
        if (Texture.Texture is { } t)
            b.Draw(t, pos, Subtexture, color);
    }

    private static AutotiledSprite? _missing;
        
    /// <summary>
    /// Represents a missing tile
    /// </summary>
    public static AutotiledSprite Missing => _missing 
        ??= new(GFX.Atlas["Rysy:tilesets/missingTile"], new(0, 0));
        
    private static AutotiledSprite? _invalid;
        
    /// <summary>
    /// Represents an invalid tile
    /// </summary>
    public static AutotiledSprite Invalid => _invalid 
        ??= new(GFX.Atlas["Rysy:tilesets/missingTile"], new(0, 0));
}

public sealed record AutotiledSpriteList : ISprite {
    public int? Depth { get; set; }
    public Color Color { get; set; } = Color.White;
    internal List<char>? UnknownTilesetsUsed;

    public ISprite WithMultipliedAlpha(float alpha) {
        return this with { Color = Color * alpha, };
    }

    public bool IsLoaded {
        get {
            foreach (var item in Sprites) {
                if (item is { Texture.Texture: not { } })
                    return false;
            }

            return true;
        }
    }

    public AutotiledSprite?[,] Sprites = new AutotiledSprite[0,0];

    public Vector2 Pos;

    public AutotiledSpriteList() {
    }

    public void Render(Camera? cam, Vector2 offset) {
        var b = GFX.Batch;
        var sprites = Sprites;

        int left, right, top, bot;
        if (cam is { }) {
            var scrPos = -Pos + cam.Pos - offset;
            left = Math.Max(0, (int) scrPos.X / 8);
            right = (int) Math.Min(sprites.GetLength(0), left + float.Round(cam.Viewport.Width / cam.Scale / 8) + 2);
            top = Math.Max(0, (int) scrPos.Y / 8);
            bot = (int) Math.Min(sprites.GetLength(1), top + float.Round(cam.Viewport.Height / cam.Scale / 8) + 2);
        } else {
            left = 0;
            top = 0;
            right = sprites.GetLength(0);
            bot = sprites.GetLength(1);
        }

        var color = Color;
        for (int x = left; x < right; x++) {
            for (int y = top; y < bot; y++) {
                sprites[x, y]?.RenderAt(b, new Vector2(Pos.X + x * 8, Pos.Y + y * 8), color);
            }
        }
    }

    public void Render() {
        Render(null, default);
    }

    public ISelectionCollider GetCollider()
        => ISelectionCollider.FromRect(Pos, Sprites.GetLength(0) * 8, Sprites.GetLength(1) * 8);
}