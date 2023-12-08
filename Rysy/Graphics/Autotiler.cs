using Rysy.Extensions;
using Rysy.Helpers;
using Rysy.Selections;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Xml;

namespace Rysy.Graphics;

public sealed class Autotiler {
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
        private readonly Dictionary<long, AutotiledSprite[]> _fastTileDataToTiles = new();

        private bool TryFindFirstMaskMatch(Span<bool> tileData, [NotNullWhen(true)] out AutotiledSprite[]? tiles) {
            var allTiles = Tiles;
            for (int i = 0; i < allTiles.Count; i++) {
                if (MatchingMask(allTiles[i].mask, tileData)) {
                    tiles = allTiles[i].tiles;
                    return true;
                }
            }

            tiles = null;
            return false;
        }

        internal bool GetFirstMatch(int x, int y, int w, int h, [NotNullWhen(true)] out AutotiledSprite[]? tiles) {
            Span<bool> mask = stackalloc bool[9];
            mask[0] = IsTileAt(w, h, x - 1, y - 1);
            mask[1] = IsTileAt(w, h, x, y - 1);
            mask[2] = IsTileAt(w, h, x + 1, y - 1);

            mask[3] = IsTileAt(w, h, x - 1, y);
            mask[4] = true;
            mask[5] = IsTileAt(w, h, x + 1, y);

            mask[6] = IsTileAt(w, h, x - 1, y + 1);
            mask[7] = IsTileAt(w, h, x, y + 1);
            mask[8] = IsTileAt(w, h, x + 1, y + 1);

            if (TryFindFirstMaskMatch(mask, out tiles)) {
                return true;
            }

            if (!IsTileAt(w, h, x - 2, y)
                || !IsTileAt(w, h, x + 2, y)
                || !IsTileAt(w, h, x, y - 2)
                || !IsTileAt(w, h, x, y + 2)) {
                tiles = Padding;
            } else {
                tiles = Center;
            }

            return true;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool IsTileAt(int w, int h, int x, int y) {
                return x > -1 && x < w && y < h && y > -1
                    ? true
                    : false;
            }
        }

        internal bool GetFirstMatch(char[,] t, int x, int y, int w, int h, bool tilesOOB, [NotNullWhen(true)] out AutotiledSprite[]? tiles) {
            Span<bool> tileData = stackalloc bool[9];
            char middleTile = t[x, y];
            tileData[0] = IsTileAt(x - 1, y - 1);
            tileData[1] = IsTileAt(x, y - 1);
            tileData[2] = IsTileAt(x + 1, y - 1);

            tileData[3] = IsTileAt(x - 1, y);
            tileData[4] = true;
            tileData[5] = IsTileAt(x + 1, y);

            tileData[6] = IsTileAt(x - 1, y + 1);
            tileData[7] = IsTileAt(x, y + 1);
            tileData[8] = IsTileAt(x + 1, y + 1);

            long bitmask =
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
                return true;
            }

            if (TryFindFirstMaskMatch(tileData, out tiles)) {
                _fastTileDataToTiles[bitmask] = tiles;
                return true;
            }

            if (!IsTileAt(x - 2, y)
             || !IsTileAt(x + 2, y)
             || !IsTileAt(x, y - 2)
             || !IsTileAt(x, y + 2)) {
                tiles = Padding;
            } else {
                tiles = Center;
            }
            //FastTileDataToTiles[bitmask] = tiles;

            return true;


            bool IsTileAt(int x, int y) {
                if (x > -1 & x < w & y < h & y > -1) {
                    var tile = t[x, y];
                    return tile != '0' && (!IgnoreAll || tile == middleTile) && (!Ignores?.Contains(tile) ?? true);
                }
                return tilesOOB;
            }
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

    public Dictionary<char, TilesetData> Tilesets = new();

    private bool _Loaded = false;
    public bool Loaded => _Loaded;

