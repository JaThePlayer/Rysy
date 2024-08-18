using Rysy.Helpers;

namespace Rysy.Gui.FieldTypes.LonnGlue;

/// <summary>
/// Only exists for spikes.lua to provide a dynamic dropdown.
/// </summary>
internal sealed record SpikeTextureFieldGlue : ILonnField {
    public static string Name => "__rysy_spikeTexture";

    public static Field Create(object? def, IUntypedData fieldInfoEntry) {
        return SpikeHelper.GetTypeField();
    }
}
