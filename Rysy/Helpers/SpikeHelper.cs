using Rysy.Graphics;

namespace Rysy.Helpers;

public static class SpikeHelper
{
    public enum Direction
    {
        Up, Down, Left, Right
    }

    public static IEnumerable<ISprite> GetSprites(Entity e, Direction dir, string type)
    {
        var tentacle = type == "tentacles";
        var size = dir switch
        {
            Direction.Up or Direction.Down => e.Width,
            _ => e.Height,
        };

        var (origin, offset) = (tentacle, dir) switch
        {
            (true, Direction.Up) => (new Vector2(0.0f, 0.5f), new Vector2()),
            (false, Direction.Up) => (new(0.5f, 1f), new(4f, 1f)),
            (true, Direction.Down) => (new(1.0f, 0.5f), new()),
            (false, Direction.Down) => (new(0.5f, 0f), new(4f, -1f)),
            (true, Direction.Right) => (new(0f, 0.5f), new()),
            (false, Direction.Right) => (new(0f, 0.5f), new(-1f, 4f)),
            (true, Direction.Left) => (new(1.0f, 0.5f), new()),
            (false, Direction.Left) => (new(1f, 0.5f), new(1f, 4f)),
            (_, _) => (default, default)
        };
        offset += e.Pos;

        if (tentacle)
        {
            var rot = dir switch
            {
                Direction.Up => 0f,
                Direction.Down => MathF.PI,
                Direction.Right => MathF.PI / 2f,
                Direction.Left => 1.5f * MathF.PI,
                _ => 0f,
            };

            for (int i = 0; i < size - 8; i += 16)
            {
                yield return GetSprite(i, "danger/tentacles00", rot);
            }
            if (size / 8 % 2 == 1)
            {
                yield return GetSprite(size - 16, "danger/tentacles00", rot);
            }

            yield break;
        }

        var tex = $"danger/spikes/{type}_{dir.ToString().ToLowerInvariant()}00";

        for (int i = 0; i < size; i += 8)
        {
            yield return GetSprite(i, tex);
        }


        Sprite GetSprite(float pos, string path, float rot = 0f)
        {
            return ISprite.FromTexture(path) with
            {
                Pos = offset + dir switch
                {
                    Direction.Up or Direction.Down => new Vector2(pos, 0),
                    _ => new(0, pos)
                },
                Origin = origin,
                Rotation = rot,
            };
        }
    }
}
