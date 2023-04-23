using KeraLua;
using Rysy.Entities;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Gui.Windows;
using Rysy.Helpers;
using Rysy.History;
using Rysy.LuaSupport;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;
using YamlDotNet.Core.Tokens;

namespace Rysy;

public sealed class Room : IPackable, ILuaWrapper {
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
        set => Attributes.X = value;
    }
    public int Y {
        get => Attributes.Y;
        set => Attributes.Y = value;
    }

    public Vector2 Pos => new(X, Y);

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
        var collection = Entities.Concat(Triggers);

        if (collection.Any())
            return collection.Max(e => e.ID) + 1;

        return 1;
    }

    public Entity? TryGetEntityById(int id) => Entities.FirstOrDefault(e => e.ID == id);

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
        Attributes.WindPattern = from.Enum("windPattern", WindPatterns.None);

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

        var children = new List<BinaryPacker.Element>();

        children.Add(FG.Pack("solids"));
        children.Add(BG.Pack("bg"));
        children.Add(ObjTiles);
        children.Add(new("fgtiles") {
            Attributes = new() {
                ["tileset"] = "Scenery",
            },
        });
        children.Add(new("bgtiles") {
            Attributes = new() {
                ["tileset"] = "Scenery",
            },
        });

        children.Add(new("entities") {
            Children = Entities.Select(e => e.Pack()).ToArray(),
        });

        children.Add(new("triggers") {
            Children = Triggers.Select(e => e.Pack()).ToArray(),
        });
        children.Add(new("fgdecals") {
            Attributes = new() {
                ["tileset"] = "Scenery",
            },
            Children = FgDecals.Select(d => d.Pack()).ToArray(),
        });
        children.Add(new("bgdecals") {
            Attributes = new() {
                ["tileset"] = "Scenery",
            },
            Children = BgDecals.Select(d => d.Pack()).ToArray(),
        });

        el.Children = children.Where(child => child is { }).ToArray();

        return el;
    }

    public Vector2 WorldToRoomPos(Camera camera, Vector2 world)
        => camera.ScreenToReal(world) - new Vector2(X, Y);

    public Point WorldToRoomPos(Camera camera, Point world) => WorldToRoomPos(camera, world.ToVector2()).ToPoint();

    public Rectangle WorldToRoomPos(Camera camera, Rectangle world)
        => RectangleExt.FromPoints(
            camera.ScreenToReal(world.Location.ToVector2()) - new Vector2(X, Y),
            camera.ScreenToReal((world.Location + world.Size).ToVector2()) - new Vector2(X, Y)
           );

    internal void StartBatch(Camera camera) {
        GFX.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, effect: null, camera.Matrix * (Matrix.CreateTranslation(X * camera.Scale, Y * camera.Scale, 0f)));
    }

    public void Render(Camera camera, bool selected) {
        var canvasReady = FullRenderCanvas is { IsDisposed: false };

        // canvases are not used in selected rooms, free the canvas
        if (canvasReady && selected)
            ClearRenderCache();

        // if the room takes up extremely tiny amounts of space due to huge zoom out, there's no point in rendering the interior
        var interiorVisible = 
               Width * camera.Scale >= 8 
            && Height * camera.Scale >= 8;

        if (!interiorVisible)
            ClearRenderCache();

        if (!camera.IsRectVisible(Bounds)) {
            ClearRenderCache();
            return;
        }

        StartBatch(camera);

        ISprite.Rect(new(0, 0, Width, Height), new Color(25, 25, 25, 255)).Render();

        if (interiorVisible)
            DrawRoomInterior(camera, selected, canvasReady);

        // Darken the room if it's not selected
        if (!selected)
            ISprite.Rect(new(0, 0, Width, Height), Color.Black * .75f).Render();

        // draw the colored border around the room
        ISprite.OutlinedRect(new(0, 0, Width, Height), Color.Transparent, CelesteEnums.RoomColors.AtOrDefault(Attributes.C, Color.White), outlineWidth: (int) (1f / camera.Scale).AtLeast(1)).Render();

        GFX.Batch.End();
    }

    private void DrawRoomInterior(Camera camera, bool selected, bool canvasReady) {
        if (!selected && canvasReady) {
            DrawFromCanvas(camera);
        } else {
            CacheSpritesIfNeeded();

            if (!selected && CachedSprites!.All(s => s.IsLoaded)) {
                RysyEngine.OnEndOfThisFrame += () => CacheIntoCanvas(camera);
            }

            if (selected)
                foreach (var item in CachedSprites!) {
                    item.Render(camera, new(X, Y));
                }
        }
    }

    internal void CacheSpritesIfNeeded() {
        if (CachedSprites is null) {
            //using var w = new ScopedStopwatch($"Generating sprites for {Name}");

            IEnumerable<ISprite> sprites = Array.Empty<ISprite>();
            var p = Persistence.Instance;
            var layer = p.EditorLayer;

            if (p.EntitiesVisible) {
                if (CachedEntitySprites is null) {
                    CachedEntitySprites = Entities.Select(e => {
                        var spr = e.GetSpritesWithNodes();
                        if (layer is { } && e.EditorLayer != layer)
                            spr = spr.Select(s => s.WithMultipliedAlpha(Settings.Instance.HiddenLayerAlpha));

                        return spr;
                    }).SelectMany(x => x).ToList();

                    EntityRenderCacheToken.Reset();
                }


                sprites = sprites.Concat(CachedEntitySprites);
            }

            if (p.TriggersVisible) {
                CachedTriggerSprites ??= Triggers.Select(e => {
                    var spr = e.GetSpritesWithNodes();
                    if (layer is { } && e.EditorLayer != layer)
                        spr = spr.Select(s => s.WithMultipliedAlpha(Settings.Instance.HiddenLayerAlpha));

                    return spr;
                }).SelectMany(x => x).ToList();
                TriggerRenderCacheToken.Reset();

                sprites = sprites.Concat(CachedTriggerSprites);
            }

            if (p.FGTilesVisible) {
                CachedFgTileSprites ??= FG.GetSprites().ToList();
                FgTilesRenderCacheToken.Reset();

                sprites = sprites.Concat(CachedFgTileSprites);
            }

            if (p.BGTilesVisible) {
                CachedBgTileSprites ??= BG.GetSprites().ToList();
                BgTilesRenderCacheToken.Reset();

                sprites = sprites.Concat(CachedBgTileSprites);
            }

            if (p.FGDecalsVisible) {
                CachedFgDecalSprites ??= FgDecals.Select<Entity, ISprite>(d => {
                    var spr = d.AsDecal()!.GetSprite();
                    if (layer is { } && d.EditorLayer != layer)
                        spr.Color *= Settings.Instance.HiddenLayerAlpha;

                    return spr;
                }).ToList();
                FgDecalsRenderCacheToken.Reset();

                sprites = sprites.Concat(CachedFgDecalSprites);
            }
            if (p.BGDecalsVisible) {
                CachedBgDecalSprites ??= BgDecals.Select<Entity, ISprite>(d => {
                    var spr = d.AsDecal()!.GetSprite();
                    if (layer is { } && d.EditorLayer != layer)
                        spr.Color *= Settings.Instance.HiddenLayerAlpha;

                    return spr;
                }).ToList();
                BgDecalsRenderCacheToken.Reset();

                sprites = sprites.Concat(CachedBgDecalSprites);
            }

            CachedSprites = sprites.OrderByDescending(x => x.Depth).ToList();

            RenderCacheToken.Reset();

            // w.Message = $"Generating {CachedSprites.Count} sprites for {Name}";

            if (Settings.Instance.LogTextureLoadTimes)
                StartTextureLoadTimer();
        }
    }

    private void DrawFromCanvas(Camera camera) {
        GFX.Batch.Draw(FullRenderCanvas, new Vector2(0, 0), Color.White);
    }

    private void CacheIntoCanvas(Camera camera) {
        if (CachedSprites is null)
            return;

        RenderTarget2D canvas;

        var gd = RysyEngine.GDM.GraphicsDevice;
        canvas = new(gd, Width, Height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        gd.SetRenderTarget(canvas);
        gd.Clear(Color.Transparent);

        GFX.Batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone);

        foreach (var item in CachedSprites) {
            item.Render();
        }

        GFX.Batch.End();
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

    public void ClearRenderCache() {
        ClearFullRenderCache();
        ClearEntityRenderCache();
        ClearTriggerRenderCache();
        ClearBgDecalsRenderCache();
        ClearFgDecalsRenderCache();
        ClearBgTilesRenderCache();
        ClearFgTilesRenderCache();
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
        int x = (int) roomPos.X / 8;
        int y = (int) roomPos.Y / 8;

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
        => new Rectangle(0, 0, Width, Height).Contains(roomPos);

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
            GetSelectionsInRectForGrid(rect, FG, list);
        }

        if ((layer & SelectionLayer.BGTiles) != 0) {
            GetSelectionsInRectForGrid(rect, BG, list);
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
            default:
                return null;
        }
    }

    private void GetSelectionsInRectForGrid(Rectangle rect, Tilegrid grid, List<Selection> into) {
        var pos = rect.Location.ToVector2().GridPosFloor(8);
        var pos2 = (rect.Location.ToVector2() + rect.Size.ToVector2()).GridPosFloor(8);


        var selection = grid.GetSelectionForArea(RectangleExt.FromPoints(pos, pos2).AddSize(1, 1).Mult(8));

        if (selection is { })
            into.Add(selection);
    }

    private void GetSelectionsInRectForEntities(Rectangle rect, TypeTrackedList<Entity> entities, List<Selection> into) {
        var layer = Persistence.Instance.EditorLayer;

        foreach (var entity in entities) {
            if (layer is { } && entity.EditorLayer != layer)
                continue;


            var mainSelect = entity.GetMainSelection();
            if (mainSelect.IsWithinRectangle(rect)) {
                into.Add(CreateSelectionFrom(entity));
            }

            if (entity.Nodes is { } nodes)
                for (int i = 0; i < nodes.Count; i++) {
                    var nodeSelect = entity.GetNodeSelection(i);
                    if (nodeSelect.IsWithinRectangle(rect)) {
                        into.Add(new Selection(new NodeSelectionHandler(entity, nodes[i])));
                    }
                }
        }
    }

    private static Selection CreateSelectionFrom(Entity entity) {
        return new Selection(new EntitySelectionHandler(entity));
    }

    private void GetSelectionsInRectForDecals(Rectangle rect, ListenableList<Entity> decals, List<Selection> into) {
        var layer = Persistence.Instance.EditorLayer;

        foreach (var decal in decals) {
            if (layer is { } && decal.EditorLayer != layer)
                continue;

            var selection = decal.GetMainSelection();

            if (selection.IsWithinRectangle(rect)) {
                into.Add(new Selection() { Handler = new EntitySelectionHandler(decal)});
            }
        }
    }

    public ISelectionHandler GetSelectionHandler() => new RoomSelectionHandler() { Room = this };

    public Room Clone() {
        var packed = Pack();
        var room = new Room();
        room.Map = Map;
        room.Unpack(packed);

        return room;
    }

    public int Lua__index(Lua lua, long key) {
        throw new NotImplementedException();
    }

    public int Lua__index(Lua lua, ReadOnlySpan<char> key) {
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
            default:
                break;
        }

        return 0;
    }
}

