using Rysy.Shared.Networking;

namespace Celeste.Mod.Rysy.InteropMod;

internal sealed class ModLifetimeOutPipeServer<T>(OutPipeServer<T> server) : IModLifetimeScoped {
    public void Load() {
        server.Load();
    }

    public void Unload() {
        server.Dispose();
    }
}