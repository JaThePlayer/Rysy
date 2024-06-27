using KeraLua;
using LuaSharpener;
using Rysy.Entities;
using Rysy.Entities.Modded;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Gui.Windows;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Layers;
using Rysy.LuaSupport;
using Rysy.Selections;
using System.Text.Json.Serialization;

namespace Rysy;

public sealed class Room : IPackable, ILuaWrapper, ILuaTable {
    /// <summary>
    /// An empty room that can be used for mocking
    /// </summary>
    public static Room DummyRoom { get; } = new Room(Map.DummyMap, 10, 10);

    public Room() {
        RenderCacheToken = new(ClearRenderCache);
        EntityRenderCacheToken = new(ClearEntityRenderCache);
        TriggerRenderCacheToken = new(ClearTriggerRenderCache);
        FgDecalsRenderCacheToken = new(ClearFgDecalsRenderCache);
        BgDecalsRenderCacheToken = new(ClearBgDecalsRenderCache);
        FgTilesRenderCacheToken = new(ClearFgTilesRenderCache);
        BgTilesRenderCacheToken = new(ClearBgTilesRenderCache);

        Entities.OnChanged += ClearEntityRenderCache;
        Triggers.OnChanged += ClearTriggerRenderCache;

        BgDecals = new(ClearBgDecalsRenderCache);
        FgDecals = new(ClearFgDecalsRenderCache);
    }

    public Room(Map map, int width, int height) : this() {
        Map = map;

        Width = width;
        Height = height;

        FG = new(width, height);
        BG = new(width, height);

        SetupFGTilegrid();
        SetupBGTilegrid();
    }

    public CacheToken RenderCacheToken;
    public CacheToken EntityRenderCacheToken;
    public CacheToken TriggerRenderCacheToken;
    public CacheToken FgDecalsRenderCacheToken;
    public CacheToken BgDecalsRenderCacheToken;
    public CacheToken FgTilesRenderCacheToken;
    public CacheToken BgTilesRenderCacheToken;

    private RenderTarget2D? FullRenderCanvas;

    private bool CanUseCanvas => Width < 4098 && Height < 4098;

    private Map _map = null!;

    [JsonIgnore]
    public Map Map {
        get => _map;
        internal set {
            _map = value;
        }
    }

    public int X {
        get => Attributes.X;
        set => Attributes.X = value.SnapToGrid(8);
    }
    public int Y {
        get => Attributes.Y;
        set => Attributes.Y = value.SnapToGrid(8);
    }

    public Vector2 Pos {
        get => new(X, Y);
        set {
            X = (int) value.X;
            Y = (int) value.Y;
        }
    }

    public int Width {
        get => Attributes.Width;
        private set => Attributes.Width = value;
    }
    public int Height {
        get => Attributes.Height;
        private set => Attributes.Height = value;
    }

    private RoomAttributes _attributes = new();

    public RoomAttributes Attributes {
        get => _attributes;
        internal set {
            _attributes = value;
            ClearRenderCache();
        }
    }

    public Rectangle Bounds => new(X, Y, Width, Height);

    public EntityList Entities { get; init; } = new();
    public EntityList Triggers { get; init; } = new();

    public ListenableList<Entity> BgDecals { get; private set; }
    public ListenableList<Entity> FgDecals { get; private set; }

    public Tilegrid FG = null!;
    public Tilegrid BG = null!;

    /// <summary>
    /// Currently unparsed
    /// </summary>
    public BinaryPacker.Element ObjTiles;

    /// <summary>
    /// Whether this room is selected by the selection tool.
    /// This being true does not mean the room is the currently active room!
    /// </summary>
    public bool Selected { get; internal set; }

    public string Name {
        get => Attributes.Name;
        set {
            var old = Attributes.Name;
            if (old != value) {
                Attributes.Name = value;
                OnNameChanged?.Invoke(old, value);
            }

        }
    }

    /// <summary>
    /// Called with (oldName, newName) whenever this room's <see cref="Name"/> gets changed.
    /// </summary>
    public event Action<string, string>? OnNameChanged;

    private List<ISprite>? CachedSprites;
    private List<ISprite>? CachedEntitySprites;
    private List<ISprite>? CachedTriggerSprites;
    private List<ISprite>? CachedBgDecalSprites;
    private List<ISprite>? CachedFgDecalSprites;
    private List<ISprite>? CachedBgTileSprites;
    private List<ISprite>? CachedFgTileSprites;

