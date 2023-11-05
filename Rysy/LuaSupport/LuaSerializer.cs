using KeraLua;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.Selections;
using System.Text;
using System.Text.RegularExpressions;

namespace Rysy.LuaSupport;

public static partial class LuaSerializer {
    private static Lua GetSandboxedLua() => new(openLibs: false);

    private static string CorrectDecalPathForLonn(string rysyPath) {
        // lonn fails to access the decal sprite for animated decals if the texture path does not end with 00...
        if (GFX.Atlas.TryGet($"decals/{rysyPath}00", out _))
            return $"{rysyPath}00";

        return rysyPath;
    }

    public static string? ConvertSelectionsToLonnString(List<CopypasteHelper.CopiedSelection> copied) {
        if (copied is not { Count: > 0 })
            return null;

        var builder = new StringBuilder();
        builder.AppendLine("{");
        foreach (var item in copied)
            switch (item.Layer) {
                case SelectionLayer.FGDecals:
                case SelectionLayer.BGDecals:
                    builder.AppendLine(CultureInfo.InvariantCulture, $$"""
                            {
                                _fromLayer = "{{SelectionLayerToLonnLayer(item.Layer)}}",
                                texture = "decals/{{CorrectDecalPathForLonn(item.Data.Attr("texture").TrimStart("decals/"))}}",
                                scaleX = {{ToLuaString(item.Data.Float("scaleX", 1))}},
                                scaleY = {{ToLuaString(item.Data.Float("scaleY", 1))}},
                                rotation = {{ToLuaString(item.Data.Float("rotation", 0))}},
                                x = {{ToLuaString(item.Data.Float("x", 0))}},
                                y = {{ToLuaString(item.Data.Float("y", 0))}},
                                color = {{ToLuaString(item.Data.Attr("color", "ffffff"))}}
                        """);
                    AppendData(builder, item, blacklistedKeys: new() { "texture", "scaleX", "scaleY", "rotation", "x", "y" });
                    builder.AppendLine("""
                            },
                        """);
                    break;
                case SelectionLayer.Entities:
                case SelectionLayer.Triggers:
                    builder.AppendLine(CultureInfo.InvariantCulture, $$"""
                            {
                                _fromLayer = "{{SelectionLayerToLonnLayer(item.Layer)}}",
                                _name = "{{item.Data.Name}}",
                                _id = {{item.Data.Int("id", 0)}},
                        """);
                    if (SelectionLayerToLonnType(item.Layer) is { } type)
                        builder.AppendLine(CultureInfo.InvariantCulture, $"""
                                    _type = "{type}",
                            """);

                    if (item.Data.Children is { Length: > 0 } nodes)
                        builder.AppendLine(CultureInfo.InvariantCulture, $$"""
                                    nodes = {{{string.Join(",", nodes.Select(n => $$"""
                                            {x={{n.Int("x")}},y={{n.Int("y")}}}
                                            """))}}},
                            """);
                    AppendData(builder, item, blacklistedKeys: new() { "id" });
                    builder.AppendLine("""
                            },
                        """);
                    break;
                case SelectionLayer.FGTiles:
                case SelectionLayer.BGTiles:
#warning Support copying tiles to lonn format once its not broken in lonn
                    return null;
                case SelectionLayer.Rooms:
                    return null;
            }

        builder.Append('}');
        return builder.ToString();

        static void AppendData(StringBuilder builder, CopypasteHelper.CopiedSelection item, HashSet<string> blacklistedKeys) {
            foreach (var (k, v) in item.Data.Attributes) {
                if (blacklistedKeys.Contains(k))
                    continue;

                builder.AppendLine(CultureInfo.InvariantCulture, $$"""
                                    {{TableKeyString(k)}} = {{ToLuaString(v)}},
                            """);
            }
        }
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

    private static HashSet<string> LuaKeywords = new() {
        "and", "break", "do", "else", "elseif", "end", "false", "for",
        "function", "goto", "if", "in", "local", "nil", "not", "or",
        "repeat", "return", "then", "true", "until", "while"
    };

    private static string ToLuaString(object obj) => obj switch {
        string s => $"""
        "{SanitizeString(s)}"
        """,
        int i => i.ToString(CultureInfo.InvariantCulture),
        long i => i.ToString(CultureInfo.InvariantCulture),
        float f => f.ToString(CultureInfo.InvariantCulture),
        double f => f.ToString(CultureInfo.InvariantCulture),
        bool b => b ? "true" : "false",
        _ => obj.ToString()!,
    };

    private readonly static char[] EscapableChars = new char[] { '\a', '\b', '\f', '\n', '\r', '\t', '\v', '\\', '"', '\'' };
    private readonly static Dictionary<char, string> EscapeSequences = new() {
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
    public static List<CopypasteHelper.CopiedSelection>? TryGetSelectionsFromLuaString(string selectionString) {
        if (DeserializeToList(selectionString) is { } luaSelections) {
            List<CopypasteHelper.CopiedSelection> copied = new();

            foreach (var obj in luaSelections) {
                if (obj is not Dictionary<string, object> selection)
                    continue;

                var layer = LonnLayerToSelectionLayer(selection.GetValueOrDefault("_fromLayer") as string);

                var name = selection.GetValueOrDefault("_name") as string;
                // bg and fg decals don't have _name set, let's grab the hardcoded SID instead
                name ??= DefaultSIDForLayer(layer);

                var tiles = selection.GetValueOrDefault("tiles") as string;
                if (layer is SelectionLayer.FGTiles or SelectionLayer.BGTiles && tiles is null)
                    continue;

                if (layer == SelectionLayer.None || name is not { })
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
                        SelectionLayer.FGTiles or SelectionLayer.BGTiles => new() {
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
                    Layer = layer,
                });
            }

            if (copied.Count > 0)
                return copied;
        }

        return null;
    }

    public static SelectionLayer LonnLayerToSelectionLayer(string? typeStr) => typeStr switch {
        "entities" => SelectionLayer.Entities,
        "triggers" => SelectionLayer.Triggers,
        "decalsBg" => SelectionLayer.BGDecals,
        "decalsFg" => SelectionLayer.FGDecals,
        "tilesFg" => SelectionLayer.FGTiles,
        "tilesBg" => SelectionLayer.BGTiles,
        _ => SelectionLayer.None,
    };

    public static string? SelectionLayerToLonnLayer(SelectionLayer layer) => layer switch {
        SelectionLayer.Entities => "entities",
        SelectionLayer.Triggers => "triggers",
        SelectionLayer.BGDecals => "decalsBg",
        SelectionLayer.FGDecals => "decalsFg",
        SelectionLayer.FGTiles => "tilesFg",
        SelectionLayer.BGTiles => "tilesBg",
        _ => null,
    };

    public static string? SelectionLayerToLonnType(SelectionLayer layer) => layer switch {
        SelectionLayer.Entities => "entity",
        SelectionLayer.Triggers => "trigger",
        _ => null,
    };

    public static string? DefaultSIDForLayer(SelectionLayer layer) => layer switch {
        SelectionLayer.BGDecals => EntityRegistry.BGDecalSID,
        SelectionLayer.FGDecals => EntityRegistry.FGDecalSID,
        SelectionLayer.FGTiles or SelectionLayer.BGTiles => "tiles",
        _ => null,
    };

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
        using var lua = GetSandboxedLua();

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

    private static string? SanitizeCode(string str) {
        if (str is not ['{', .., '}'])
            return null;

        str = $"return {str}";

        return str;
    }

    private static LuaStatus PCallString(Lua lua, string code, string? chunkName = null, int args = 0, int results = 1) {
        var st = lua.LoadString(code, chunkName ?? code);
        if (st != LuaStatus.OK)
            return st;

        const int millisecondsTimeout = 1_000;
        LuaStatus status = LuaStatus.ErrRun;

        lock (lua) {
            var task = Task.Run(() => {
                return lua.PCall(args, results, 0);
            });

            if (task.Wait(millisecondsTimeout))
                status = task.Result;
            else {
                Logger.Write("LuaSerializer", LogLevel.Warning, $"Timed out trying to load string: {code}");

                // From now on, as soon as a line is executed, error
                // keep erroring until the script reaches the top
                // https://stackoverflow.com/questions/6913999/forcing-a-lua-script-to-exit
                lua.SetHook(static (s, b) => {
                    var lua = Lua.FromIntPtr(s);

                    lua.Error();
                }, LuaHookMask.Count, 1);
            }
        }

        return status;
    }

    [GeneratedRegex("^[a-zA-Z_][\\w_]*$")]
    private static partial Regex VariableNameRegex();
}
