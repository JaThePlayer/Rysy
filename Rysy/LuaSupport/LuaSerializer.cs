using KeraLua;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.Layers;
using System.CodeDom.Compiler;
using System.Text;
using System.Text.RegularExpressions;

namespace Rysy.LuaSupport;

public static partial class LuaSerializer {
    private static Lua GetSandboxedLua() => Lua.CreateNew(openLibs: false);

    public static string CorrectDecalPathForLonn(string rysyPath) {
        // lonn fails to access the decal sprite for animated decals if the texture path does not end with 00...
        if (Gfx.Atlas.TryGet($"decals/{rysyPath}00", out _))
            return $"{rysyPath}00";

        return rysyPath;
    }

    private static void WriteSerializedToLua(IndentedTextWriter writer, BinaryPacker.Element element) {
        writer.WriteLine("{");
        writer.Indent++;

        foreach (var (k, v) in element.Attributes) {
            writer.WriteLine($"{TableKeyString(k)} = {ToLuaString(v)},");
        }
        
        if (element.Children is { Length: > 0 } nodes) {
            writer.WriteLine("nodes = {");
            writer.Indent++;
            foreach (var n in nodes) {
                var x = n.Int("x");
                var y = n.Int("y");
                
                writer.WriteLine($"{{ x={x}, y={y} }},");
            }

            writer.Indent--;
            writer.WriteLine("},");
        }
        
        writer.Indent--;
        writer.WriteLine("},");
    }

    public static string? ConvertSelectionsToLonnString(IReadOnlyList<IEditorLayer> layers, List<CopypasteHelper.CopiedSelection> copied) {
        if (copied is not { Count: > 0 })
            return null;
        if (copied.Any(x => x.ResolveLayer(layers) is not ILonnSerializableLayer))
            return null;

        var writer = new StringWriter();
        var indentedWriter = new IndentedTextWriter(writer);

        indentedWriter.WriteLine("{");
        indentedWriter.Indent++;
        foreach (var item in copied) {
            var layer = item.ResolveLayer(layers);

            if (layer is ILonnSerializableLayer serializableLayer)
                WriteSerializedToLua(indentedWriter, serializableLayer.ConvertToLonnFormat(item));
        }

        indentedWriter.Indent--;
        indentedWriter.Write('}');
        return writer.ToString();
    }

    private static string TableKeyString(string key) {
        if (IsValidKey(key))
            return key;

        return $"""
            ["{ToLuaString(key)}"]
            """;
    }

    private static bool IsValidKey(string key) {
        if (LuaKeywords.Contains(key))
            return false;

        return VariableNameRegex().IsMatch(key);
    }

    private static readonly HashSet<string> LuaKeywords = new() {
        "and", "break", "do", "else", "elseif", "end", "false", "for",
        "function", "goto", "if", "in", "local", "nil", "not", "or",
        "repeat", "return", "then", "true", "until", "while"
    };

    public static string ToLuaString(object obj) => obj switch {
        string s => $"""
        "{SanitizeString(s)}"
        """,
        char c => $"\"{SanitizeString(c.ToString())}\"",
        int i => i.ToString(CultureInfo.InvariantCulture),
        long i => i.ToString(CultureInfo.InvariantCulture),
        float f => f.ToString(CultureInfo.InvariantCulture),
        double f => f.ToString(CultureInfo.InvariantCulture),
        bool b => b ? "true" : "false",
        _ => obj.ToString()!,
    };

    private static readonly char[] EscapableChars = new char[] { '\a', '\b', '\f', '\n', '\r', '\t', '\v', '\\', '"', '\'' };
    private static readonly Dictionary<char, string> EscapeSequences = new() {
        ['\a'] = @"\a",
        ['\b'] = @"\b",
        ['\f'] = @"\f",
        ['\n'] = @"\n",
        ['\r'] = @"\r",
        ['\t'] = @"\t",
        ['\v'] = @"\v",
        ['\0'] = @"\0",
    };

    private static string SanitizeString(string s) {
        StringBuilder builder = new(s.Length);
        var span = s.AsSpan();
        int i;
        var escapable = EscapableChars;
        var sequences = EscapeSequences;

        while ((i = span.IndexOfAny(escapable.AsSpan())) > -1) {
            builder.Append(span[..i]);
            if (sequences.TryGetValue(span[i], out var escape)) {
                builder.Append(escape);
            } else {
                builder.Append('\\');
                builder.Append(span[i]);
            }

            span = span[(i+1)..];
        }
        builder.Append(span);

        return builder.ToString();
    }

