#pragma warning disable CS0649
//#define FAILED_CACHE

using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("spinner")]
public sealed class Spinner : Entity, IPlaceable {
    private const int SpinnerDepth = -8500;
    private const int ConnectorDepth = -8500 + 1;
    private const int OutlineDepth = SpinnerDepth + 2;
    
    public override int Depth => -8500;

    private static readonly ColoredSpriteTemplate DustSprite =
        SpriteTemplate.FromTexture("danger/dustcreature/base00", SpinnerDepth).Centered().CreateColoredTemplate(Color.White);
    
    private static readonly ColoredSpriteTemplate DustOutlineSprite =
        SpriteTemplate.FromTexture("Rysy:dust_creature_outlines/base00", -48).Centered().CreateColoredTemplate(Color.Red);
    
    private static readonly ColoredSpriteTemplate[] FgSprites = [
        SpriteTemplate.FromTexture("danger/crystal/fg_blue00", SpinnerDepth).Centered().CreateColoredTemplate(Color.White),
        SpriteTemplate.FromTexture("danger/crystal/fg_red00", SpinnerDepth).Centered().CreateColoredTemplate(Color.White),
        SpriteTemplate.FromTexture("danger/crystal/fg_purple00", SpinnerDepth).Centered().CreateColoredTemplate(Color.White),
        SpriteTemplate.FromTexture("Rysy:coreSpinnerFg", SpinnerDepth).Centered().CreateColoredTemplate(Color.White),
        SpriteTemplate.FromTexture("danger/crystal/fg_white00", SpinnerDepth).Centered().CreateColoredTemplate(Color.White),
    ];

    private static readonly ColoredSpriteTemplate[] FgBorderSprites = FgSprites
        .Select(s => s.Template
            .WithDepth(OutlineDepth)
            .WithOutlineTexture()
            .CreateColoredTemplate(Color.Black))
        .ToArray();

    private static readonly ColoredSpriteTemplate[] BgSprites = {
        SpriteTemplate.FromTexture("danger/crystal/bg_blue00", ConnectorDepth).Centered().CreateColoredTemplate(Color.White),
        SpriteTemplate.FromTexture("danger/crystal/bg_red00", ConnectorDepth).Centered().CreateColoredTemplate(Color.White),
        SpriteTemplate.FromTexture("danger/crystal/bg_purple00", ConnectorDepth).Centered().CreateColoredTemplate(Color.White),
        SpriteTemplate.FromTexture("Rysy:coreSpinnerBg", ConnectorDepth).Centered().CreateColoredTemplate(Color.White),
        SpriteTemplate.FromTexture("danger/crystal/bg_white00", ConnectorDepth).Centered().CreateColoredTemplate(Color.White),
    };

    private static readonly ColoredSpriteTemplate[] BgBorderSprites = BgSprites
        .Select(s => s.Template
            .WithDepth(OutlineDepth)
            .WithOutlineTexture()
            .CreateColoredTemplate(Color.Black))
        .ToArray();

    [Bind("attachToSolid")]
    public bool AttachToSolid;

    [Bind("dust")]
    public bool Dust;

    [Bind("color")]
    private SpinnerColors _spinnerColor;

    public override void OnChanged(EntityDataChangeCtx changed) {
        base.OnChanged(changed);

        ClearCache();
    }

    private void ClearCache(bool loop = true) {
#if FAILED_CACHE
        _cachedSprites = null;
        if (_cachedConnectedSpinners is { } connected) {
            _cachedConnectedSpinners = null;
            foreach (var r in connected) {
                if (r.TryGetTarget(out var spinner)) {
                    //spinner._cachedConnectedSpinners = null;
                    spinner._cachedSprites = null;
                }
            }

            if (loop)
                foreach (Spinner s in Room.Entities[typeof(Spinner)]) {
                    if ((s._cachedSprites is {} || s._cachedConnectedSpinners is {}) && IsValidConnection(s)) {
                        s.ClearCache(false);
                    }
                }
        }
#endif
    }

