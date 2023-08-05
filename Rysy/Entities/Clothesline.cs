using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.Selections;

namespace Rysy.Entities;

[CustomEntity("clothesline")]
internal class Clothesline : Entity, IPlaceable {
    public override int Depth => 8999;

    public override Range NodeLimits => 1..1;

    public override IEnumerable<ISprite> GetSprites() {
        return FlaglineHelper.GetSprites(Pos, Nodes![0].Pos, FlaglineOptions);
    }

    public override IEnumerable<ISprite> GetAllNodeSprites() => Array.Empty<ISprite>();

    public override ISelectionCollider GetMainSelection()
        => ISelectionCollider.FromRect(Pos.Add(-2, -2), 4, 4);

    public override ISelectionCollider GetNodeSelection(int nodeIndex)
        => ISelectionCollider.FromRect(Nodes[nodeIndex].Pos.Add(-2, -2), 4, 4);

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("clothesline");

    private static FlaglineHelper.Options FlaglineOptions = new FlaglineHelper.Options() {
        LineColor = Color.Lerp(Color.Gray, Color.DarkBlue, 0.25f),
        PinColor = Color.Gray,
        Colors = new()
        {
            "0d2e6b".FromRGB(),
            "3d2688".FromRGB(),
            "4f6e9d".FromRGB(),
            "47194a".FromRGB()
        },
        FlagHeight = 8..20,
        FlagLength = 8..16,
        Space = 2..8,
    }.CreateHighlightColors(c => Color.Lerp(c, Color.White, 0.3f));
}
