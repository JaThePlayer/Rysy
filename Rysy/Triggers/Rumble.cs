using Rysy.Extensions;
using Rysy.Graphics;

namespace Rysy.Triggers;

[CustomEntity("rumbleTrigger")]
public sealed class Rumble : Trigger, IPlaceable {
    public override Range NodeLimits => 2..2;

    public static FieldList GetFields() => new(new {
        manualTrigger = false,
        persistent = false,
        constrainHeight = false
    });

    public static PlacementList GetPlacements() => new("rumble");
    
    public override IEnumerable<ISprite> GetNodePathSprites() {
        foreach (var s in NodePathTypes.Fan(this)) {
            yield return s;
        }
        
        if (Nodes is not [var a, var b]) {
            yield break;
        }

        Rectangle rect = Bool("constrainHeight")
            ? RectangleExt.FromPoints(a, b)
            : RectangleExt.FromPoints(new Vector2(a.X, 0), new(b.X, Room.Height));

        // +1,+1 to adjust for trigger nodes being centered the way they are.
        yield return ISprite.OutlinedRect(rect.AddSize(1, 1), NodePathTypes.Color * 0.2f, NodePathTypes.Color, 1);
    }
}