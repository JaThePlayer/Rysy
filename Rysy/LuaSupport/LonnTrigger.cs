using System.Text.Json.Serialization;

namespace Rysy.LuaSupport;

public sealed class LonnTrigger : Trigger {
    [JsonIgnore]
    public LonnEntityPlugin Plugin;

    [JsonIgnore]
    public override int Depth => Plugin.GetDepth(Room, this);

    public override Range NodeLimits => Plugin.GetNodeLimits(Room, this);

    public override Point MinimumSize => Plugin.GetMinimumSize?.Invoke(Room, this) ?? base.MinimumSize;
}
