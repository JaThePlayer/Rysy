using System.Text.Json.Serialization;

namespace Rysy.LuaSupport;

public class LonnTrigger : Trigger {
    [JsonIgnore]
    public LonnEntityPlugin Plugin;
}