    /// <summary>
    /// Gets an entity ID that's not yet used in this room.
    /// </summary>
    /// <returns></returns>
    public int NextEntityID() {
        if (Entities.Count > 0 || Triggers.Count > 0)
            return Entities.Concat(Triggers).Max(e => e.Id) + 1;

        return 1;
    }

    public Entity? TryGetEntityById(int id) => Entities.FirstOrDefault(e => e.Id == id);

    public void Unpack(BinaryPacker.Element from) {
        Attributes = new();
        Name = from.Attr("name");
        X = from.Int("x");
        Y = from.Int("y");
        Width = from.Int("width");
        Height = from.Int("height");

        Attributes.AltMusic = from.Attr("altMusic", "");
        Attributes.AmbienceProgress = from.Attr("ambienceProgress", "");
        Attributes.C = from.Int("c", 0);
        Attributes.CameraOffsetX = from.Int("cameraOffsetX", 0);
        Attributes.CameraOffsetY = from.Int("cameraOffsetY", 0);
        Attributes.Dark = from.Bool("dark", false);
        Attributes.DelayAltMusicFade = from.Bool("delayAltMusicFade", false);
        Attributes.DisableDownTransition = from.Bool("disableDownTransition", false);
        Attributes.Music = from.Attr("music", "");
        Attributes.MusicLayer1 = from.Bool("musicLayer1", false);
        Attributes.MusicLayer2 = from.Bool("musicLayer2", false);
        Attributes.MusicLayer3 = from.Bool("musicLayer3", false);
        Attributes.MusicLayer4 = from.Bool("musicLayer4", false);
        Attributes.MusicProgress = from.Attr("musicProgress", "");
        Attributes.Space = from.Bool("space", false);
        Attributes.Underwater = from.Bool("underwater", false);
        Attributes.Whisper = from.Bool("whisper", false);
        Attributes.WindPattern = from.Enum("windPattern", CelesteEnums.WindPatterns.None);

        // Normalize room size to be an increment of a whole tile.
        if (Width % 8 != 0) {
            Width += 8 - Width % 8;
        }

        if (Height % 8 != 0) {
            Height += 8 - Height % 8;
        }

        Rectangle bounds = new(0, 0, Width, Height);

        foreach (var child in from.Children) {
            switch (child.Name) {
                case "bgdecals":
                    BgDecals = child.Children.Select((e) => {
                        var d = Decal.Create(e, false, this);
                        d.Room = this;
                        return d;
                    }).ToListenableList<Entity>(ClearBgDecalsRenderCache);
                    break;
                case "fgdecals":
                    FgDecals = child.Children.Select((e) => {
                        var d = Decal.Create(e, true, this);
                        d.Room = this;
                        return d;
                    }).ToListenableList<Entity>(ClearFgDecalsRenderCache);
                    break;
                case "entities":
                    Entities.Clear();
                    foreach (var entity in child.Children) {
                        Entities.Add(EntityRegistry.Create(entity, this, trigger: false));
                    }

                    break;
                case "triggers":
                    Triggers.Clear();
                    foreach (var entity in child.Children) {
                        Triggers.Add(EntityRegistry.Create(entity, this, trigger: true));
                    }
                    break;
                case "objtiles":
                    ObjTiles = child;
                    break;
                case "solids":
                    FG = Tilegrid.FromString(Width, Height, child.Attr("innerText"));
                    SetupFGTilegrid();
                    break;
                case "bg":
                    BG = Tilegrid.FromString(Width, Height, child.Attr("innerText"));
                    SetupBGTilegrid();
                    break;
            }
        }

        Attributes.Checkpoint = Entities[typeof(Checkpoint)].Count > 0;

        // It should be noted that there are two additional child elements - bgtiles and fgtiles.
        // These appear to follow the same format as the objtiles element and likely have a similar function.
        // However, they aren't parsed here simply because they are so rarely needed and object tiles work fine.
    }

    private void SetupBGTilegrid() {
        BG.Depth = Depths.BGTerrain;
        BG.Autotiler = Map.BGAutotiler ?? throw new Exception("Map.BGAutotiler must not be null!");
        BG.RenderCacheToken = BgTilesRenderCacheToken;
    }

