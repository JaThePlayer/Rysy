namespace Rysy.Triggers; 

[CustomEntity("everest/musicLayerTrigger")]
public sealed class MusicLayer : Trigger, IPlaceable {
    public override string Category => TriggerCategories.Audio;

    public static FieldList GetFields() => new(new {
        layers = "",
        enable = false
    });

    public static PlacementList GetPlacements() => new("music_layer");
}