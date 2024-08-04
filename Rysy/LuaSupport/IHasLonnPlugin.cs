using Rysy.Helpers;

namespace Rysy.LuaSupport;

public interface IHasLonnPlugin {
    ListenableDictionaryRef<string, RegisteredEntity> LonnPluginRef { get; internal set; }
}

