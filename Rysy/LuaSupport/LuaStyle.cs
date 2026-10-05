using Rysy.Helpers;
using Rysy.Stylegrounds;
using System.Text.Json.Serialization;

namespace Rysy.LuaSupport;

internal class LuaStyle : Style, IHasLonnPlugin {
    private ListenableDictionaryRef<string, RegisteredEntity> _lonnPluginRef;
    
    ListenableDictionaryRef<string, RegisteredEntity> IHasLonnPlugin.LonnPluginRef { 
        get => _lonnPluginRef;
        set => _lonnPluginRef = value;
    }
    
    [JsonIgnore]
    internal LonnStylePlugin? Plugin {
        get {
            var exists = _lonnPluginRef.TryGetValue(out var info, out var changed);

            if (changed) {
                OnChanged(new EntityDataChangeCtx { AllChanged = true });
            }
            
            return exists ? info!.LonnStylePlugin : null;
        }
    }
    
    public override IReadOnlyList<string>? AssociatedMods
        => Plugin?.GetAssociatedMods?.Invoke(this) ?? base.AssociatedMods;

    public override bool CanBeInBackground => Plugin?.GetCanBackground?.Invoke(this) ?? base.CanBeInBackground;
    public override bool CanBeInForeground => Plugin?.GetCanForeground?.Invoke(this) ?? base.CanBeInForeground;

    public override void Unpack(BinaryPacker.Element from) {
        base.Unpack(from);

        _lonnPluginRef = EntityRegistry.RegisteredStyles.GetReference(from.Name ?? "");
    }
}
