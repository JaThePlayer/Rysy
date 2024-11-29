#if SourceCodePlugins

using KeraLua;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Rysy.Mods;
using System.Collections.Immutable;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Rysy.Helpers;

public static class CodeCompilationHelper {
    private const string GlobalUsingsCode = """
        global using Rysy.Extensions;
        global using Rysy.Graphics;
        global using Rysy.LuaSupport;
        global using Rysy.Helpers;
        global using Rysy.Scripting;
        global using Rysy.History;
        global using Rysy.Entities;
        global using Rysy.Mods;
        global using Rysy.Scenes;
        global using Rysy.Selections;
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
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Lua).Assembly.Location),
        };

        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var asm in assemblies!) {
            if (!string.IsNullOrWhiteSpace(asm.Location))
                references.Add(MetadataReference.CreateFromFile(asm.Location));

            //Console.WriteLine(asm.FullName);
        }

        _CachedReferences = references;
        
        return references;
    }

    private static List<MetadataReference>? _CachedReferences;

    private static byte[]? RysyAssemblyHash;

    private static byte[] GetAsmHash(Assembly asm) {
        var loc = asm.Location;
        using var asmStream = File.OpenRead(loc);

#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms - not used for crypto
        return MD5.HashData(asmStream);
#pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms
    }

    public static bool CompileFiles(string asmName, List<(string SourceCode, string Filename)> files, string? cachePath, 
        bool addGlobalUsings, out Assembly? asm, out EmitResult? emitResult, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary) {
        asm = null;
        emitResult = null;

        if (files.Count == 0)
            return false;

        string? cachedDLLPath = null, cachedPdbPath = null, cachedSrcPath = null, currentSource = null;
        var cacheFs = SettingsHelper.GetFilesystem(perProfile: true);

        if (cachePath is { }) {
            cachedDLLPath = $"{cachePath}/c.dll";
            cachedPdbPath = $"{cachePath}/c.pdb";
            cachedSrcPath = $"{cachePath}/src.txt";
            currentSource = (files, RysyAssemblyHash ??= GetAsmHash(typeof(CodeCompilationHelper).Assembly)).ToJson(minified: true);
            
            if (cacheFs.FileExists(cachedDLLPath) && cacheFs.FileExists(cachedSrcPath)) {
                var cachedSource = cacheFs.TryReadAllText(cachedSrcPath);
                
                if (currentSource == cachedSource) {
                    var pdb = cacheFs.TryReadAllBytes(cachedPdbPath);
                    var rawAsm = cacheFs.TryReadAllBytes(cachedDLLPath)!;

                    asm = Assembly.Load(rawAsm, pdb);

                    return true;
                }
                
                cacheFs.TryDeleteFile(cachedDLLPath);
                cacheFs.TryDeleteFile(cachedSrcPath);
            }
        }


        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp11);

        IEnumerable<(string SourceCode, string Filename)> allfiles = files;
        if (addGlobalUsings && !files.Any(f => f.Filename.EndsWith("GlobalUsings.cs", StringComparison.Ordinal))) {
            allfiles = allfiles.Append((SourceCode: GlobalUsingsCode, Filename: "GlobalUsings.gen.cs"));
        }

        var trees = allfiles.Select(f => SyntaxFactory.ParseSyntaxTree(SourceText.From(f.SourceCode, Encoding.UTF8), options, path: f.Filename));

        var csCompilation = CSharpCompilation.Create(asmName,
            trees,
            references: GetReferences(),
            options: new CSharpCompilationOptions(outputKind,
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
                cacheFs.TryWriteToFile(cachedSrcPath!, currentSource ?? "");
                cacheFs.TryWriteToFile(cachedDLLPath!, asmBytes);
                cacheFs.TryWriteToFile(cachedPdbPath!, pdbBytes);
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
            str.AppendLine(CultureInfo.InvariantCulture, $"[{diagnostic.Location.GetLineSpan()}] {diagnostic.Id}: {diagnostic.GetMessage(CultureInfo.InvariantCulture)}");
        }

        return str.ToString();
    }
}

#endif
