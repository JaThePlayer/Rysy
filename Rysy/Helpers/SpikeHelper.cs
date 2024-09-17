using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Gui.FieldTypes;
using Rysy.Selections;

namespace Rysy.Helpers;

public static class SpikeHelper {
    public enum Direction {
        Up, 
        Right,
        Down, 
        Left, 
    }

    public static Color[] DefaultDustColors { get; } = new[] {
        Color.Lerp("f25a10".FromRGB(), Color.DarkSlateBlue, 0.4f),
        Color.Lerp("ff0000".FromRGB(), Color.DarkSlateBlue, 0.4f),
        Color.Lerp("f21067".FromRGB(), Color.DarkSlateBlue, 0.4f),
    };

    public static IEnumerable<ISprite> GetDustSprites(Entity e, Direction dir, IReadOnlyList<Color>? colors = null) {
        colors ??= DefaultDustColors;

        var size = dir switch {
            Direction.Up or Direction.Down => e.Width,
            _ => e.Height,
        };

        var origin = new Vector2(0, 1);
        var offset = dir switch {
            Direction.Up => new Vector2(0, 0),
            Direction.Down => new(4f, 0f),
            Direction.Right => new(0, 0),
            Direction.Left => new(0f, 4f),
            _ => default
        };
        var secondOffset = dir switch {
            Direction.Up => new Vector2(1, 0),
            Direction.Down => new(-1, 0f),
            Direction.Right => new(0, 1),
            Direction.Left => new(0f, -1),
            _ => default
        };

        offset += e.Pos;

        var textures = GFX.Atlas.GetSubtextures(dir is Direction.Up or Direction.Down ? "danger/triggertentacle/wiggle_v" : "danger/triggertentacle/wiggle_v");

        var rot = dir switch {
            Direction.Up => 0f,
            Direction.Down => MathF.PI,
            Direction.Right => MathF.PI / 2f,
            Direction.Left => 1.5f * MathF.PI,
            _ => 0f,
        };

        for (int i = 0; i < size; i += 4) {
            var seed = new Vector2(i * 123198);

            var second = i % 8 == 4;
            var color = seed.SeededRandomFrom(colors);
            var off = second ? i : i;
            var texture = seed.SeededRandomFrom(textures);//second ? t2 : t1;

            var sprite = GetSprite(off, texture, rot, default);
            yield return sprite with {
                Color = Color.Black,
                Pos = sprite.Pos + secondOffset
            };
            yield return sprite with {
                Color = color,
            };
        }

        Sprite GetSprite(float pos, VirtTexture path, float rot, Vector2 off) {
            return ISprite.FromTexture(path) with {
                Pos = offset + off + dir switch {
                    Direction.Up or Direction.Down => new Vector2(pos, 0),
                    Direction.Left => new(0, pos),
                    _ => new(0, pos)
                },
                Origin = origin,
                Rotation = rot,
            };
        }
    }

    public static IEnumerable<ISprite> GetSprites(Entity e, Direction dir, string type, bool triggerSpikes = false) {
        if (triggerSpikes) {
            // Create a preview above the normal sprites
            foreach (var previewSprite in GetSprites(e, dir, type, triggerSpikes: false)) {
                yield return previewSprite.WithMultipliedAlpha(0.3f);
            }
        }

        var tentacle = type == "tentacles";
        var size = dir switch {
            Direction.Up or Direction.Down => e.Width,
            _ => e.Height,
        };

        var (origin, offset) = (tentacle, dir) switch {
            (true, Direction.Up) => (new Vector2(0.0f, 0.5f), new Vector2()),
            (true, Direction.Down) => (new(1.0f, 0.5f), new()),
            (true, Direction.Right) => (new(0f, 0.5f), new()),
            (true, Direction.Left) => (new(1.0f, 0.5f), new()),

            (false, Direction.Up) => (new(0.5f, 1f), new(4f, 1f)),
            (false, Direction.Down) => (new(0.5f, 0f), new(4f, -1f)),
            (false, Direction.Right) => (new(0f, 0.5f), new(-1f, 4f)),
            (false, Direction.Left) => (new(1f, 0.5f), new(1f, 4f)),
            (_, _) => (default, default)
        };

        if (triggerSpikes) {
            offset = dir switch {
                Direction.Down => new Vector2(4, -5),
                Direction.Left => new Vector2(5, 4),
                Direction.Right => new Vector2(-5, 4),
                _ => new Vector2(4, 5),
            };
        }

        offset += e.Pos;

        if (tentacle) {
            var rot = dir switch {
                Direction.Up => 0f,
                Direction.Down => MathF.PI,
                Direction.Right => MathF.PI / 2f,
                Direction.Left => 1.5f * MathF.PI,
                _ => 0f,
            };

            for (int i = 0; i < size - 8; i += 16) {
                yield return GetSprite(i, "danger/tentacles00", rot);
            }
            if (size / 8 % 2 == 1) {
                yield return GetSprite(size - 16, "danger/tentacles00", rot);
            }

            yield break;
        }

        var tex = $"danger/spikes/{type}_{dir.ToString().ToLowerInvariant()}00";

        for (int i = 0; i < size; i += 8) {
            yield return GetSprite(i, tex);
        }


        Sprite GetSprite(float pos, string path, float rot = 0f) {
            return ISprite.FromTexture(path) with {
                Pos = offset + dir switch {
                    Direction.Up or Direction.Down => new Vector2(pos, 0),
                    _ => new(0, pos)
                },
                Origin = origin,
                Rotation = rot,
            };
        }
    }

    public static ISelectionCollider GetSelection(Entity entity, Direction dir) {
        return dir switch {
            Direction.Up => ISelectionCollider.FromRect(entity.X, entity.Y - 8, entity.Width, 8),
            Direction.Down => ISelectionCollider.FromRect(entity.Rectangle),
            Direction.Left => ISelectionCollider.FromRect(entity.X - 8, entity.Y, 8, entity.Height),
            Direction.Right => ISelectionCollider.FromRect(entity.Rectangle),
            _ => ISelectionCollider.FromRect(entity.Rectangle),
        };
    }

    public static PlacementList CreatePlacements(Func<string, string> typeToPlacementName) => new[] {
        "default",
        "outline",
        "cliffside",
        "reflection"
    }.Select(t => new Placement(typeToPlacementName(t), new {
        type = t
    }))
    .ToPlacementList();

    public static PathField GetTypeField() => Fields.AtlasPath("default", "^danger/spikes/(.*)_up00$");
}
