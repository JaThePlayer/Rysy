using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("soundSource")]
public sealed class SoundSource : SpriteEntity, IPlaceable {
    public override int Depth => 0;

    public override string TexturePath => "@Internal@/sound_source";

    public static FieldList GetFields() => new(new {
        sound = Fields.Dropdown("event:/env/local/02_old_site/phone_lamp", CelesteEnums.EnvironmentalSounds, editable: true),
    });

    public static PlacementList GetPlacements() => new("sound_source");
}