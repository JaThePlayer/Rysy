#pragma warning disable CS0649

using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.LuaSupport;
using Rysy.Selections;

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
            sprites.Add(cache.Fg.CreateRainbow(pos));
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
                    sprites.Add(cache.Bg.CreateRainbow(connectorPos));
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
    
    public override bool CanTrim(string key, object val) => key switch {
        "destroyColor" => val.ToString() == "639bff", // the C# side has a different default than lonn for this 1 thing...
        _ => IsDefault(key, val),
    };
}

[CustomEntity("FrostHelper/RainbowTilesetController", associatedMods: [ "FrostHelper" ])]
internal sealed class RainbowTilesetController : LonnEntity, IPlaceable {
    [Bind("tilesets")]
    public ReadOnlyArray<char> Tilesets;

    [Bind("bg")]
    public bool Bg;

    public TileLayer TileLayer => Bg ? TileLayer.BG : TileLayer.FG;
    
    public static FieldList GetFields() => new(new {
        tilesets = Fields.List("3", Fields.TileDropdown('3', ctx => ctx.GetValue("bg") is true)),
        bg = false,
    });

    public static PlacementList GetPlacements() => [];
}

[CustomEntity("FrostHelper/ArbitraryShapeCloud", [ "FrostHelper" ])]
internal sealed class ArbitraryShapeCloud : Entity
{
    public override int Depth => Int("depth");
    
    public override IEnumerable<ISprite> GetSprites()
    {
        var nodes = Nodes.Select(n => n.Pos).Append(Pos).ToList();
        var color = RGBA("color");
        var fillColor = color;

        var textures = ParseTextureList(Attr("textures", DefaultTextureString));

        var start = Pos;

        foreach (var n in nodes)
        {
            var angle = VectorExt.Angle(start, n);
            var angleVec = angle.AngleToVector(1f);

            var curr = start;
            var dist = Vector2.Distance(start, n);
            while (dist > 0) {
                var t = (curr + Room.Pos).SeededRandomFrom(textures);
                var spr = GFX.Atlas[t.Path];
                var sprW = spr.Width * 3 / 4;
                var offset = Math.Min(0, dist - sprW);
                var rot = angle + t.DefaultRotation;

                yield return ISprite.FromTexture((curr + angleVec * offset).Floored() + Vector2.UnitY.Rotate(rot) * 2f, spr) with {
                    Origin = new(0f, 1f),
                    Rotation = rot,
                    Color = fillColor,
                };

                curr += angleVec * sprW;
                dist -= sprW;
            }

            start = n;
        }
        
        Vector2[] linePoints = [start, .. nodes];

        yield return new PolygonSprite(linePoints, Enum("windingOrder", WindingOrders.Auto) switch {
            WindingOrders.Clockwise => Triangulator.WindingOrder.Clockwise,
            WindingOrders.CounterClockwise => Triangulator.WindingOrder.CounterClockwise,
            _ => null,
        }) {
            Color = fillColor,
        };

        foreach (var n in linePoints) {
            yield return ISprite.OutlinedRect(n.Add(-2, -2), 4, 4, Color.Transparent, Color.Orange);
        }
    }

    public override IEnumerable<ISprite> GetNodeSprites(int nodeIndex) => NodePathTypes.None;

    public override IEnumerable<ISprite> GetNodePathSprites() => NodePathTypes.None;

    public override ISelectionCollider GetMainSelection()
        => ISelectionCollider.FromRect(Pos.Add(-2, -2), 4, 4);

    public override ISelectionCollider GetNodeSelection(int nodeIndex)
        => ISelectionCollider.FromRect(Nodes[nodeIndex].Pos.Add(-2, -2), 4, 4);

    public override Range NodeLimits => 2..;

    public enum WindingOrders {
        Clockwise, CounterClockwise, Auto
    }

    private static List<CloudTexture> ParseTextureList(string list) {
        if (CloudTextureCache.TryGetValue(list, out var cached))
            return cached;

        var split = list.Trim().Split(',');

        var t = new List<CloudTexture>(split.Length);

        foreach (var texDef in split) {
            t.Add(new(texDef, 0f));
        }

        CloudTextureCache[list] = t;

        return t;
    }

    private const string DefaultTextureString = @"decals/10-farewell/clouds/cloud_c,decals/10-farewell/clouds/cloud_cc,decals/10-farewell/clouds/cloud_cd,decals/10-farewell/clouds/cloud_ce";

    private record CloudTexture(string Path, float DefaultRotation);

    private static readonly Dictionary<string, List<CloudTexture>> CloudTextureCache = new(StringComparer.Ordinal);
}