    public override void ClearInnerCaches() {
        base.ClearInnerCaches();
        ClearCache();
    }

#if FAILED_CACHE
    private List<ISprite>? _cachedSprites;
    private List<WeakReference<Spinner>>? _cachedConnectedSpinners;
    private bool IsValidConnection(Spinner spinner) {
        return DistanceSquaredLessThan(Pos, spinner.Pos, 24 * 24)
               && !spinner.Dust && spinner.AttachToSolid == AttachToSolid && spinner.Room is {};
    }
#endif

    public override IEnumerable<ISprite> GetSprites() {
#if FAILED_CACHE
        if (_cachedSprites is { }) {
            /*
            if (_cachedConnectedSpinners is {})
                foreach (var s in _cachedConnectedSpinners) {
                    if (!s.TryGetTarget(out var spinner)) {
                        ClearCache();
                        break;
                    }

                    if (!IsValidConnection(spinner)) {
                        ClearCache();
                        break;
                    }
                }*/
            
            if (_cachedSprites is {})
                return _cachedSprites;
        }
#endif

        var sprites = GetSpritesUncached();
#if FAILED_CACHE
        sprites = _cachedSprites = sprites.ToList();
#endif
        return sprites;
    }

    private int _lastSpriteCount = 2;
    private IEnumerable<ISprite> GetSpritesUncached() {
        if (Dust) {
            return [
                DustOutlineSprite.Create(Pos),
                DustSprite.Create(Pos)
            ];
        }

        var color = _spinnerColor;
        var rainbow = color == SpinnerColors.Rainbow;
        var pos = Pos;

        var sprites = new List<ISprite>(_lastSpriteCount);

        if (rainbow) {
            sprites.Add(FgSprites[(int) color].CreateRainbow(pos));
        } else {
            sprites.Add(FgSprites[(int) color].Create(pos));
        }
        
        // the border has to be a separate sprite to render it at a different depth
        sprites.Add(FgBorderSprites[(int) color].Create(pos));

        // connectors
#if FAILED_CACHE
        _cachedConnectedSpinners = new();
        bool createSprites = true;
#endif
        foreach (Spinner spinner in Room.Entities[typeof(Spinner)]) {
            if (spinner == this) {
#if FAILED_CACHE
                createSprites = false;
#else
                break;
#endif
            }

            if (DistanceSquaredLessThan(pos, spinner.Pos, 24 * 24)
                && !spinner.Dust && spinner.AttachToSolid == AttachToSolid) {
                #if FAILED_CACHE
                _cachedConnectedSpinners.Add(new(spinner));
                //if (spinner._cachedSprites is not { })
                //    continue;
                //if (!createSprites)
                //    continue;
                #endif
                var connectorPos = ((pos + spinner.Pos) / 2f).Floored();

                if (rainbow) {
                    sprites.Add(BgSprites[(int) color].CreateRainbow(connectorPos));
                } else {
                    sprites.Add(BgSprites[(int) color].Create(connectorPos));
                }

                // the border has to be a separate sprite to render it at a different depth
                sprites.Add(BgBorderSprites[(int) color].Create(connectorPos));
            }
        }

        _lastSpriteCount = sprites.Count;
        return sprites;
    }

    public static bool DistanceSquaredLessThan(Vector2 value1, Vector2 value2, float maxDist) {
        float xDiff = value1.X - value2.X;
        float xDiffSq = xDiff * xDiff;
        if (xDiffSq > maxDist)
            return false;

        float yDiff = value1.Y - value2.Y;
        return xDiffSq + yDiff * yDiff < maxDist;
    }

    public static FieldList GetFields() => new(new {
        color = SpinnerColors.Blue,
        attachToSolid = false,
        dust = false,
    });

    public static PlacementList GetPlacements() => IterationHelper.EachName<SpinnerColors>()
        .Select(n => new Placement(n.ToLower(), new {
            color = n
        }))
        .Append(new Placement("dust", new {
            dust = true,
        }))
        .ToPlacementList();

    private enum SpinnerColors {
        Blue, Red, Purple, Core, Rainbow
    }

    public override bool CanTrim(string key, object val) {
        return (key, val) switch {
            ("attachToSolid", false) => true,
            ("dust", false) => true,
            _ => base.CanTrim(key, val)
        };
    }
}
