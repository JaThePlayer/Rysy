using CommunityToolkit.HighPerformance;
using Rysy.Helpers;
using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Xml;

namespace Rysy.Graphics;

public sealed class Autotiler {
    public sealed class AutotilerData {
        public string Filename;
        public VirtTexture Texture = null!;
        public List<(string mask, Point[] tiles)> Tiles = new();

        public Point[] Center = null!;
        public Point[] Padding = null!;

        public char[]? Ignores;

        public bool IgnoreAll;

        /// <summary>
        /// Stores a tilegrid bitmask -> possible tiles.
        /// Used for speeding up <see cref="GetFirstMatch(char[,], int, int, int, int, out Point[]?)"/>
        /// </summary>
        private Dictionary<long, Point[]> FastTileDataToTiles = new();

        private bool TryFindFirstMaskMatch(Span<bool> tileData, [NotNullWhen(true)] out Point[]? tiles) {
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

        public bool GetFirstMatch(int x, int y, int w, int h, [NotNullWhen(true)] out Point[]? tiles) {
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

        public bool GetFirstMatch(char[,] t, int x, int y, int w, int h, [NotNullWhen(true)] out Point[]? tiles) {
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

            if (FastTileDataToTiles.TryGetValue(bitmask, out tiles)) {
                return true;
            }

            if (TryFindFirstMaskMatch(tileData, out tiles)) {
                FastTileDataToTiles[bitmask] = tiles;
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
                return true;
            }
        }

        public static bool MatchingMask(string mask, Span<bool> tileData) {
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

    public Dictionary<char, AutotilerData> Tilesets = new();

    public bool IsLoaded() => Tilesets.Count > 0;

    public CacheToken TilesetDataCacheToken = new();

    public void UseCache(Cache<Stream?> cache) {
        cache.Token.OnInvalidate += () => {
            using var str = cache.Value;
            if (str is { }) { // Stream can be null if the file gets locked forever for some reason
                ReadFromXml(str);

                cache.Token.Reset();

                TilesetDataCacheToken.Invalidate();
                TilesetDataCacheToken.Reset();
            } else {
                Logger.Write("[Autotiler]", LogLevel.Error, $"Failed to reload tileset, most likely because the xml file got locked by another process.");
            }
        };

        cache.Token.Invalidate();
    }

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

                AutotilerData autotilerData = new();
                autotilerData.Filename = path;
                autotilerData.Texture = GFX.Atlas[$"tilesets/{path}"];
                autotilerData.Ignores = ignores;
                autotilerData.IgnoreAll = ignores?.Contains('*') ?? false;

                if (tileset.Attributes?["copy"]?.InnerText is [var copy]) {
                    var copied = Tilesets[copy];
                    autotilerData.Tiles = new(copied.Tiles);
                    autotilerData.Padding = copied.Padding;
                    autotilerData.Center = copied.Center;
                }



                var tiles = tileset.ChildNodes.OfType<XmlNode>().Where(n => n.Name == "set").Select(n => {
                    var mask = n.Attributes?["mask"]?.InnerText ?? throw new Exception($"<set> missing mask for tileset {id}");
                    var tiles = n.Attributes?["tiles"]?.InnerText ?? throw new Exception($"<set> missing tiles for tileset {id}");

                    switch (mask) {
                        case "padding":
                            autotilerData.Padding = ParseTiles(tiles);
                            return (null!, null!);
                        case "center":
                            autotilerData.Center = ParseTiles(tiles);
                            return (null!, null!);
                        default:
                            return (mask.Replace("-", ""), ParseTiles(tiles));
                    }
                }).Where(x => x.Item1 is { }).ToList();

                autotilerData.Tiles.AddRange(tiles);
                Tilesets[id] = autotilerData;
            }
        }
    }

    private static Point[] ParseTiles(string tiles) {
        return tiles.Split(';').Select(x => {
            var split = x.Split(',');
            return new Point(int.Parse(split[0]) * 8, int.Parse(split[1]) * 8);
        }).ToArray();
    }


    /// <summary>
    /// Generates sprites needed to render a rectangular tile grid fully made up of a specified id
    /// </summary>
    public IEnumerable<ISprite> GetSprites(Vector2 position, char id, int w, int h) {
        if (id == '0')
            yield break;

        if (!Tilesets.TryGetValue(id, out var data)) {
            LogUnknownTileset((int) position.X, (int) position.Y, id);
            yield return ISprite.Rect(new((int) position.X, (int) position.Y, w * 8, h * 8), Color.Pink);
            yield break;
        }

        for (int x = 0; x < w; x++) {
            for (int y = 0; y < h; y++) {
                if (!data.GetFirstMatch(x, y, w, h, out var tiles)) {
                    yield return ISprite.Rect(new((int) position.X + x * 8, (int) position.Y + y * 8, 8, 8), Color.Red);
                    continue;
                }

                var pos = position + new Vector2(x * 8, y * 8);
                var tile = tiles[RandomExt.SeededRandom(pos) % (uint) tiles.Length];
                yield return ISprite.FromTexture(pos, data.Texture).CreateSubtexture(tile.X, tile.Y, 8, 8);
            }
        }
    }

