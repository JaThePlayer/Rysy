using Rysy.Extensions;
using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("bridge")]
public class Bridge : Entity, IPlaceable {
    public override int Depth => 0;

    public override Range NodeLimits => 2..2;

    public override bool ResizableX => true;

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("bridge");

    public override IEnumerable<ISprite> GetSprites() {
        if (Nodes is not [{ X: var gapStartX }, { X: var gapEndX }])
            yield break;

        var (x, y, w) = (X, Y, Width);
        var currentX = x;
        var i = 0;

        while (currentX < x + w) {
            var pos = new Vector2(currentX, y - 8);

            var tileSize = (i is >= 2 and <= 7) ? TileSizes[2 + pos.SeededRandomInclusive(0, 6)] : TileSizes[i];

            if (currentX < gapStartX || currentX >= gapEndX) {
                yield return ISprite.FromTexture(pos, "scenery/bridge").CreateSubtexture(tileSize);
            }

            currentX += tileSize.Width;
            i = (i + 1) % TileSizes.Count;
        }
    }

    public override IEnumerable<ISprite> GetNodeSprites(int nodeIndex) {
        yield return ISprite.Rect(new(Nodes[nodeIndex].X, Y - 8), 1, 16, Color.Red);
    }

    public override IEnumerable<ISprite> GetNodePathSprites() {
        yield break;
    }

    public override ISelectionCollider GetNodeSelection(int nodeIndex) {
        return ISelectionCollider.FromRect(new(Nodes[nodeIndex].X - 2, Y - 8), 5, 16);
    }

    private static List<Rectangle> TileSizes = new() {
        new(0, 0, 16, 55),
        new(16, 0, 8, 55),
        new(24, 0, 8, 55),
        new(32, 0, 8, 55),
        new(40, 0, 8, 55),
        new(48, 0, 8, 55),
        new(56, 0, 8, 55),
        new(64, 0, 8, 55),
        new(72, 0, 8, 55),
        new(80, 0, 16, 55),
        new(96, 0, 8, 55)
    };
}
