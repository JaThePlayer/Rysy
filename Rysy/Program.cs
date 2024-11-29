using Microsoft.CodeAnalysis;
using Rysy;
using Rysy.Helpers;

RysyState.CmdArguments = new(args);

Environment.CurrentDirectory = Path.GetDirectoryName(typeof(RysyEngine).Assembly.Location) ?? Environment.CurrentDirectory;

if (RysyState.CmdArguments.Headless) {
    await ReplUtils.LoadHeadless(cSharpPlugins: true, luaPlugins: true);

    if (RysyState.CmdArguments.HeadlessScriptFile is { } file) {
        var contents = await File.ReadAllTextAsync(file);
        CodeCompilationHelper.CompileFiles(Path.GetFileName(file), [(contents, file)], null, addGlobalUsings: true, out var asm, out var emitResult,
            OutputKind.ConsoleApplication);
    
        if (emitResult is { Success: false }) {
            Logger.Write("Run Code Script", LogLevel.Error, $"Failed to compile cmd script:\n{emitResult.Diagnostics.FormatDiagnostics()}");
            return;
        }

        if (asm is { }) {
            try {
                asm.EntryPoint?.Invoke(null, [ Array.Empty<string>() ]);
            } catch (Exception ex) {
                Logger.Write("CommandlineScript", LogLevel.Error, $"Failed to run cmd script:\n{ex}");
            }
        }
    }
    
    return;
}

using var engine = new RysyEngine();
engine.Run();
