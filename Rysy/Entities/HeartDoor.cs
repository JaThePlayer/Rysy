using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("heartGemDoor")]
public sealed class HeartDoor : Entity, IPlaceable {
    private static Color WallColor = "18668f".FromRGB();

    public override int Depth => 0;

    public override Range NodeLimits => 0..1;

    public override bool ResizableX => true;

    public override IEnumerable<ISprite> GetSprites() {
        var (x, y) = Pos;
        var w = Width;
        var rh = Room.Height;

        var edgeTexture = GFX.Atlas["objects/heartdoor/edge"]!;
        var (edgeWidth, edgeHeight) = (edgeTexture.Width, edgeTexture.Height);

        yield return ISprite.Rect(new((int)x, 0, w, rh), WallColor);

        for (int ey = 0; ey < rh; ey += edgeHeight) {
            yield return ISprite.FromTexture(new(x + edgeWidth - 4, ey), edgeTexture) with {
                Origin = new(0.5f, 0f),
                Scale = new(-1, 1),
            };

            yield return ISprite.FromTexture(new(x + w - edgeWidth + 4, ey), edgeTexture) with {
                Origin = new(0.5f, 0f),
            };
        }

        int hearts = Int("requires");
        if (hearts <= 0)
            yield break;

        var heartTexture = GFX.Atlas["objects/heartdoor/icon00"]!;
        var heartWidth = heartTexture.Width + 4;

        int maxHeartsPerRow = (w - 8) / heartWidth;
        int rows = (int) MathF.Ceiling(hearts / (float)maxHeartsPerRow);

        int heartsLeft = hearts;
        for (int row = 0; row < rows; row++) {
            int heartsThisRow = Math.Min(heartsLeft, maxHeartsPerRow);
            var yOffset = (-rows / 2f + row + 0.5f) * heartWidth;

            for (int i = 1; i <= heartsThisRow; i++) {
                yield return ISprite.FromTexture(new(x + (w / (float)(heartsThisRow + 1) * i), y + yOffset), heartTexture).Centered();
            }

            heartsLeft -= heartsThisRow;
        }
    }

    public override ISelectionCollider GetMainSelection() {
        return ISelectionCollider.FromRect(X, 0, Width, Room.Height);
    }

    public static FieldList GetFields() => new(new {
        requires = 0,
        startHidden = false
    });

    public static PlacementList GetPlacements() => new("door");
}