using Hexa.NET.ImGui;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using System.Reflection;

namespace Rysy.Helpers.Repl;

public sealed class CSharpRepl : IRepl {
    private readonly IRysyLogger _logger;

    private static IEnumerable<Assembly> DefaultScriptReferences => [
        typeof(CSharpRepl).Assembly,
        typeof(ImGui).Assembly,
        typeof(Lua).Assembly
    ];

    private static IEnumerable<string> DefaultImports => [
        "System",
        "System.Linq",
        "System.Collections",
        "System.Collections.Generic",
        "System.Text",
        "System.Threading",
        "System.Reflection",
        "System.Globalization",
        "Rysy",
        "Rysy.Components",
        "Rysy.Extensions",
        "Rysy.Helpers",
        "Rysy.Shared",
        "Rysy.Mods",
        "Microsoft.Xna.Framework",
        "Microsoft.Xna.Framework.Graphics",
    ];
    
    public Script Script { get; }
    
    private ScriptState ScriptState { get; set; }
    
    public const string DefaultBootCode = """
    
    """;
    
    public CSharpRepl(string bootCode, IRysyLogger logger) {
        _logger = logger;
        var options = ScriptOptions.Default
            .AddReferences(DefaultScriptReferences)
            .AddImports(DefaultImports);
        
        var loader = new InteractiveAssemblyLoader();

        /*
        foreach (var modPluginAsm in ModRegistry.Mods.Values.SelectWhereNotNull(x => x.PluginAssembly)) {
            loader.RegisterDependency(modPluginAsm);
            options.AddReferences(MetadataReference.CreateFromStream(File.OpenRead(cachedAsmPath)));
        }
        */
        
        Script = CSharpScript.Create(bootCode, options, null, loader);
        Reset();
    }

    void Reset() {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
        try {
            ScriptState = Script.RunAsync(null, cts.Token).Result;
        } catch (Exception ex) {
            _logger.Error(ex, "Failed to reset C# Repl");
        }
    }

    public async Task<object?> ContinueWith(string newCode) {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));

        var newState = await ScriptState.ContinueWithAsync(newCode, options: null, cts.Token);

        ScriptState = newState;

        return newState.ReturnValue;
    }
}