    /// <summary>
    /// Generates sprites needed to render a tile grid
    /// </summary>
    public IEnumerable<ISprite> GetSprites(Vector2 position, char[,] tileGrid) {
        List<char>? unknownTilesetsUsed = null;
        var w = tileGrid.GetLength(0);
        var h = tileGrid.GetLength(1);

        AutotiledSpriteList l = new() {
            Sprites = new AutotiledSpriteList.AutotiledSprite[w, h],
            Pos = position,
        };

        for (int i = 0; i < l.Sprites.GetLength(0); i++) {
            for (int j = 0; j < l.Sprites.GetLength(1); j++) {
                l.Sprites[i, j] = new();
            }
        }

        for (int x = 0; x < w; x++) {
            for (int y = 0; y < h; y++) {
                var c = tileGrid[x, y];
                if (c == '0')
                    continue;

                if (!Tilesets.TryGetValue(c, out var data)) {
                    unknownTilesetsUsed ??= new();
                    if (!unknownTilesetsUsed.Contains(c)) {
                        unknownTilesetsUsed.Add(c);
                        LogUnknownTileset(x, y, c);
                    }

                    yield return ISprite.Rect(new((int) position.X + x * 8, (int) position.Y + y * 8, 8, 8), Color.Pink);
                    continue;
                }

                if (!data.GetFirstMatch(tileGrid, x, y, w, h, out var tiles)) {
                    yield return ISprite.Rect(new((int) position.X + x * 8, (int) position.Y + y * 8, 8, 8), Color.Red);
                    continue;
                }


                //var tile = tiles[random.Next(0, tiles.Length)];
                var tile = tiles[RandomExt.SeededRandom(x, y) % (uint) tiles.Length];

                l.Sprites[x, y] = new() {
                    T = data.Texture,
                    Subtext = ISprite.FromTexture(default, data.Texture).GetSubtextureRect(tile.X, tile.Y, 8, 8)
                };
            }
        }
        yield return l;
    }

    private static void LogUnknownTileset(int x, int y, char c) {
        Logger.Write("Autotiler", LogLevel.Warning, $"Unknown tileset {c} ({(int) c}) at {{{x},{y}}} (and possibly more)");
    }

    internal class AutotiledSpriteList : ISprite {
        public int? Depth { get; set; }
        public Color Color { get; set; } = Color.White;

        public void MultiplyAlphaBy(float alpha) {
            Color *= alpha;
        }

        public bool IsLoaded => Sprites.Cast<AutotiledSprite>().All(s => s.T is null || s.T.Texture is { });

        public AutotiledSprite[,] Sprites;

        public Vector2 Pos;

        public AutotiledSpriteList() {
        }

        public void Render(Camera? cam, Vector2 offset) {
            var b = GFX.Batch;
            var sprites = Sprites;

            int left, right, top, bot;
            if (cam is { }) {
                var scrPos = cam.Pos - offset;
                left = Math.Max(0, (int) scrPos.X / 8);
                right = (int) Math.Min(sprites.GetLength(0), left + float.Round(cam.Viewport.Width / cam.Scale / 8) + 1);
                top = Math.Max(0, (int) scrPos.Y / 8);
                bot = (int) Math.Min(sprites.GetLength(1), top + float.Round(cam.Viewport.Height / cam.Scale / 8) + 1);
            } else {
                left = 0;
                top = 0;
                right = sprites.GetLength(0);
                bot = sprites.GetLength(1);
            }

            var sprSpan = sprites.AsSpan2D();
            var color = Color;
            for (int x = left; x < right; x++) {
                for (int y = top; y < bot; y++) {
                    ref var s = ref sprSpan.DangerousGetReferenceAt(x, y);// ref sprites[x, y];

                    if (s.T?.Texture is { } t)
                        b.Draw(t, new Vector2(Pos.X + x * 8, Pos.Y + y * 8), s.Subtext, color);
                }
            }
        }

        public void Render() {
            Render(null, default);
        }

        public struct AutotiledSprite {
            public VirtTexture T;
            public Rectangle Subtext;

            public AutotiledSprite() {
            }
        }

    }

}
