using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("cliffflag")]
public class CliffFlags : Entity, IPlaceable {
    public override int Depth => 8999;

    public override Range NodeLimits => 1..1;

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("cliffflag");

    public override IEnumerable<ISprite> GetSprites() {
        return FlaglineHelper.GetSprites(Pos, Nodes![0].Pos, FlaglineOptions);
    }

    public override IEnumerable<ISprite> GetAllNodeSprites() => Array.Empty<ISprite>();

    public override ISelectionCollider GetMainSelection()
        => ISelectionCollider.FromRect(Pos.Add(-2, -2), 4, 4);

    public override ISelectionCollider GetNodeSelection(int nodeIndex)
        => ISelectionCollider.FromRect(Nodes[nodeIndex].Pos.Add(-2, -2), 4, 4);

    private static FlaglineHelper.Options FlaglineOptions = new FlaglineHelper.Options() {
        LineColor = Color.Lerp(Color.Gray, Color.DarkBlue, 0.25f),
        PinColor = Color.Gray,
        Colors = new()
        {
            "d85f2f".FromRGB(),
            "d82f63".FromRGB(),
            "2fd8a2".FromRGB(),
            "d8d62f".FromRGB(),
        },
        FlagHeight = 10..10,
        FlagLength = 10..10,
        Space = 2..8,
    }.CreateHighlightColors(c => Color.Lerp(c, Color.White, 0.3f));
}