public sealed class RoomLuaWrapper : ILuaWrapper {
    /// <summary>
    /// Whether this wrapper was ever accessed.
    /// </summary>
    public bool Used { get; private set; } = false;

    internal List<string> Reasons = new();

    private Room Room { get; init; }

    public Room GetRoom() {
        Used = true;
        Reasons.Add("GetRoom");

        return Room;
    }

    public RoomLuaWrapper(Room room) {
        Room = room;
    }

    public int Lua__index(Lua lua, long key) {
        Used = true;

        Reasons.Add(key.ToString());

        return Room.Lua__index(lua, key);
    }

    public int Lua__index(Lua lua, ReadOnlySpan<char> key) {
        Used = true;
        Reasons.Add(key.ToString());

        return Room.Lua__index(lua, key);
    }
}

public class RoomSelectionHandler : ISelectionHandler {
    public Room Room { get; set; }
    private Vector2 RemainderOffset;

    public Rectangle Bounds => Room.Bounds;

    public object Parent => Room;

    public SelectionLayer Layer => SelectionLayer.Rooms;

    public void ClearCollideCache() {
    }

    public IHistoryAction DeleteSelf() {
        return new RoomDeleteAction(Room.Map, Room);
    }

    public bool IsWithinRectangle(Rectangle roomPos) => Bounds.Intersects(roomPos);

    public IHistoryAction MoveBy(Vector2 offset) {
        var tileOffset = ((offset + RemainderOffset) / 8).ToPoint();

        // since offset might be less than 8, let's accumulate the offsets that weren't sufficient to move tiles.
        RemainderOffset += offset - (tileOffset.ToVector2() * 8);

        if (tileOffset.X == 0 && tileOffset.Y == 0) {
            return new MergedAction(Array.Empty<IHistoryAction>());
        }

        return new RoomMoveAction(Room, tileOffset.X, tileOffset.Y);
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
        return new RoomResizeAction(Room, Room.Width + delta.X, Room.Height + delta.Y);
    }

    public IHistoryAction PlaceClone(Room room) {
        return new AddRoomAction(Room.Map, Room.Clone());
    }
}