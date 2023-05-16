using Rysy.Extensions;
using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("gondola")]
public sealed class Gondola : Entity, IPlaceable {
    public override int Depth => 0;

    public override Range NodeLimits => 1..1;

    public override IEnumerable<ISprite> GetSprites() {
        foreach (var gondolaSprite in GetGondolaSprites(Pos, Bool("active"))) {
            yield return gondolaSprite;
        }

        yield return ISprite.FromTexture(Pos.AddX(-124), "objects/gondola/cliffsideLeft") with {
            Origin = new(0f, 1f),
            Depth = 8998,
        };
    }

    public override IEnumerable<ISprite> GetNodeSprites(int nodeIndex) {
        var nodePos = Nodes![0].Pos;

        foreach (var gondolaSprite in GetGondolaSprites(nodePos, !Bool("active"))) {
            yield return gondolaSprite;
        }

        yield return ISprite.FromTexture(nodePos.Add(144, -104), "objects/gondola/cliffsideRight") with {
            Origin = new(0f, 0.5f),
            Depth = 8998,
            Scale = new(-1, 1)
        };
    }

    private IEnumerable<ISprite> GetGondolaSprites(Vector2 pos, bool hasLever) {
        const float offsetY = -64f;
        var posOffset = pos + new Vector2(0, offsetY);
        Color color = hasLever ? Color.White : (Color.White * 0.3f);


        yield return ISprite.FromTexture(posOffset, "objects/gondola/front") with {
            Origin = new(0.5f, 0f),
            Color = color,
        };

        yield return ISprite.FromTexture(posOffset, "objects/gondola/back") with {
            Origin = new(0.5f, 0f),
            Color = color,
            Depth = 9000,
        };

        yield return ISprite.FromTexture(posOffset, "objects/gondola/top") with {
            Origin = new(0.5f, 0f),
            Color = color,
        };

        if (hasLever) {
            yield return ISprite.FromTexture(posOffset, "objects/gondola/lever01") with {
                Origin = new(0.5f, 0f),
                Color = color,
            };
        }
    }

    public static FieldList GetFields() => new(new {
        active = true
    });

    public static PlacementList GetPlacements() => new("gondola");
}