    private void SetupFGTilegrid() {
        FG.Depth = Depths.FGTerrain;
        FG.Autotiler = Map.FGAutotiler ?? throw new Exception("Map.FGAutotiler must not be null!");
        FG.RenderCacheToken = FgTilesRenderCacheToken;
    }

    public BinaryPacker.Element Pack() {
        BinaryPacker.Element el = new("level");
        var attr = Attributes;
        el.Attributes = new() {
            ["x"] = X,
            ["y"] = Y,
            ["width"] = Width,
            ["height"] = Height,
            ["name"] = Name,
            ["altMusic"] = attr.AltMusic,
            ["ambienceProgress"] = attr.AmbienceProgress,
            ["c"] = attr.C,
            ["cameraOffsetX"] = attr.CameraOffsetX,
            ["cameraOffsetY"] = attr.CameraOffsetY,
            ["dark"] = attr.Dark,
            ["delayAltMusicFade"] = attr.DelayAltMusicFade,
            ["disableDownTransition"] = attr.DisableDownTransition,
            ["music"] = attr.Music,
            ["musicLayer1"] = attr.MusicLayer1,
            ["musicLayer2"] = attr.MusicLayer2,
            ["musicLayer3"] = attr.MusicLayer3,
            ["musicLayer4"] = attr.MusicLayer4,
            ["musicProgress"] = attr.MusicProgress,
            ["space"] = attr.Space,
            ["underwater"] = attr.Underwater,
            ["whisper"] = attr.Whisper,
            ["windPattern"] = attr.WindPattern,
        };

        var trimEntities = Settings.Instance.TrimEntities;
        
        var children = new List<BinaryPacker.Element> {
            FG.Pack("solids"),
            BG.Pack("bg"),
            ObjTiles,
            new("fgtiles") {
                Attributes = new() {
                    ["tileset"] = "Scenery",
                },
            },
            new("bgtiles") {
                Attributes = new() {
                    ["tileset"] = "Scenery",
                },
            },

            new("entities") {
                Children = Entities.Select(e => e.Pack(trimEntities)).ToArray(),
            },

            new("triggers") {
                Children = Triggers.Select(e => e.Pack(trimEntities)).ToArray(),
            },
            new("fgdecals") {
                Attributes = new() {
                    ["tileset"] = "Scenery",
                },
                Children = FgDecals.Select(d => d.Pack(trimEntities)).ToArray(),
            },
            new("bgdecals") {
                Attributes = new() {
                    ["tileset"] = "Scenery",
                },
                Children = BgDecals.Select(d => d.Pack(trimEntities)).ToArray(),
            }
        };

        el.Children = children.Where(child => child is { }).ToArray();

        return el;
    }

    public IEnumerable<char> GetRainbowTilesets(TileLayer layer) {
        foreach (RainbowTilesetController c in Entities[typeof(RainbowTilesetController)]) {
            if (c.TileLayer != layer)
                continue;
            
            foreach (var id in c.Tilesets) {
                yield return id;
            }
        }
    }

    public Vector2 WorldToRoomPos(Camera camera, Vector2 world)
        => camera.ScreenToReal(world) - new Vector2(X, Y);

    public Point WorldToRoomPos(Camera camera, Point world) => WorldToRoomPos(camera, world.ToVector2()).ToPoint();

    public Rectangle WorldToRoomPos(Camera camera, Rectangle world)
        => RectangleExt.FromPoints(
            camera.ScreenToReal(world.Location.ToVector2()) - new Vector2(X, Y),
            camera.ScreenToReal((world.Location + world.Size()).ToVector2()) - new Vector2(X, Y)
           );

    internal void StartBatch(Camera camera) {
        GFX.BeginBatch(new SpriteBatchState(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, camera.Matrix * (Matrix.CreateTranslation(X * camera.Scale, Y * camera.Scale, 0f))));
    }

