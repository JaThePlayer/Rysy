using Rysy.Helpers;
using Rysy.Mods;
using System.Diagnostics;
using System.Reflection;

namespace Rysy.Tools;

public class ToolRegistry {
    public static readonly ToolRegistry Global = CreateScanned();

    public ListenableList<Type> Tools { get; private set; } = new();

    private static ToolRegistry CreateScanned() {
        var registry = new ToolRegistry();

        ModRegistry.RegisterModAssemblyScanner((mod, old) => ScanAssembly(mod, old, registry));

        return registry;
    }

    private static void ScanAssembly(ModMeta mod, Assembly? oldAsm, ToolRegistry registry) {
        if (oldAsm is { }) {
            registry.Tools.RemoveAll(t => t.Assembly == oldAsm);
        }

        if (mod.PluginAssembly is not { } asm) {
            return;
        }

        registry.Tools.AddAll(asm.GetTypes().Where(t => t.IsSubclassOf(typeof(Tool)) && !t.IsAbstract));
    }
}
