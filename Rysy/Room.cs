using Microsoft.Xna.Framework.Graphics;
using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy;

public sealed class Room : IPackable {
    public Room() {
        RenderCacheToken = new(ClearRenderCache);
    }

    public CacheToken RenderCacheToken;

    private Map _map = null!;
    public Map Map {
        get => _map;
        internal set {
            _map = value;
        }
    }

    public int X, Y, Width, Height;

    public RoomAttributes Attributes;

    public Rectangle Bounds => new(X, Y, Width, Height);

    public TypeTrackedList<Entity> Entities = new();
    public TypeTrackedList<Entity> Triggers = new();

    public List<Decal> BgDecals = new();
    public List<Decal> FgDecals = new();

    public Tilegrid FG = null!;
    public Tilegrid BG = null!;

    /// <summary>
    /// Currently unparsed
    /// </summary>
    public BinaryPacker.Element ObjTiles;

    private string _name = "";
    public string Name {
        get => _name;
        set {
            _name = value;
            ResetRandom();
        }
    }

    private List<ISprite>? CachedSprites;

    public int RandomSeed => Name.Sum(c => (int) c);

    public Random Random { get; private set; } = null!;

    private void ResetRandom() {
        Random = new(RandomSeed);
    }

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
        Attributes.WindPattern = from.Attr("windPattern", "");

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
                    BgDecals = child.Children.Select(Decal.Create).ToList();
                    break;
                case "fgdecals":
                    FgDecals = child.Children.Select(Decal.Create).ToList();
                    break;
                case "entities":
                    foreach (var entity in child.Children) {
                        Entities.Add(EntityRegistry.Create(entity, this, trigger: false));
                    }

                    break;
                case "triggers":
                    foreach (var entity in child.Children) {
                        Triggers.Add(EntityRegistry.Create(entity, this, trigger: true));
                    }
                    break;
                case "objtiles":
                    ObjTiles = child;
                    break;
                case "solids":
                    FG = Tilegrid.FromString(Width, Height, child.Attr("innerText"));
                    FG.Depth = Depths.FGTerrain;
                    FG.Autotiler = Map.FGAutotiler ?? throw new Exception("Map.FGAutotiler must not be null!");
                    FG.CacheToken = RenderCacheToken;
                    break;
                case "bg":
                    BG = Tilegrid.FromString(Width, Height, child.Attr("innerText"));
                    BG.Depth = Depths.BGTerrain;
                    BG.Autotiler = Map.BGAutotiler ?? throw new Exception("Map.BGAutotiler must not be null!");
                    BG.CacheToken = RenderCacheToken;
                    break;
            }
        }

        // It should be noted that there are two additional child elements - bgtiles and fgtiles.
        // These appear to follow the same format as the objtiles element and likely have a similar function.
        // However, they aren't parsed here simply because they are so rarely needed and object tiles work fine.
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

    internal void StartBatch(Camera camera) {
        GFX.Batch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, effect: null, camera.Matrix * (Matrix.CreateTranslation(X * camera.Scale, Y * camera.Scale, 0f)));
    }

    public void Render(Camera camera, bool selected) {
        if (!camera.IsRectVisible(Bounds)) {
            CachedSprites = null;
            return;
        }

        StartBatch(camera);
        ISprite.Rect(new(0, 0, Width, Height), Color.Gray * (selected ? .5f : .2f)).Render();

        if (CachedSprites is null) {
            ResetRandom();
            //using (var w = new ScopedStopwatch($"Generating sprites for {Name}"))
            CachedSprites = Entities.Select(e => {
                return NodeHelper.GetNodeSpritesFor(e).Concat(e.GetSprites().SetDepth(e.Depth));
            }).SelectMany(x => x)
            .Concat(BgDecals.Select<Decal, ISprite>(d => d.GetSprite(false)))
            .Concat(FgDecals.Select<Decal, ISprite>(d => d.GetSprite(true)))
            .Concat(FG.GetSprites(Random))
            .Concat(BG.GetSprites(Random))
            .Concat(Triggers.Select(e => {
                return NodeHelper.GetNodeSpritesFor(e).Concat(e.GetSprites().SetDepth(e.Depth));
            }).SelectMany(x => x))
            .OrderByDescending(x => x.Depth)
            .ToList();

            RenderCacheToken.Reset();

            if (Settings.Instance.LogTextureLoadTimes)
                StartTextureLoadTimer();
        }

        foreach (var item in CachedSprites) {
            if (item is Sprite s) {
                s.Render(camera, new(X, Y));
            } else if (item is Autotiler.AutotiledSpriteList s2) {
                s2.Render(camera, new(X, Y));
            } else
                item.Render();
        }

        // Darken the room if it's not selected
        if (!selected)
            ISprite.Rect(new(0, 0, Width, Height), Color.Black * .75f).Render();


        GFX.Batch.End();
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
}