    public void Render(Camera camera, bool selected) {
        if (!selected && !camera.IsRectVisible(Bounds)) {
            ClearRenderCacheIfWanted();
            return;
        }

        var canvasReady = FullRenderCanvas is { IsDisposed: false };

        // canvases are not used in selected rooms, free the canvas
        if (canvasReady && selected)
            ClearFullRenderCache();

        // if the room takes up extremely tiny amounts of space due to huge zoom out, there's no point in rendering the interior
        var interiorVisible = selected || (
            Width * camera.Scale >= 8
            && Height * camera.Scale >= 8
        );
        
        // If we can't cache into a canvas and the room is not selected,
        // there's no point in trying to render the interior
        if (!CanUseCanvas && !selected)
            interiorVisible = false;

        if (!interiorVisible)
            ClearRenderCacheIfWanted();

        StartBatch(camera);

        if (!Settings.Instance.StylegroundPreview)
            ISprite.Rect(new(0, 0, Width, Height), new Color(25, 25, 25, 255)).Render();

        if (interiorVisible)
            DrawRoomInterior(camera, selected, canvasReady);

        // Darken the room if it's not selected
        if (!selected)
            ISprite.Rect(new(0, 0, Width, Height), Color.Black * .75f).Render();

        // draw the colored border around the room
        ISprite.OutlinedRect(new(0, 0, Width, Height), Color.Transparent, CelesteEnums.RoomColors.AtOrDefault(Attributes.C, Color.White), outlineWidth: (int) (1f / camera.Scale).AtLeast(1)).Render();

        GFX.EndBatch();
    }

    private void SetupRainbowTilesets() {
        Map.FGAutotiler.SetRainbowTiles(GetRainbowTilesets(TileLayer.FG));
        Map.BGAutotiler.SetRainbowTiles(GetRainbowTilesets(TileLayer.BG));
    }

    private void DrawRoomInterior(Camera camera, bool selected, bool canvasReady) {
        if (!selected && canvasReady) {
            DrawFromCanvas(camera);
        } else {
            CacheSpritesIfNeeded();

            if (!selected && CachedSprites!.TrueForAll(s => s.IsLoaded)) {
                RysyState.OnEndOfThisFrame += () => CacheIntoCanvas(camera);
            }

            if (selected) {
                SetupRainbowTilesets();

                var ctx = new SpriteRenderCtx(camera, new(X, Y), this, Settings.Instance?.Animate ?? false);
                foreach (var item in CachedSprites!) {
                    item.Render(ctx);
                }
            }
        }
    }

    internal void CacheSpritesIfNeeded() {
        if (CachedSprites is null) {
            using var w = Settings.Instance.LogSpriteCachingTimes ? new ScopedStopwatch($"Generating sprites for {Name}") : null;

            IEnumerable<ISprite> sprites = Array.Empty<ISprite>();
            var p = Persistence.Instance;

            if (p.EntitiesVisible) {
                if (CachedEntitySprites is null) {
#if LOG_PER_ENTITY_PERF
                    Dictionary<string, TimeSpan> TimePerType = new(StringComparer.Ordinal);
#endif

                    CachedEntitySprites = Entities.Select(e => {
                        var spr = e.GetSpritesWithNodes();
                        if (!e.EditorGroups.Enabled)
                            spr = spr.Select(s => s.WithMultipliedAlpha(Settings.Instance.HiddenLayerAlpha));

#if LOG_PER_ENTITY_PERF
                        return spr.Timed((elapsed) => {
                            var t = e.Name;
                            if (!TimePerType.TryGetValue(t, out TimeSpan time)) {
                                time = TimeSpan.Zero;
                            }

                            TimePerType[t] = time + elapsed;
                        });
#else
                        return spr;
#endif
                    }).SelectMany(x => x).ToList();

#if LOG_PER_ENTITY_PERF
                    TimePerType.OrderByDescending(kv => kv.Value).LogAsJson();
#endif

                    EntityRenderCacheToken.Reset();
                }


                sprites = sprites.Concat(CachedEntitySprites);
            }

            if (p.TriggersVisible) {
                CachedTriggerSprites ??= Triggers.Select(e => {
                    var spr = e.GetSpritesWithNodes();
                    if (!e.EditorGroups.Enabled)
                        spr = spr.Select(s => s.WithMultipliedAlpha(Settings.Instance.HiddenLayerAlpha));

                    return spr;
                }).SelectMany(x => x).ToList();
                TriggerRenderCacheToken.Reset();

                sprites = sprites.Concat(CachedTriggerSprites);
            }

            if (p.FGTilesVisible) {
                if (CachedFgTileSprites is null) {
                    var fgsprites = FG.GetSprites();
                    //fgsprites.UseRenderTarget(true);
                    
                    CachedFgTileSprites ??= fgsprites.ToList();
                }
                FgTilesRenderCacheToken.Reset();

                sprites = sprites.Concat(CachedFgTileSprites);
            }

            if (p.BGTilesVisible) {
                if (CachedBgTileSprites is null) {
                    var bgsprites = BG.GetSprites();
                    //bgsprites.UseRenderTarget(true);
                    
                    CachedBgTileSprites ??= bgsprites.ToList();
                }
                //CachedBgTileSprites ??= BG.GetSprites().ToList();
                BgTilesRenderCacheToken.Reset();

                sprites = sprites.Concat(CachedBgTileSprites);
            }

            if (p.FGDecalsVisible) {
                CachedFgDecalSprites ??= FgDecals.Select<Entity, ISprite>(d => {
                    var spr = d.AsDecal()!.GetSprite();
                    if (!d.EditorGroups.Enabled)
                        spr.Color *= Settings.Instance.HiddenLayerAlpha;

                    return spr;
                }).ToList();
                FgDecalsRenderCacheToken.Reset();

                sprites = sprites.Concat(CachedFgDecalSprites);
            }
            if (p.BGDecalsVisible) {
                CachedBgDecalSprites ??= BgDecals.Select<Entity, ISprite>(d => {
                    var spr = d.AsDecal()!.GetSprite();
                    if (!d.EditorGroups.Enabled)
                        spr.Color *= Settings.Instance.HiddenLayerAlpha;

                    return spr;
                }).ToList();
                BgDecalsRenderCacheToken.Reset();

                sprites = sprites.Concat(CachedBgDecalSprites);
            }

            CachedSprites = sprites.OrderByDescending(x => x.Depth).ToList();

            RenderCacheToken.Reset();
            
            if (w is { })
                w.Message = $"Generating {CachedSprites.Count} sprites for {Name}";

            if (Settings.Instance.LogTextureLoadTimes)
                StartTextureLoadTimer();
        }
    }

