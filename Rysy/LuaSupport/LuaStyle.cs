using Rysy.Stylegrounds;

namespace Rysy.LuaSupport;

internal sealed class LuaStyle : Style {
    internal LonnStylePlugin Plugin { get; set; }
    
    public override List<string>? AssociatedMods
        => Plugin?.GetAssociatedMods?.Invoke(this) ?? base.AssociatedMods;

    public override bool CanBeInBackground => Plugin?.GetCanBackground?.Invoke(this) ?? base.CanBeInBackground;
    public override bool CanBeInForeground => Plugin?.GetCanForeground?.Invoke(this) ?? base.CanBeInForeground;

    public override void Unpack(BinaryPacker.Element from) {
        base.Unpack(from);

        if (EntityRegistry.GetInfo(from.Name ?? "", RegisteredEntityType.Style) is { } info) {
            Plugin = info.LonnStylePlugin!;
        }
    }
}
