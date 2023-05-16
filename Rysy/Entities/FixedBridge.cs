using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("bridgeFixed")]
public sealed class FixedBridge : LoopingSpriteEntity, IPlaceable {
    public override int Depth => 0;

    public override bool ResizableX => true;

    public override Vector2 Offset => new(0, -8);

    public override Vector2 Origin => new();

    public override string TexturePath => "scenery/bridge_fixed";

    public static FieldList GetFields() => new();

    public static PlacementList GetPlacements() => new("bridge_fixed");
}
