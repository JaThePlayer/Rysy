using Microsoft.Xna.Framework.Graphics;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.Triggers;

namespace Rysy;

public sealed class Room : IPackable
{
    public Room()
    {
        RenderCacheToken = new(ClearRenderCache);
    }

    public CacheToken RenderCacheToken;

    private Map _map = null!;
    public Map Map
    {
        get => _map;
        internal set
        {
            _map = value;
        }
    }

    public int X, Y, Width, Height;

    public Rectangle Bounds => new(X, Y, Width, Height);

    public TypeTrackedList<Entity> Entities = new();
    public TypeTrackedList<Entity> Triggers = new();

    public List<Decal> BgDecals = new();
    public List<Decal> FgDecals = new();

    public Tilegrid FG = null!;
    public Tilegrid BG = null!;

    private string _name = "";
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            ResetRandom();
        }
    }

    private List<ISprite>? CachedSprites;

    public int RandomSeed => Name.Sum(c => (int)c);

    public Random Random { get; private set; } = null!;

    private void ResetRandom()
    {
        Random = new(RandomSeed);
    }

    public void Unpack(BinaryPacker.Element from)
    {
        Name = from.Attr("name");
        X = from.Int("x");
        Y = from.Int("y");
        Width = from.Int("width");
        Height = from.Int("height");


        // Normalize room size to be an increment of a whole tile.
        if (Width % 8 != 0)
        {
            Width += 8 - Width % 8;
        }

        if (Height % 8 != 0)
        {
            Height += 8 - Height % 8;
        }

        Rectangle bounds = new(0, 0, Width, Height);

        foreach (var child in from.Children)
        {
            switch (child.Name)
            {
            case "bgdecals":
                BgDecals = child.Children.Select(Decal.Create).ToList();
                break;
            case "fgdecals":
                FgDecals = child.Children.Select(Decal.Create).ToList();
                break;
            case "entities":
                foreach (var entity in child.Children)
                {
                    Entities.Add(EntityRegistry.Create(entity, this, trigger: false));
                }

                break;
            case "triggers":
                foreach (var entity in child.Children)
                {
                    Triggers.Add(EntityRegistry.Create(entity, this, trigger: true));
                }
                break;
            case "objtiles":
                Logger.Write("Room.Unpack", LogLevel.Error, "todo: objtiles");
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

    public BinaryPacker.Element Pack()
    {
        throw new NotImplementedException();
    }

    public Vector2 WorldToRoomPos(Camera camera, Vector2 world)
        => camera.ScreenToReal(world) - new Vector2(X, Y);

    internal void StartBatch(Camera camera)
    {
        GFX.Batch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, effect: null, camera.Matrix * (Matrix.CreateTranslation(X * camera.Scale, Y * camera.Scale, 0f)));
    }

    public void Render(Camera camera, bool selected)
    {
        if (!camera.IsRectVisible(Bounds))
        {
            CachedSprites = null;
            return;
        }

        StartBatch(camera);
        ISprite.Rect(new(0, 0, Width, Height), Color.Gray * (selected ? .5f : .2f)).Render();

        if (CachedSprites is null)
        {
            ResetRandom();
            //using (var w = new ScopedStopwatch($"Generating sprites for {Name}"))
            CachedSprites = Entities.Select(e =>
            {
                return NodeHelper.GetNodeSpritesFor(e).Concat(e.GetSprites().SetDepth(e.Depth));
            }).SelectMany(x => x)
            .Concat(BgDecals.Select<Decal, ISprite>(d => d.GetSprite(false)))
            .Concat(FgDecals.Select<Decal, ISprite>(d => d.GetSprite(true)))
            .Concat(FG.GetSprites(Random))
            .Concat(BG.GetSprites(Random))
            .Concat(Triggers.Select(e =>
            {
                return NodeHelper.GetNodeSpritesFor(e).Concat(e.GetSprites().SetDepth(e.Depth));
            }).SelectMany(x => x))
            .OrderByDescending(x => x.Depth)
            .ToList();

            RenderCacheToken.Reset();

            if (Settings.Instance.LogTextureLoadTimes)
                StartTextureLoadTimer();
        }

        foreach (var item in CachedSprites)
        {
            if (item is Sprite s)
            {
                s.Render(camera, new(X, Y));
            }
            else if (item is Autotiler.AutotiledSpriteList s2)
            {
                s2.Render(camera, new(X, Y));
            }
            else
                item.Render();
        }

        // Darken the room if it's not selected
        if (!selected)
            ISprite.Rect(new(0, 0, Width, Height), Color.Black * .75f).Render();


        GFX.Batch.End();
    }

    private void StartTextureLoadTimer()
    {
        Task.Run(async () =>
        {
            using (var w = new ScopedStopwatch($"Loading {CachedSprites!.Count} textures for {Name}"))
                while (!CachedSprites!.All(s => s.IsLoaded))
                {
                    await Task.Delay(100);
                }
        });
    }

    public void ClearRenderCache()
    {
        CachedSprites = null;
    }

    public bool IsTileAt(Vector2 roomPos)
    {
        int x = (int)roomPos.X / 8;
        int y = (int)roomPos.Y / 8;

        return FG.SafeTileAt(x, y) != '0';
    }

    public bool IsSolidAt(Vector2 roomPos)
    {
        if (IsTileAt(roomPos))
            return true;

        foreach (var e in Entities[typeof(ISolid)])
        {
            Rectangle bRect = e.Rectangle;

            if (bRect.Contains(roomPos))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsInBounds(Vector2 roomPos) 
        => new Rectangle(0, 0, Width, Height).Contains(roomPos);
}
