using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using JetBrains.Annotations;
using Rysy.Mods.RelinkSteps;

namespace Rysy.Mods;

internal sealed class RelinkCtx {
    public ModMeta Mod { get; }
    
    public string Path { get; }
    
    public bool Success { get; internal set; }

    public bool RelinkingNecessary { get; internal set; } = true;

    public AssemblyDefinition AssemblyDefinition {
        get => field ?? throw new InvalidOperationException("Can't access AssemblyDefinition when Success is false!");
        private init;
    }
    
    public Version? RysyVersion { get; internal set; }
    
    public Logger Logger { get; }
    
    private MemoryStream? OriginalAssemblyStream { get; }
    
    internal RelinkCtx(ModMeta mod, string path, AssemblyDefinition? assemblyDefinition,
                       MemoryStream? originalAssemblyStream) {
        Mod = mod;
        Path = path;
        Success = assemblyDefinition != null;
        AssemblyDefinition = assemblyDefinition!;
        OriginalAssemblyStream = originalAssemblyStream;
        Logger = new Logger($"Relinker({mod.Name})");
    }
    
    [MustDisposeResource]
    public Stream GetRelinkedStream() {
        if (!RelinkingNecessary)
            return OriginalAssemblyStream ?? throw new InvalidOperationException("Can't invoke GetRelinkedStream when Success is false!");
        
        var memStream = new MemoryStream();
        AssemblyDefinition.WriteManifest(memStream);
        memStream.Seek(0, SeekOrigin.Begin);
        
        return memStream;
    }
}

internal static class Relinker {
    public static RelinkCtx Relink(ModMeta mod, string path) {
        using var watch = new ScopedStopwatch($"Relinking mod module: {path}");
        
        var memStream = new MemoryStream();

        var asmDef = mod.Filesystem.OpenFile(path, fsStream => {
            // Zip streams don't support seeking which is used by FromStream,
            // so we need to copy to a memory stream first.
            fsStream.CopyTo(memStream);
            memStream.Seek(0, SeekOrigin.Begin);

            return AssemblyDefinition.FromStream(memStream);
        });

        memStream.Seek(0, SeekOrigin.Begin);
        var ctx = new RelinkCtx(mod, path, asmDef, memStream);

        if (asmDef is not null) {
            Relink(ctx, asmDef);
        }

        return ctx;
    }

    private static void Relink(RelinkCtx ctx, AssemblyDefinition asmDef) {
        asmDef.RuntimeContext!.AddAssembly(AssemblyDefinition.FromFile(Path.Combine(Environment.CurrentDirectory, "Rysy.dll"), createRuntimeContext: false));

        var manifest = asmDef.ManifestModule;
        if (manifest is null) {
            ctx.Logger.Info("Assembly does not have a Manifest Module, skipping relinking...");
            return;
        }
        
        var rysyVersion = GetTargetRysyVersion(asmDef);
        if (rysyVersion is null) {
            ctx.Logger.Info("Assembly does not reference Rysy, skipping relinking...");
            return;
        }
        ctx.Logger.Info($"Target Rysy Version: {rysyVersion}");
        ctx.RysyVersion = rysyVersion;

        List<IRelinkStep> steps = [];
        if (rysyVersion < new Version(0, 0, 14, 0)) {
            steps.Add(new Version0_0_13_0());
        }

        if (steps.Count == 0) {
            ctx.RelinkingNecessary = false;
            ctx.Logger.Info("Relinking not necessary.");
            return;
        }

        foreach (var step in steps) {
            HandleRelinkStep(ctx, step);
        }
    }

    private static void HandleRelinkStep(RelinkCtx ctx, IRelinkStep step) {
        var manifest = ctx.AssemblyDefinition.ManifestModule!;
        
        step.Begin(ctx);

        if (step is IRelinkStepVisitCilMethods visitCilMethods) {
            foreach (var t in manifest.GetAllTypes()) {
                foreach (var m in t.Methods) {
                    if (m is { MethodBody: CilMethodBody c }) {
                        visitCilMethods.Visit(ctx, c);
                    }
                }
            }
        }
    }

    private static Version? GetTargetRysyVersion(AssemblyDefinition asmDef) {
        var re = asmDef.ManifestModule?.AssemblyReferences
            .FirstOrDefault(r => r.Name?.AsSpan().SequenceEqual("Rysy"u8) ?? false);
        var ret = re?.Version;
        
        re?.Version = RysyEngine.Version;
        return ret;
    }
}