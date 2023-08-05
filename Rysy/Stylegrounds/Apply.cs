namespace Rysy.Stylegrounds;

[CustomEntity("apply")]
public sealed class Apply : StyleFolder, IPlaceable {
    public override string DisplayName => Data.Attr("_editorName", base.DisplayName);

    public override bool CanBeNested => false;

    public static FieldList GetFields() => new() {
        ["_editorName"] = Fields.String(null!).AllowNull().ConvertEmptyToNull()
    };

    public static PlacementList GetPlacements() => new();
}
