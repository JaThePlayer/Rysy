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
            Tiles = [],
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

    public AnimatedTileBank? AnimatedTiles;

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
                    tilesetData.Tiles = copied.Tiles.Select(t => t with { Tiles = t.Tiles.Select(x => x.WithTileset(tilesetData)).ToArray() }).ToList();
                    tilesetData.Padding = copied.Padding.Select(x => x.WithTileset(tilesetData)).ToArray();
                    tilesetData.Center = copied.Center.Select(x => x.WithTileset(tilesetData)).ToArray();
                }
                
                tilesetData.Defines = tileset.ChildNodes.OfType<XmlNode>()
                    .Where(n => n.Name == "define")
                    .Select(n => new TilesetDefine(n, id))
                    .ToDictionary(d => d.Id, d => d);

                var tiles = tileset.ChildNodes.OfType<XmlNode>().Where(n => n.Name == "set").SelectWhereNotNull(n => {
                    var mask = n.Attributes?["mask"]?.InnerText ?? throw new Exception($"<set> missing mask for tileset {id}");
                    var tilesString = n.Attributes?["tiles"]?.InnerText ?? throw new Exception($"<set> missing tiles for tileset {id}");

                    var tiles = ParseTiles(tilesString, tilesetData);

                    if (AnimatedTiles is {} && n.Attributes?["sprites"]?.Value is { } spritesString) {
                        var sprites = spritesString.Split(',')
                            .SelectWhereNotNull(s => AnimatedTiles.Get(s));
                        
                        // To keep memory usage of individual tiles low, we store the animated tile together with each tile.
                        // This does mean we need to create tons of clones though...
                        tiles = IterationHelper.EachPair(tiles, sprites)
                            .SelectTuple((t, s) => t.WithAnimatedTile(s))
                            .ToArray();
                    }
                    
                    switch (mask) {
                        case "padding":
                            tilesetData.Padding = tiles;
                            return null;
                        case "center":
                            tilesetData.Center = tiles;
                            return null;
                        default:
                            return new TilesetSet(new TilesetMask(mask), tiles);
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

    private static AutotiledSprite[] ParseTiles(string tiles, TilesetData tileset) {
        return tiles.Split(';').Select(x => {
            var split = x.Split(',');
            return new Point(int.Parse(split[0], CultureInfo.InvariantCulture) * 8, int.Parse(split[1], CultureInfo.InvariantCulture) * 8);
        }).Select(p => AutotiledSprite.Create(tileset, p)).ToArray();
    }

    public string GetTilesetDisplayName(char c) {
        if (!Tilesets.TryGetValue(c, out var data)) {
            return $"Unknown: {c}";
        }

        return data.GetDisplayName();
    }

    public TilesetData? GetTilesetData(char c) {
        return Tilesets.GetValueOrDefault(c);
    }

    public AutotiledSpriteList GetSprites<T>(Vector2 position, T tileChecker, int tileWidth, int tileHeight, Color color) where T : struct, ITileChecker {
        if (!Loaded) {
            return new(this);
        }

        //using var watch = tileWidth * tileHeight > 300 ? new ScopedStopwatch($"Autotiler.GetSprites - {tileWidth}x{tileHeight} [{tileChecker.GetType().Name}]") : null;
        
        List<char>? unknownTilesetsUsed = null;

        AutotiledSpriteList l = new(this) {
            Sprites = new AutotiledSprite[tileWidth, tileHeight],
            Pos = position,
            Color = color,
        };

        var sprites = l.Sprites;
        for (int x = 0; x < tileWidth; x++) {
            for (int y = 0; y < tileHeight; y++) {
               var spr = GetSprite(tileChecker, x, y, ref unknownTilesetsUsed);
               sprites[x, y] = spr;
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
        var tilesetId = tileChecker.GetTileAt(x, y, '0');
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
                var sprite = GetSprite(checker, x, y, ref toUpdate.UnknownTilesetsUsed);
                sprites[x, y] = sprite;
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

        var changeMask = WrappedBitArray.Rent(tileGrid.Length);
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
                    
                    var sprite = GetSprite(checker, x, y, ref toUpdate.UnknownTilesetsUsed);
                    sprites[x, y] = sprite;
                }
            }
        }

        changeMask.ReturnToPool();
    }
    
    /// <summary>
    /// Sets which tilesets should use the rainbow effect.
    /// </summary>
    public void SetRainbowTiles(IEnumerable<char>? tilesets) {
        foreach (var (_, tileset) in Tilesets) {
            tileset.Rainbow = false;
        }

        if (tilesets is { }) {
            foreach (var t in tilesets) {
                if (GetTilesetData(t) is {} tileset)
                    tileset.Rainbow = true;
            }
        }
    }
}

/// <summary>
/// Represents a sprite for a specific autotiled sprite.
/// For memory efficiency, one instance of this class should be created and re-used for each tile inside a given tileset.
/// </summary>
public sealed class AutotiledSprite {
    internal readonly VirtTexture Texture;
    internal readonly Rectangle? Subtexture;
        
    /// <summary>
    /// The location as stored in the xml, as Subtext is different for atlased images.
    /// We need to store it for copying into different VirtTextures.
    /// </summary>
    internal readonly Point RelativeLocation;

    internal readonly AnimatedTileData? AnimatedTile;

    internal readonly TilesetData? Tileset;

    public AutotiledSprite WithTileset(TilesetData newTileset) 
        => new(newTileset, RelativeLocation);

    public AutotiledSprite WithAnimatedTile(AnimatedTileData tile)
        => Tileset is {} ? new(Tileset, RelativeLocation, tile) : new(Texture, RelativeLocation, tile);

    public static AutotiledSprite Create(TilesetData texture, Point location) => new(texture, location);

    private AutotiledSprite(VirtTexture texture, Point location, AnimatedTileData? animatedTile = null) {
        Texture = texture;
        RelativeLocation = location;
        Subtexture = texture.GetSubtextureRect(RelativeLocation.X, RelativeLocation.Y, 8, 8, out _);

        AnimatedTile = animatedTile;
    }
    
    private AutotiledSprite(TilesetData tileset, Point location, AnimatedTileData? animatedTile = null) : this(tileset.Texture, location, animatedTile) {
        Tileset = tileset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RenderAt(SpriteBatch b, Vector2 pos, Color color) {
        if (Texture.Texture is { } t) {
            b.Draw(t, pos, Subtexture, color);
        }
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
    
    internal Autotiler Autotiler { get; set; }

    private RenderTarget2D? _renderTarget;
    private bool _renderTargetCached;

    public bool IsRenderTargetEnabled() => _renderTarget is { };
    
    public void UseRenderTarget(bool enable) {
        if (enable) {
            _renderTarget ??= new RenderTarget2D(GFX.Batch.GraphicsDevice, 
                Sprites.GetLength(0) * 8, Sprites.GetLength(1) * 8, 
                false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _renderTargetCached = false;
        } else {
            _renderTarget?.Dispose();
            _renderTarget = null;
            _renderTargetCached = false;
        }
    }
    
    public AutotiledSpriteList(Autotiler autotiler) {
        Autotiler = autotiler;
    }

    private void ScheduleCache(Room? room) {
        RysyState.OnEndOfThisFrame += () => {
            var b = GFX.Batch;
            var gd = b.GraphicsDevice;
            
            RenderTargetBinding[]? renderTargetBindings = gd.GetRenderTargets();
            gd.SetRenderTarget(_renderTarget);
            gd.Clear(Color.Transparent);

            GFX.BeginBatch(new SpriteBatchState(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone));

            DoRender(SpriteRenderCtx.Default(false), default, room);
            
            GFX.EndBatch();
            gd.SetRenderTargets(renderTargetBindings);
        };
    }
    
    public void Render(SpriteRenderCtx ctx) {
        var shouldCache = _renderTarget is { } && !_renderTargetCached && IsLoaded;

        if (shouldCache) {
            _renderTargetCached = true;
            ScheduleCache(ctx.Room);
        }
        
        if (_renderTarget is { } cache && !shouldCache) {
            GFX.Batch.Draw(cache, Pos, Color.White);
            return;
        }
        
        DoRender(ctx, Pos, ctx.Room);
    }

    private void DoRender(SpriteRenderCtx ctx, Vector2 selfPos, Room? room) {
        var b = GFX.Batch;
        var sprites = Sprites;
        
        int left, right, top, bot;
        if (ctx.Camera is { } cam) {
            var scrPos = cam.Pos - ctx.CameraOffset - selfPos;
            left = Math.Max(0, (int) (scrPos.X / 8));
            right = (int) Math.Min(sprites.GetLength(0), float.Round((scrPos.X + cam.Viewport.Width / cam.Scale) / 8) + 2);
            top = Math.Max(0, (int) (scrPos.Y / 8));
            bot = (int) Math.Min(sprites.GetLength(1), float.Round((scrPos.Y + cam.Viewport.Height / cam.Scale) / 8) + 2);
        } else {
            left = 0;
            top = 0;
            right = sprites.GetLength(0);
            bot = sprites.GetLength(1);
        }

        var hasAnimatedTiles = false;

        var color = Color;
        for (int y = top; y < bot; y++) {
            for (int x = left; x < right; x++) {
                var sprite = sprites[x, y];
                if (sprite is null)
                    continue;

                var pos = new Vector2(selfPos.X + x * 8, selfPos.Y + y * 8);
                var tileColor = color;
                if (room is {} && sprite.Tileset is { Rainbow: true }) {
                    tileColor = ctx.Animate
                        ? ColorHelper.GetRainbowColorAnimated(room, pos) * (color.A / 255f)
                        : ColorHelper.GetRainbowColor(room, pos) * (color.A / 255f);
                }
                sprite.RenderAt(b, pos, tileColor);

                if (sprite.AnimatedTile is { }) {
                    hasAnimatedTiles = true;
                }
            }
        }

        if (hasAnimatedTiles) {
            const int extend = 1;
            right = (right + extend).AtMost(sprites.GetLength(0));
            bot = (bot + extend).AtMost(sprites.GetLength(1));
            for (int x = (left - extend).AtLeast(0); x < right; x++) {
                for (int y = (top - extend).AtLeast(0); y < bot; y++) {
                    var sprite = sprites[x, y];
                    if (sprite is not { AnimatedTile: { } animated })
                        continue;

                    var pos = new Vector2(selfPos.X + x * 8, selfPos.Y + y * 8);
                    animated.RenderAt(ctx, b, pos, color);
                }
            }
        }
    }

    public ISelectionCollider GetCollider()
        => ISelectionCollider.FromRect(Pos, Sprites.GetLength(0) * 8, Sprites.GetLength(1) * 8);
}