    private void DrawFromCanvas(Camera camera) {
        if (FullRenderCanvas is {} canvas)
            GFX.Batch.Draw(canvas, new Vector2(0, 0), Color.White);
    }

    private void CacheIntoCanvas(Camera camera) {
        if (CachedSprites is null)
            return;

        var gd = RysyState.GraphicsDevice;
        RenderTarget2D canvas = new(gd, Width, Height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        gd.SetRenderTarget(canvas);
        gd.Clear(Color.Transparent);

        GFX.BeginBatch(new SpriteBatchState(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone));

        SetupRainbowTilesets();
        var ctx = new SpriteRenderCtx(null, default, this, false);
        foreach (var item in CachedSprites) {
            item.Render(ctx);
        }

        GFX.EndBatch();
        gd.SetRenderTarget(null);

        ClearRenderCache();
        FullRenderCanvas = canvas;
    }

    private void StartTextureLoadTimer() {
        Task.Run(async () => {
            using (var w = new ScopedStopwatch($"Loading {CachedSprites!.Count} textures for {Name}"))
                while (!CachedSprites!.All(s => s.IsLoaded)) {
                    await Task.Delay(100);
                }
        });
    }

    /// <summary>
    /// Clears the render cache if the user wants to aggressively clear caches for better memory usage.
    /// If the cache needs to be cleared regardless of settings, call <see cref="ClearRenderCache"/>
    /// </summary>
    public void ClearRenderCacheIfWanted() {
        if (Settings.Instance?.ClearRenderCacheForOffScreenRooms ?? false)
            ClearRenderCacheAggressively();
    }

    public void ClearRenderCache() {
        ClearFullRenderCache();
        ClearEntityRenderCache();
        ClearTriggerRenderCache();
        ClearBgDecalsRenderCache();
        ClearFgDecalsRenderCache();
        ClearBgTilesRenderCache();
        ClearFgTilesRenderCache();
    }

    /// <summary>
    /// Clears the render cache, and as many intermediate caches used by entities, tilegrids, etc.
    /// </summary>
    public void ClearRenderCacheAggressively() {
        ClearRenderCache();
        
        // clear autotiled sprite caches for tilegrids
        FG.ClearSpriteCache();
        BG.ClearSpriteCache();
        
        foreach (var e in Entities) {
            e.ClearInnerCaches();
        }
        foreach (var e in Triggers) {
            e.ClearInnerCaches();
        }
        foreach (var e in BgDecals) {
            e.ClearInnerCaches();
        }
        foreach (var e in FgDecals) {
            e.ClearInnerCaches();
        }
    }

    public void ClearEntityRenderCache() {
        CachedEntitySprites?.Clear();
        CachedEntitySprites = null;
        ClearFullRenderCache();
    }

    public void ClearTriggerRenderCache() {
        CachedTriggerSprites = null;
        ClearFullRenderCache();
    }

    public void ClearFgDecalsRenderCache() {
        CachedFgDecalSprites = null;
        ClearFullRenderCache();
    }

    public void ClearBgDecalsRenderCache() {
        CachedBgDecalSprites = null;
        ClearFullRenderCache();
    }

    public void ClearFgTilesRenderCache() {
        CachedFgTileSprites = null;
        ClearFullRenderCache();
    }
    public void ClearBgTilesRenderCache() {
        CachedBgTileSprites = null;
        ClearFullRenderCache();
    }

    /// <summary>
    /// Clears the full list of cached sprite (without clearing the cache for individual layers),
    /// and the render target if it exists.
    /// </summary>
    private void ClearFullRenderCache() {
        FullRenderCanvas?.Dispose();
        FullRenderCanvas = null;
        CachedSprites = null;
    }

    public bool IsTileAt(Vector2 roomPos) {
        int x = (int) float.Floor(roomPos.X / 8f);
        int y = (int) float.Floor(roomPos.Y / 8f);

        return FG.SafeTileAt(x, y) != '0';
    }

    public bool IsSolidAt(Vector2 roomPos) {
        if (IsTileAt(roomPos))
            return true;

        foreach (var e in Entities[typeof(ISolid)]) {
            Rectangle bRect = e.Rectangle;

            if (bRect.Contains(roomPos)) {
                return true;
            }
        }

        return false;
    }

    public bool IsInBounds(Vector2 roomPos)
        => new Rectangle(0, 0, Width, Height).Contains(roomPos.ToPoint());

    public IEnumerable<Entity> GetAllEntitylikes() => Entities.Concat(Triggers).Concat(BgDecals).Concat(FgDecals);

    public IEnumerable<Entity> GetAllEntitylikesInGroup(EditorGroup? gr) => gr is null 
        ? Array.Empty<Entity>() 
        : GetAllEntitylikes().Where(e => e.EditorGroups.Contains(gr));
    
    public IEnumerable<Entity> GetAllEntitylikesInGroups(EditorGroupList? groups) => groups is null 
        ? Array.Empty<Entity>() 
        : GetAllEntitylikes().Where(e => e.EditorGroups.Any(groups.Contains));

    public Room Clone() {
        var packed = Pack();
        var room = new Room();
        room.Map = Map;
        room.Unpack(packed);

        return room;
    }

    /// <summary>
    /// Tries to get a rainbow color by using rainbow spinner controllers in this room.
    /// </summary>
    internal Color? GetOverridenRainbowColor(Vector2 pos, float time) {
        Color? ret = null;
        
        foreach (IRainbowSpinnerController controller in Entities[typeof(IRainbowSpinnerController)]) {
            var local = controller.IsLocal;
            var success = controller.TryGetRainbowColor(pos, time, out var color);
            
            // If a local controller returned something, make that a priority.
            if (local && success)
                return color;
            if (success)
                ret ??= color;
        }

        return ret;
    }
    
    #region Selections
    /// <summary>
    /// Returns a list of all selections within the provided rectangle, using <paramref name="layer"/> as a mask for which layers to use.
    /// Respects editor layers.
    /// </summary>
    public List<Selection> GetSelectionsInRect(Rectangle rect, SelectionLayer layer) {
        var list = new List<Selection>();

        if ((layer & SelectionLayer.Rooms) != 0) {
            var map = Map;


            foreach (var room in map.Rooms) {
                var handler = room.GetSelectionHandler();

                if (handler.IsWithinRectangle(rect)) {
                    list.Add(new Selection(handler));
                }
            }

            return list;
        }

        if ((layer & SelectionLayer.Entities) != 0) {
            GetSelectionsInRectForEntities(rect, Entities, list);
        }

        if ((layer & SelectionLayer.Triggers) != 0) {
            GetSelectionsInRectForEntities(rect, Triggers, list);
        }

        if ((layer & SelectionLayer.FGDecals) != 0) {
            GetSelectionsInRectForDecals(rect, FgDecals, list);
        }

        if ((layer & SelectionLayer.BGDecals) != 0) {
            GetSelectionsInRectForDecals(rect, BgDecals, list);
        }

        if ((layer & SelectionLayer.FGTiles) != 0) {
            GetSelectionsInRectForGrid(rect, FG, list, SelectionLayer.FGTiles);
        }

        if ((layer & SelectionLayer.BGTiles) != 0) {
            GetSelectionsInRectForGrid(rect, BG, list, SelectionLayer.BGTiles);
        }

        return list;
    }

    /// <summary>
    /// Returns a list of all selections for objects simillar to this one, to be used for double clicking in the selection tool.
    /// </summary>
    public List<Selection>? GetSelectionsForSimillar(object obj) {
        switch (obj) {
            case Decal d:
                return d.GetRoomList().Select(CreateSelectionFrom).ToList();
            case Entity e:
                if (e.GetRoomList() is TypeTrackedList<Entity> tracked) {
                    var sid = e.EntityData.SID;
                    return tracked.Where(e => e.EntityData.SID == sid).Select(CreateSelectionFrom).ToList();
                }
                return null;
            case Room room:
                if (room.Map is { } map) {
                    return map.Rooms.Select(r => new Selection(r.GetSelectionHandler())).ToList();
                }
                return null;
            default:
                return null;
        }
    }

    private void GetSelectionsInRectForGrid(Rectangle rect, Tilegrid grid, List<Selection> into, SelectionLayer layer) {
        var pos = rect.Location.ToVector2().GridPosFloor(8);
        var pos2 = (rect.Location.ToVector2() + rect.Size().ToVector2()).GridPosFloor(8);

        if (grid.GetSelectionForArea(RectangleExt.FromPoints(pos, pos2).AddSize(1, 1).Mult(8), layer) is { } s)
            into.Add(s);
    }

    private void GetSelectionsInRectForEntities(Rectangle rect, TypeTrackedList<Entity> entities, List<Selection> into) {
        foreach (var entity in entities) {
            if (!entity.EditorGroups.Enabled)
                continue;

            var selection = entity.CreateSelection();

            if (selection.Check(rect)) {
                into.Add(selection);
            }

            if (entity.Nodes is { } nodes)
                for (int i = 0; i < nodes.Count; i++) {
                    var nodeSelect = entity.CreateNodeSelection(i);
                    if (nodeSelect.Check(rect)) {
                        into.Add(nodeSelect);
                    }
                }
        }
    }

    private static Selection CreateSelectionFrom(Entity entity) => entity.CreateSelection();

    private void GetSelectionsInRectForDecals(Rectangle rect, ListenableList<Entity> decals, List<Selection> into) {
        foreach (var decal in decals) {
            if (!decal.EditorGroups.Enabled)
                continue;

            var selection = decal.GetMainSelection();

            if (selection.IsWithinRectangle(rect)) {
                into.Add(decal.CreateSelection());
            }
        }
    }


    private RoomSelectionHandler? _SelectionHandler;
    public ISelectionHandler GetSelectionHandler() => _SelectionHandler ??= new RoomSelectionHandler(this);

    #endregion

    #region LuaWrapper
    public int LuaIndex(Lua lua, long key) {
        throw new NotImplementedException();
    }

    public int LuaIndex(Lua lua, ReadOnlySpan<char> key) {
        switch (key) {
            case "entities":
                lua.PushWrapper(new EntityListWrapper(Entities));
                return 1;
            case "tilesFg":
                lua.PushWrapper(FG);
                return 1;
            case "tilesBg":
                lua.PushWrapper(BG);
                return 1;
            case "width":
                lua.PushNumber(Width);
                return 1;
            case "height":
                lua.PushNumber(Height);
                return 1;
            case "x":
                lua.PushNumber(X);
                return 1;
            case "y":
                lua.PushNumber(Y);
                return 1;
        }

        return 0;
    }
    #endregion

    object? ILuaTable.this[object? key] {
        get => key switch {
            "entities" => new EntityListWrapper(Entities),
            "triggers" => new EntityListWrapper(Triggers),
            "tilesFg" => FG,
            "tilesBg" => BG,
            "width" => Width,
            "height" => Height,
            "x" => X,
            "y" => Y,
            _ => null,
        };
        set => throw new NotImplementedException();
    }
}

public sealed class RoomLuaWrapper : ILuaWrapper {
    /// <summary>
    /// Whether this wrapper was ever accessed.
    /// </summary>
    public bool Used { get; private set; } = false;

