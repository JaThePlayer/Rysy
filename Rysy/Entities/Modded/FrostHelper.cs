#pragma warning disable CS0649

using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.LuaSupport;

namespace Rysy.Entities.Modded;

[CustomEntity("FrostHelper/IceSpinner", associatedMods: [ "FrostHelper" ])]
internal sealed class CustomSpinner : LonnEntity {
    [Bind("attachToSolid")]
    public bool AttachToSolid;

    [Bind("attachGroup")]
    public int AttachGroup;

    [Bind("rainbow")]
    public bool Rainbow;

    [Bind("drawOutline")]
    public bool DrawOutline;

    [Bind("tint")]
    public Color Color;

    public Color BorderColor => RGBA("borderColor", Color.Black);

    private SpinnerPathCache _cache;
    
    public override string? Documentation => "https://github.com/JaThePlayer/FrostHelper/wiki/Custom-Spinners";

    public override void OnChanged(EntityDataChangeCtx changed) {
        base.OnChanged(changed);

        if (!changed.OnlyPositionChanged)
            _cache = GetBaseSprites(Depth, Color, BorderColor);
    }

    record SpinnerPathCache(ColoredSpriteTemplate Fg, ColoredSpriteTemplate FgBack, ColoredSpriteTemplate FgOutline, 
        ColoredSpriteTemplate Bg, ColoredSpriteTemplate BgBack, ColoredSpriteTemplate BgOutline);

    private static Dictionary<(string directory, string spritePathSuffix, Color color, Color borderColor), SpinnerPathCache> SpriteCache = new();
    private SpinnerPathCache GetBaseSprites(int depth, Color color, Color borderColor) {
        var directory = Attr("directory", "danger/FrostHelper/icecrystal");
        var suffix = Attr("spritePathSuffix", "");

        if (SpriteCache.TryGetValue((directory, suffix, color, borderColor), out var cached)) {
            return cached;
        }

        var fg = SpriteTemplate.FromTexture($"{directory}/fg{suffix}", depth).Centered();
        var bg = SpriteTemplate.FromTexture($"{directory}/bg{suffix}", depth).Centered() with { Depth = depth + 1, };
        var cache = new SpinnerPathCache(
            fg.CreateColoredTemplate(color),
            fg.WithDepth(depth + 2).CreateColoredTemplate(default, borderColor),
            fg.WithOutlineTexture().WithDepth(depth + 2).CreateColoredTemplate(borderColor),
            bg.CreateColoredTemplate(color),
            bg.WithDepth(depth + 2).CreateColoredTemplate(default, borderColor),
            bg.WithOutlineTexture().WithDepth(depth + 2).CreateColoredTemplate(borderColor)
        );
        
        SpriteCache[(directory, suffix, color, borderColor)] = cache;

        return cache;
    }

    private int _lastSpriteCount = 2;
    
    public override IEnumerable<ISprite> GetSprites() {
        var attachToSolid = AttachToSolid;
        var attachGroup = AttachGroup;
        var pos = Pos;
        var rainbow = Rainbow;
        var drawOutline = DrawOutline;
        var useOutlineTexture = drawOutline && BorderColor == Color.Black;

        var cache = _cache;
        var sprites = new List<ISprite>(capacity: _lastSpriteCount);

        if (rainbow) {
            sprites.Add(cache.Fg.Template.Create(pos, ColorHelper.GetRainbowColor(Room, pos)));
        } else {
            sprites.Add(cache.Fg.Create(pos));
        }

        // the border has to be a separate sprite to render it at a different depth
        if (useOutlineTexture) {
            // use an outline texture to optimise rendering
            sprites.Add(cache.FgOutline.Create(pos));
        }
        else if (drawOutline) {
            sprites.Add(cache.FgBack.Create(pos));
        }

        foreach (CustomSpinner spinner in Room.Entities[typeof(CustomSpinner)]) {
            if (spinner == this)
                break;

            var otherPos = spinner.Pos;

            if (Spinner.DistanceSquaredLessThan(pos, otherPos, 24f * 24f) && spinner.AttachToSolid == attachToSolid && spinner.AttachGroup == attachGroup) {
                var connectorPos = (pos + otherPos) / 2f;

                if (rainbow) {
                    sprites.Add(cache.Bg.Template.Create(connectorPos, ColorHelper.GetRainbowColor(Room, connectorPos)));
                } else {
                    sprites.Add(cache.Bg.Create(connectorPos));
                }
                
                if (useOutlineTexture) {
                    // use an outline texture to optimise rendering
                    sprites.Add(cache.BgOutline.Create(connectorPos));
                }
                else if (drawOutline) {
                    sprites.Add(cache.BgBack.Create(connectorPos));
                }
            }
        }

        _lastSpriteCount = sprites.Count;
        return sprites;
    }
}

[CustomEntity("FrostHelper/RainbowTilesetController", associatedMods: [ "FrostHelper" ])]
internal sealed class RainbowTilesetController : LonnEntity, IPlaceable {
    [Bind("tilesets")]
    public IReadOnlyList<char> Tilesets;

    [Bind("bg")]
    public bool Bg;

    public TileLayer TileLayer => Bg ? TileLayer.BG : TileLayer.FG;
    
    public static FieldList GetFields() => new(new {
        tilesets = Fields.List("3", Fields.TileDropdown('3', ctx => ctx.GetValue("bg") is true)),
        bg = false,
    });

    public static PlacementList GetPlacements() => [];
}
