using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Rysy.Extensions;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Rysy.Helpers;

public static class CodeCompilationHelper {
    private static readonly string GlobalUsingsCode = """
        global using Rysy.Extensions;
        global using Rysy.Graphics;
        global using Rysy.LuaSupport;
        global using Rysy.Helpers;
        global using Rysy.Scripting;
        global using Rysy.History;
        global using Rysy.Entities;
        global using Rysy.Mods;
        global using Rysy.Scenes;
        global using Rysy;

        global using System;
        global using System.Linq;
        global using System.Collections.Generic;

        global using Microsoft.Xna.Framework;
        global using Microsoft.Xna.Framework.Graphics;
        global using Color = Microsoft.Xna.Framework.Color;
        global using Rectangle = Microsoft.Xna.Framework.Rectangle;
        global using Vector2 = Microsoft.Xna.Framework.Vector2;

        global using NumVector2 = System.Numerics.Vector2;
        global using NumVector3 = System.Numerics.Vector3;
        global using NumVector4 = System.Numerics.Vector4;

        global using XnaVector2 = Microsoft.Xna.Framework.Vector2;
        """;

    private static List<MetadataReference> GetReferences() {
        if (_CachedReferences is { } cached)
            return cached;

        var references = new List<MetadataReference> {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
        };

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var asm in assemblies!) {
            if (!string.IsNullOrWhiteSpace(asm.Location))
                references.Add(MetadataReference.CreateFromFile(asm.Location));
        }

        _CachedReferences = references;

        return references;
    }

    private static List<MetadataReference>? _CachedReferences;

    public static bool CompileFiles(string asmName, List<(string SourceCode, string Filename)> files, string cachePath, out Assembly? asm, out EmitResult? emitResult) {
        asm = null;
        emitResult = null;

        if (files.Count == 0)
            return false;

        var cachedDLLPath = $"{cachePath}/c.dll";
        var cachedPdbPath = $"{cachePath}/c.pdb";
        var cachedSrcPath = $"{cachePath}/src.txt";
        var currentSource = files.ToJson(minified: true);

        if (File.Exists(cachedDLLPath) && File.Exists(cachedSrcPath)) {
            var cachedSource = File.ReadAllText(cachedSrcPath);

            if (currentSource == cachedSource) {
                var pdb = File.Exists(cachedPdbPath) ? File.ReadAllBytes(cachedPdbPath) : null;

                asm = Assembly.Load(File.ReadAllBytes(cachedDLLPath), pdb);

                return true;
            }
            //Console.WriteLine("invalidating cache");

            File.Delete(cachedDLLPath);
            File.Delete(cachedSrcPath);
        }

        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp11);

        var trees = files
            .Append((SourceCode: GlobalUsingsCode, Filename: "GlobalUsings.gen.cs"))
            .Select(f => SyntaxFactory.ParseSyntaxTree(SourceText.From(f.SourceCode, Encoding.UTF8), options, path: f.Filename));

        var csCompilation = CSharpCompilation.Create(asmName,
            trees,
            references: GetReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release,
                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));

        using var peStream = new MemoryStream();
        using var pdbStream = new MemoryStream();

        emitResult = csCompilation.Emit(peStream, pdbStream);

        if (emitResult.Success) {
            var asmBytes = peStream.ToArray();
            var pdbBytes = pdbStream.ToArray();

            asm = Assembly.Load(asmBytes, pdbBytes);

            if (cachePath is { }) {
                Directory.CreateDirectory(cachePath);
                File.WriteAllText(cachedSrcPath, currentSource);
                File.WriteAllBytes(cachedDLLPath, asmBytes);
                File.WriteAllBytes(cachedPdbPath, pdbBytes);
            }

            return true;
        }

        return true;
    }

    public static string FormatDiagnostics(this ImmutableArray<Diagnostic> diagnostics) {
        if (diagnostics == null) {
            return "";
        }

        var str = new StringBuilder();

        var failures = diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);
        foreach (var diagnostic in failures) {
            str.AppendLine($"[{diagnostic.Location.GetLineSpan()}] {diagnostic.Id}: {diagnostic.GetMessage(CultureInfo.InvariantCulture)}");
        }

        return str.ToString();
    }
}