    internal HashSet<string> Reasons = new();

    private Room Room { get; init; }

    public Room GetRoom() {
        Used = true;
        Reasons.Add("GetRoom");

        return Room;
    }

    public RoomLuaWrapper(Room room) {
        Room = room;
    }

    public int LuaIndex(Lua lua, long key) {
        Used = true;

        Reasons.Add(key.ToString(CultureInfo.InvariantCulture));

        return Room.LuaIndex(lua, key);
    }

    public int LuaIndex(Lua lua, ReadOnlySpan<char> key) {
        Used = true;
        Reasons.Add(key.ToString());

        return Room.LuaIndex(lua, key);
    }
}

public sealed class RoomSelectionHandler : ISelectionHandler {
    public Room Room { get; }
    public Rectangle Bounds => Room.Bounds;

    internal RoomSelectionHandler(Room room) {
        Room = room;
    }

    public void OnSelected() {
        Room.Selected = true;
    }

    public void OnDeselected() { 
        Room.Selected = false;
    }

    public object Parent => Room;

    public SelectionLayer Layer => SelectionLayer.Rooms;

    public Rectangle Rect => Bounds;

    public bool ResizableX => true;

    public bool ResizableY => true;

    public void ClearCollideCache() {
    }

    public IHistoryAction DeleteSelf() {
        return new RoomDeleteAction(Room);
    }

