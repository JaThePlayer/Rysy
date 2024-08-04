using Rysy.Helpers;
using System.Text.Json.Serialization;

namespace Rysy.LuaSupport;

public sealed class LonnTrigger : Trigger, IHasLonnPlugin {
    private ListenableDictionaryRef<string, RegisteredEntity> _lonnPluginRef;
    
    ListenableDictionaryRef<string, RegisteredEntity> IHasLonnPlugin.LonnPluginRef { 
        get => _lonnPluginRef;
        set => _lonnPluginRef = value;
    }

    [JsonIgnore] 
    public LonnEntityPlugin? Plugin => _lonnPluginRef.TryGetValue(out var plugin, out var changed) 
        ? plugin.LonnPlugin
        : null;
    
    public override List<string>? AssociatedMods
        => Plugin?.GetAssociatedMods?.Invoke(this) ?? base.AssociatedMods;

    public override Range NodeLimits => Plugin?.GetNodeLimits(Room, this) ?? base.NodeLimits;

    public override Point MinimumSize => Plugin?.GetMinimumSize?.Invoke(Room, this) ?? base.MinimumSize;
}
