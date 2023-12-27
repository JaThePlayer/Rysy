using Rysy.Helpers;
using Rysy.Mods;
using System.Reflection;

namespace Rysy.MapAnalyzers;

public class MapAnalyzerRegistry {
    public static readonly MapAnalyzerRegistry Global = CreateScanned();

    public ListenableList<Type> AnalyzerTypes { get; private set; } = new();

    public AnalyzerCtx Analyze(Map map) {
        using var watch = new ScopedStopwatch("Map Analyzers");
        
        AnalyzerCtx ctx = new(map);

        foreach (var type in AnalyzerTypes) {
            var analizer = (MapAnalyzer) Activator.CreateInstance(type)!;

            analizer.Analyze(ctx);
        }

        return ctx;
    }

    private static MapAnalyzerRegistry CreateScanned() {
        var registry = new MapAnalyzerRegistry();

        ModRegistry.RegisterModAssemblyScanner((mod, old) => ScanAssembly(mod, old, registry));

        return registry;
    }

    private static void ScanAssembly(ModMeta mod, Assembly? oldAsm, MapAnalyzerRegistry registry) {
        if (oldAsm is { }) {
            registry.AnalyzerTypes.RemoveAll(t => t.Assembly == oldAsm);
        }

        if (mod.PluginAssembly is not { } asm) {
            return;
        }

        registry.AnalyzerTypes.AddAll(asm.GetTypes().Where(t => t.IsSubclassOf(typeof(MapAnalyzer)) && !t.IsAbstract));
    }
}
