using Microsoft.Xna.Framework.Graphics;
using Rysy.Graphics;
using Rysy.Helpers;
using System.Diagnostics;

namespace Rysy;

public sealed class Room : IPackable
{
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
                        Entities.Add(EntityRegistry.Create(entity, this));
                    }

                    break;
                case "triggers":
                    Logger.Write("Room.Unpack", LogLevel.Error, "todo: triggers");

                    break;
                case "objtiles":
                    Logger.Write("Room.Unpack", LogLevel.Error, "todo: objtiles");
                    break;
                case "solids":
                    FG = Tilegrid.FromString(Width, Height, child.Attr("innerText"));
                    FG.Depth = Depths.FGTerrain;
                    FG.Autotiler = Map.FGAutotiler ?? throw new Exception("Map.FGAutotiler must not be null!");
                    break;
                case "bg":
                    BG = Tilegrid.FromString(Width, Height, child.Attr("innerText"));
                    BG.Depth = Depths.BGTerrain;
                    BG.Autotiler = Map.BGAutotiler ?? throw new Exception("Map.BGAutotiler must not be null!");
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

    public void Render(Camera camera)
    {
        if (!camera.IsRectVisible(Bounds))
        {
            CachedSprites = null;
            return;
        }

        GFX.Batch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, effect: null, camera.Matrix * (Matrix.CreateTranslation(X * camera.Scale, Y * camera.Scale, 0f)));
        ISprite.Rect(new(0, 0, Width, Height), Color.Gray * .3f).Render();

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
            .Concat(FG.GetSprites())
            .Concat(BG.GetSprites())
            .OrderByDescending(x => x.Depth)
            .ToList();

            StartTextureLoadAwaiter();
            //CachedSprites.Sort(ISprite.DepthDescendingComparer);
        }

        foreach (var item in CachedSprites)
        {
            item.Render();
        }

        GFX.Batch.End();
    }

    private void StartTextureLoadAwaiter()
    {
        Task.Run(async () =>
        {
            using (var w = new ScopedStopwatch($"Loading textures for {Name}"))
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

    public bool IsTileAt(Vector2 pos)
    {
        int x = (int)pos.X / 8;
        int y = (int)pos.Y / 8;

        return FG.SafeTileAt(x, y) != '0';
    }

    public bool IsSolidAt(Vector2 pos)
    {
        if (IsTileAt(pos))
            return true;

        foreach (var e in Entities[typeof(ISolid)])
        {
            Rectangle bRect = EntityHelper.GetEntityRectangle(e);

            if (bRect.Contains(pos))
            {
                return true;
            }
        }

        return false;
    }
}