    /// <summary>
    /// Tries to convert lonn-copied placements into Rysy placements.
    /// </summary>
    public static List<CopypasteHelper.CopiedSelection>? TryGetSelectionsFromLuaString(IReadOnlyList<IEditorLayer> layers, string selectionString) {
        if (DeserializeToList(selectionString) is { } luaSelections) {
            List<CopypasteHelper.CopiedSelection> copied = new();

            foreach (var obj in luaSelections) {
                if (obj is not Dictionary<string, object> selection)
                    continue;

                var layer = LonnLayerToSelectionLayer(layers, selection.GetValueOrDefault("_fromLayer") as string);

                var name = selection.GetValueOrDefault("_name") as string;
                // bg and fg decals don't have _name set, let's grab the hardcoded SID instead
                name ??= DefaultSidForLayer(layer);

                var tiles = selection.GetValueOrDefault("tiles") as string;
                if (layer is TileEditorLayer && tiles is null)
                    continue;

                if (layer is null || name is not { })
                    continue;

                var nodes = (selection.GetValueOrDefault("nodes") as List<object>)?
                            .OfType<Dictionary<string, object>>()
                            .Select(o => new BinaryPacker.Element() {
                                Attributes = o,
                            })
                            .ToArray();

                var data = new BinaryPacker.Element() {
                    Name = name,
                    Attributes = layer switch {
                        TileEditorLayer => new() {
                            ["text"] = tiles!,
                            ["x"] = Convert.ToInt32(selection["x"], CultureInfo.InvariantCulture) * 8 - 8,
                            ["y"] = Convert.ToInt32(selection["y"], CultureInfo.InvariantCulture) * 8 - 8,
                            ["w"] = selection["width"],
                            ["h"] = selection["height"],
                        },
                        _ => selection.Where(kv => kv.Key is not "_type" and not "_name" and not "nodes" and not "_id" and not "_fromLayer").ToDictionary(kv => kv.Key, kv => kv.Value)
                    },
                    Children = nodes!,
                };

                copied.Add(new() {
                    Data = data,
                    Layer = layer.Name,
                });
            }

            if (copied.Count > 0)
                return copied;
        }

        return null;
    }

    public static IEditorLayer? LonnLayerToSelectionLayer(IReadOnlyList<IEditorLayer> layers, string? typeStr) 
        => layers.FirstOrDefault(l => l is ILonnSerializableLayer { LonnLayerName: {} name } && name == typeStr);

    public static string? SelectionLayerToLonnLayer(IEditorLayer layer) =>
        (layer as ILonnSerializableLayer)?.LonnLayerName;

    public static string? SelectionLayerToLonnType(IEditorLayer layer) =>
        (layer as ILonnSerializableLayer)?.LoennInstanceTypeName;

    public static string? DefaultSidForLayer(IEditorLayer? layer) =>
        (layer as ILonnSerializableLayer)?.DefaultSid;

    /// <summary>
    /// Tries to convert a lua string representation of a table to a c# dictionary.
    /// </summary>
    public static Dictionary<string, object>? DeserializeToDict(string str) {
        return Deserialize(str, (lua) => lua.TableToDictionary(lua.GetTop()));
    }

    /// <summary>
    /// Tries to convert a lua string representation of a table to a c# list.
    /// </summary>
    public static List<object>? DeserializeToList(string str) {
        return Deserialize(str, (lua) => lua.ToList(lua.GetTop()));
    }

    /// <summary>
    /// Deserializes a lua value from a string, using <paramref name="valueGetter"/> to convert lua state into a c# object.
    /// </summary>
    public static T? Deserialize<T>(string str, Func<Lua, T?> valueGetter) where T : class {
        var lua = GetSandboxedLua();

        var sanitized = SanitizeCode(str);

        if (sanitized is not { }) {
            Console.WriteLine("un sanitized");
            return null;
        }

        if (PCallString(lua, sanitized) == LuaStatus.OK) {
            var d = valueGetter(lua);
            lua.Pop(1);
            return d;
        }

        return null;
    }

    /// <summary>
    /// Checks whether the given code is valid lua code, by running it in a sandboxed lua instance with a timeout.
    /// </summary>
    public static bool IsValidLua(string str) {
        var lua = GetSandboxedLua();
        if (PCallString(lua, str) != LuaStatus.OK)
            return false;
        
        lua.Pop(1);
        return true;
    }

    private static string? SanitizeCode(string str) {
        if (str is not ['{', .., '}'])
            return null;

        str = $"return {str}";

        return str;
    }

    private static readonly Lock _pCallStringLock = new();
    
    private static LuaStatus PCallString(Lua lua, string code, string? chunkName = null, int args = 0, int results = 1) {
        var st = lua.LoadString(code, chunkName ?? code);
        if (st != LuaStatus.OK)
            return st;

        const int millisecondsTimeout = 1_000;
        LuaStatus status = LuaStatus.ErrRun;

        lock (_pCallStringLock) {
            var task = Task.Run(() => lua.PCall(args, results, 0));

            if (task.Wait(millisecondsTimeout))
                status = task.Result;
            else {
                Logger.Write("LuaSerializer", LogLevel.Warning, $"Timed out trying to load string: {code}");

                // From now on, as soon as a line is executed, error
                // keep erroring until the script reaches the top
                // https://stackoverflow.com/questions/6913999/forcing-a-lua-script-to-exit
                lua.SetHook(static (lua, b) => {
                    lua.Error();
                }, LuaHookMask.Count, 1);
            }
        }

        return status;
    }

    [GeneratedRegex("^[a-zA-Z_][\\w_]*$")]
    private static partial Regex VariableNameRegex();
}
