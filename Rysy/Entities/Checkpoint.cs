using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("checkpoint")]
internal sealed class Checkpoint : Entity, IPlaceable {
    public override int Depth => 9990;

    public int CheckpointId => Int("checkpointID", -1);

    public override IEnumerable<ISprite> GetSprites() {
        var bg = Attr("bg", "");
        var texture = string.IsNullOrWhiteSpace(bg) ? "objects/checkpoint/flash03" : $"objects/checkpoint/bg/{bg}";

        yield return ISprite.FromTexture(Pos, texture) with {
            Origin = new(0.5f, 1f),
        };
    }

    public static FieldList GetFields() => new(new {
        bg = Fields.AtlasPath("", @"^objects/checkpoint/bg/(.*)$").AllowNull(),
        checkpointID = Fields.Int(-1).WithValidator((ctx, valueObj) => {
            var id = valueObj.CoerceToInt(-1);
            if (id == -1 || ctx.EditorState?.Map is not { } map)
                return ValidationResult.Ok;

            var checkpointsWithThisId = map.Rooms.SelectMany(r => r.Entities.OfType<Checkpoint>())
                .Count(c => c.CheckpointId == id);
            
            return checkpointsWithThisId > 1 ? ValidationResult.CheckpointIdNotUnique : ValidationResult.Ok;
        }),
        inventory = Fields.EnumNamesWithAddedOptionsDropdown<CelesteEnums.Inventories>("", NullableBoolField.MapDefaultLangKey.Translate()).AllowNull().ConvertEmptyToNull(),
        coreMode = Fields.EnumNamesWithAddedOptionsDropdown<CelesteEnums.CoreModes>("", NullableBoolField.MapDefaultLangKey.Translate()).AllowNull().ConvertEmptyToNull(),
        dreaming = Fields.BoolNullable(null).WithNullName(NullableBoolField.MapDefaultLangKey),
        allowOrigin = Fields.Bool(true).MakeHidden() // Legacy backwards compat field, shouldn't be edited anymore.
    });

    public static PlacementList GetPlacements() => [];
}