    public bool IsWithinRectangle(Rectangle roomPos) => Bounds.Intersects(roomPos);

    public IHistoryAction MoveBy(Vector2 offset) {
        var tileOffset = (offset / 8).ToPoint();

        if (tileOffset.X == 0 && tileOffset.Y == 0) {
            return new MergedAction(Array.Empty<IHistoryAction>());
        }

        return new RoomMoveAction(Room, tileOffset.X, tileOffset.Y);
    }

    private IHistoryAction ResizeByAndMoveInsides(Vector2 resize, Vector2 offset) {
        var tileOffset = (resize / 8).ToPoint();
        if (tileOffset.X == 0 && tileOffset.Y == 0) {
            return new MergedAction(Array.Empty<IHistoryAction>());
        }
        return new RoomResizeAndMoveInsidesAction(Room, tileOffset.X * 8, tileOffset.Y * 8, offset);
    }

    public void OnRightClicked(IEnumerable<Selection> selections) {
        RysyEngine.Scene.AddWindow(new RoomEditWindow(Room, false));
    }

    public BinaryPacker.Element? PackParent() {
        return Room.Pack();
    }

    public void RenderSelection(Color c) {
        ISelectionCollider.FromRect(Bounds).Render(c);
    }

    public (IHistoryAction, ISelectionHandler)? TryAddNode(Vector2? pos = null) {
        return null;
    }

