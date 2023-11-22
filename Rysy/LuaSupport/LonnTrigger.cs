using Rysy.Helpers;
using System.Text.Json.Serialization;

namespace Rysy.LuaSupport;

public sealed class LonnTrigger : Trigger {
    internal ListenableDictionaryRef<string, LonnEntityPlugin> PluginRef;

    [JsonIgnore] 
    public LonnEntityPlugin? Plugin => PluginRef.TryGetValue(out var plugin, out var changed) 
        ? plugin 
        : null;

    public override Range NodeLimits => Plugin?.GetNodeLimits(Room, this) ?? base.NodeLimits;

    public override Point MinimumSize => Plugin?.GetMinimumSize?.Invoke(Room, this) ?? base.MinimumSize;
}
