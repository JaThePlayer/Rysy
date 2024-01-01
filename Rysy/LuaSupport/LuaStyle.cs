using Rysy.Stylegrounds;

namespace Rysy.LuaSupport;

internal sealed class LuaStyle : Style {
    internal LonnStylePlugin Plugin { get; set; }
    
    public override List<string>? AssociatedMods
        => Plugin?.GetAssociatedMods?.Invoke(this) ?? base.AssociatedMods;

    public override void Unpack(BinaryPacker.Element from) {
        base.Unpack(from);
        
        if (EntityRegistry.SIDToLonnStylePlugin.TryGetValue(from.Name ?? "", out var pl)) {
            Plugin = pl;
        }
    }
}