    public IHistoryAction? TryResize(Point delta) {
        var tileOffset = (delta.ToVector2() / 8).ToPoint();

        return new RoomResizeAction(Room, Room.Width + tileOffset.X * 8, Room.Height + tileOffset.Y * 8);
    }

    public IHistoryAction? GetMoveOrResizeAction(Vector2 offset, NineSliceLocation grabbed) {
        var off = offset.ToPoint();
        return grabbed switch {
            NineSliceLocation.TopLeft => ResizeByAndMoveInsides(new(-off.X, -off.Y), new(off.X, off.Y)),
            NineSliceLocation.TopMiddle => ResizeByAndMoveInsides(new(0, -off.Y), new(0, off.Y)),
            NineSliceLocation.TopRight => ResizeByAndMoveInsides(new(off.X, -off.Y), new(0, off.Y)),
            NineSliceLocation.Left => ResizeByAndMoveInsides(new(-off.X, 0), new(off.X, 0)),
            NineSliceLocation.Right => TryResize(new(off.X, 0)),
            NineSliceLocation.BottomLeft => ResizeByAndMoveInsides(new(-off.X, off.Y), new(off.X, 0)),
            NineSliceLocation.BottomMiddle => TryResize(new(0, off.Y)),
            NineSliceLocation.BottomRight => TryResize(new(off.X, off.Y)),
            _ => MoveBy(offset),
        };
    }

    public IHistoryAction PlaceClone(Room room) {
        return new AddRoomAction(Room.Clone());
    }
}