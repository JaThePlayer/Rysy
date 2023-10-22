using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using System;
using System.Runtime.CompilerServices;
using YamlDotNet.Core;

namespace Rysy.Entities;

[CustomEntity("spinner")]
public sealed class Spinner : Entity, IPlaceable {
    public override int Depth => -8500;

    private static readonly Sprite[] FgSprites = new[] {
        ISprite.FromTexture("danger/crystal/fg_blue00").Centered(),
        ISprite.FromTexture("danger/crystal/fg_red00").Centered(),
        ISprite.FromTexture("danger/crystal/fg_purple00").Centered(),
        ISprite.FromTexture("danger/crystal/fg_red00").Centered(),
        ISprite.FromTexture("danger/crystal/fg_white00").Centered(),
    };

    private static readonly Sprite[] FgBorderSprites = FgSprites.Select(s => s with {
        Color = Color.Transparent,
        OutlineColor = Color.Black,
        Depth = -8500 + 2,
    }).ToArray();

    private static readonly Sprite[] BgSprites = new[] {
        ISprite.FromTexture("danger/crystal/bg_blue00").Centered() with { Depth = -8500 + 1, },
        ISprite.FromTexture("danger/crystal/bg_red00").Centered() with { Depth = -8500 + 1, },
        ISprite.FromTexture("danger/crystal/bg_purple00").Centered() with { Depth = -8500 + 1, },
        ISprite.FromTexture("danger/crystal/bg_red00").Centered() with { Depth = -8500 + 1, },
        ISprite.FromTexture("danger/crystal/bg_white00").Centered() with { Depth = -8500 + 1, },
    };

    private static readonly Sprite[] BgBorderSprites = BgSprites.Select(s => s with {
        Color = Color.Transparent,
        OutlineColor = Color.Black,
        Depth = -8500 + 2,
    }).ToArray();

    [Bind("attachToSolid")]
    public bool AttachToSolid;

    [Bind("dust")]
    public bool Dust;

    [Bind("color")]
    private SpinnerColors SpinnerColor;

    public override void OnChanged(EntityDataChangeCtx changed) {
        base.OnChanged(changed);

        ClearCache();
    }

    private void ClearCache() {
        _cachedSprites = null;
        if (_cachedConnectedSpinners is { } connected) {
            _cachedConnectedSpinners = null;
            foreach (var r in connected) {
                if (r.TryGetTarget(out var spinner)) {
                    spinner._cachedConnectedSpinners = null;
                    spinner._cachedSprites = null;
                }
            }
        }
    }

    private List<ISprite>? _cachedSprites;
    private List<WeakReference<Spinner>>? _cachedConnectedSpinners;

    public override IEnumerable<ISprite> GetSprites() {
        if (_cachedSprites is { } cached) {
            return cached;
        }

        var sprites = GetSpritesUncached().ToList();
        _cachedSprites = sprites;
        return sprites;
    }

    private IEnumerable<ISprite> GetSpritesUncached() {
        if (Dust) {
            yield return ISprite.FromTexture(Pos, "Rysy:dust_creature_outlines/base00").Centered() with {
                Color = Color.Red,
                Depth = -48,
            };
            yield return ISprite.FromTexture(Pos, "danger/dustcreature/base00").Centered();
            yield break;
        }

        var color = SpinnerColor;
        var rainbow = color == SpinnerColors.Rainbow;
        var pos = Pos;

        yield return FgSprites[(int) color] with {
            Pos = pos,
            Color = rainbow ? ColorHelper.GetRainbowColor(Room, pos) : Color.White,
        };
        // the border has to be a seperate sprite to render it at a different depth
        yield return FgBorderSprites[(int) color] with {
            Pos = pos,
        };

        // connectors
        _cachedConnectedSpinners = new();
        bool createSprites = true;
        foreach (Spinner spinner in Room.Entities[typeof(Spinner)]) {
            if (spinner == this)
                createSprites = false;
                //break;

            if (DistanceSquaredLessThan(pos, spinner.Pos, 24 * 24)
                && !spinner.Dust && spinner.AttachToSolid == AttachToSolid) {
                _cachedConnectedSpinners.Add(new(spinner));
                if (spinner._cachedSprites is not { })
                    continue;
                var connectorPos = (pos + spinner.Pos) / 2f;

                yield return BgSprites[(int) color] with {
                    Pos = connectorPos,
                    Color = rainbow ? ColorHelper.GetRainbowColor(Room, connectorPos) : Color.White,
                };

                // the border has to be a seperate sprite to render it at a different depth
                yield return BgBorderSprites[(int) color] with {
                    Pos = connectorPos,
                };
            }
        }
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

    public static PlacementList GetPlacements() => IterationHelper.EachNameToLower<SpinnerColors>()
        .Select(n => new Placement(n, new {
            color = n
        }))
        .ToPlacementList();

    private enum SpinnerColors {
        Blue, Red, Purple, Core, Rainbow
    }
}