    public CacheToken TilesetDataCacheToken { get; set; } = new();

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
                };

                if (tileset.Attributes?["copy"]?.InnerText is [var copy]) {
                    var copied = Tilesets[copy];
                    tilesetData.Tiles = copied.Tiles.Select(t => (t.mask, t.tiles.Select(x => x.WithTexture(tilesetData.Texture)).ToArray())).ToList();
                    tilesetData.Padding = copied.Padding.Select(x => x.WithTexture(tilesetData.Texture)).ToArray();
                    tilesetData.Center = copied.Center.Select(x => x.WithTexture(tilesetData.Texture)).ToArray();
                }

                var tiles = tileset.ChildNodes.OfType<XmlNode>().Where(n => n.Name == "set").Select(n => {
                    var mask = n.Attributes?["mask"]?.InnerText ?? throw new Exception($"<set> missing mask for tileset {id}");
                    var tiles = n.Attributes?["tiles"]?.InnerText ?? throw new Exception($"<set> missing tiles for tileset {id}");

                    switch (mask) {
                        case "padding":
                            tilesetData.Padding = ParseTiles(tiles, tilesetData.Texture);
                            return (null!, null!);
                        case "center":
                            tilesetData.Center = ParseTiles(tiles, tilesetData.Texture);
                            return (null!, null!);
                        default:
                            return (mask.Replace("-", "", StringComparison.Ordinal), ParseTiles(tiles, tilesetData.Texture));
                    }
                }).Where(x => x.Item1 is { }).ToList();

                tiles.Sort((a, b) => {
                    int aSum = a.Item1.CountFast('x');
                    int bSum = b.Item1.CountFast('x');

                    return aSum - bSum;
                });

                tilesetData.Tiles.AddRange(tiles);
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

    /// <summary>
    /// Generates sprites needed to render a rectangular tile grid fully made up of a specified id
    /// </summary>
    public IEnumerable<ISprite> GetSprites(Vector2 position, char id, int tileWidth, int tileHeight, Color color) {
        if (!Loaded) {
            yield break;
        }

        if (id == '0')
            yield break;

        AutotiledSpriteList l = new() {
            Sprites = new AutotiledSprite[tileWidth, tileHeight],
            Pos = position,
            Color = color
        };

        if (!Tilesets.TryGetValue(id, out var data)) {
            LogUnknownTileset((int) position.X, (int) position.Y, id);
            yield return ISprite.Rect(new((int) position.X, (int) position.Y, tileWidth * 8, tileHeight * 8), Color.Pink);
            yield break;
        }

        var sprites = l.Sprites;
        for (int x = 0; x < tileWidth; x++) {
            for (int y = 0; y < tileHeight; y++) {
                if (!data.GetFirstMatch(x, y, tileWidth, tileHeight, out var tiles)) {
                    sprites[x, y] = AutotiledSprite.Invalid;
                    continue;
                }

                var pos = position + new Vector2(x * 8, y * 8);
                var tile = tiles[pos.SeededRandom() % (uint) tiles.Length];
                sprites[x, y] = tile;
            }
        }

        yield return l;
    }

    /// <summary>
    /// Generates sprites needed to render a tile grid
    /// </summary>
    public AutotiledSpriteList GetSprites(Vector2 position, char[,] tileGrid, Color color, bool tilesOOB = true) {
        if (!Loaded) {
            return new();
        }

        //using var watch = new ScopedStopwatch("Autotiler.GetSprites");
        
        List<char>? unknownTilesetsUsed = null;
        var w = tileGrid.GetLength(0);
        var h = tileGrid.GetLength(1);

        AutotiledSpriteList l = new() {
            Sprites = new AutotiledSprite[w, h],
            Pos = position,
            Color = color,
        };

        var sprites = l.Sprites;

        for (int x = 0; x < w; x++) {
            for (int y = 0; y < h; y++) {
                var c = tileGrid[x, y];
                if (c is '0' or '\0')
                    continue;

                SetTile(sprites, tileGrid, c, x, y, tilesOOB, ref unknownTilesetsUsed);
            }
        }
        return l;
    }

    public AutotiledSprite? GetSprite(char[,] tileGrid, char c, int x, int y, bool tilesOOB, ref List<char>? unknownTilesetsUsed) {
        if (c == '0') {
            return null;
        }
        
        if (!Tilesets.TryGetValue(c, out var data)) {
            unknownTilesetsUsed ??= new(1);
            if (!unknownTilesetsUsed.Contains(c)) {
                unknownTilesetsUsed.Add(c);
                LogUnknownTileset(x, y, c);
            }

            return AutotiledSprite.Missing;
        }

        if (!data.GetFirstMatch(tileGrid, x, y, tileGrid.GetLength(0), tileGrid.GetLength(1), tilesOOB, out var tiles)) {
            return AutotiledSprite.Invalid;
        }

        return tiles.Length == 1 ? tiles[0] : tiles[RandomExt.SeededRandom(x, y) % (uint) tiles.Length];
    }

    private static void LogUnknownTileset(int x, int y, char c) {
        Logger.Write("Autotiler", LogLevel.Warning, $"Unknown tileset {c} ({(int) c}) at {{{x},{y}}} (and possibly more)");
    }

    internal void SetTile(AutotiledSprite[,] sprites, char[,] tileGrid, char c, int x, int y, bool tilesOOB, ref List<char>? unknownTilesetsUsed) {
        sprites[x, y] = GetSprite(tileGrid, c, x, y, tilesOOB, ref unknownTilesetsUsed)!;
    }

    internal const int SpriteListPropagationRange = 2;
    
    internal void UpdateSpriteList(AutotiledSpriteList toUpdate, char[,] tileGrid, int changedX, int changedY, bool tilesOOB) {
        var sprites = toUpdate.Sprites;
        const int offset = SpriteListPropagationRange;

        var endX = (changedX + offset).AtMost(tileGrid.GetLength(0) - 1);
        var endY = (changedY + offset).AtMost(tileGrid.GetLength(1) - 1);
        for (int x = (changedX - offset).AtLeast(0); x <= endX; x++) {
            for (int y = (changedY - offset).AtLeast(0); y <= endY; y++) {
                var c = tileGrid[x, y];
                if (c == '0') {
                    sprites[x, y] = null!;
                    continue;
                }

                SetTile(sprites, tileGrid, c, x, y, tilesOOB, ref toUpdate.UnknownTilesetsUsed);
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
        const int offset = SpriteListPropagationRange;

        BitArray changeMask = new(tileGrid.Length);
        
        while (changed.MoveNext()) {
            var (changedX, changedY) = changed.Current;
            
            var endX = (changedX + offset).AtMost(tileGrid.GetLength(0) - 1);
            var endY = (changedY + offset).AtMost(tileGrid.GetLength(1) - 1);
            for (int x = (changedX - offset).AtLeast(0); x <= endX; x++) {
                for (int y = (changedY - offset).AtLeast(0); y <= endY; y++) {
                    var changeMaskLoc = changeMask.Get1dLoc(x, y, tileGrid.GetLength(0));
                    
                    if (changeMask.Get(changeMaskLoc)) {
                        continue;
                    }

                    changeMask.Set(changeMaskLoc, true);
                    
                    SetTile(sprites, tileGrid, tileGrid[x, y], x, y, tilesOOB, ref toUpdate.UnknownTilesetsUsed);
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

    public sealed record AutotiledSpriteList : ISprite {
        public int? Depth { get; set; }
        public Color Color { get; set; } = Color.White;
        internal List<char>? UnknownTilesetsUsed;

        public ISprite WithMultipliedAlpha(float alpha) {
            return this with {
                Color = Color * alpha,
            };
        }

        public bool IsLoaded
        {
            get {
                foreach (var item in Sprites) {
                    if (item is { Texture.Texture: not { } })
                        return false;
                }
                return true;
            }
        }

        public AutotiledSprite[,] Sprites;

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
}
