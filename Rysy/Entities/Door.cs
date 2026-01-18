using Rysy.Gui.FieldTypes;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("door")]
public class Door : SpriteBankEntity, IPlaceable {
    public override string SpriteBankEntry => Attr("type", "wood") switch {
        "wood" => "door",
        var other => $"{other}door"
    };

    public override string Animation => "idle";

    public override int Depth => 8998;

    public static FieldList GetFields() => new(new {
        type = GetTypeField()
    });

    public static PlacementList GetPlacements() => [
        new Placement("wood"),
        new Placement("metal", new {
            type = "metal"
        })
    ];

    public static PathField GetTypeField() =>
        Fields.SpriteBankPath("wood", "(.*)door$", previewAnimation: "idle")
        .WithConverter((found) => found.Captured switch {
            "" => "wood", // the wood doors don't follow the naming convention...
            var other => other,
        })
        .WithFilter((found) => found.Path is not "ghost_door" && !found.Path.Contains("trapdoor", StringComparison.OrdinalIgnoreCase));
}
