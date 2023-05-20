using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("tentacles")]
public sealed class Tentacles : SpriteEntity, IPlaceable {
    public override int Depth => 0;

    public override Range NodeLimits => 1..;

    public override string TexturePath => "@Internal@/tentacles";

    public static FieldList GetFields() => new(new {
        fear_distance = FearDistances.None,
        slide_until = 0
    });

    public static PlacementList GetPlacements() => new("tentacles");

    public enum FearDistances {
        None, Close, Distant, Far
    }
}