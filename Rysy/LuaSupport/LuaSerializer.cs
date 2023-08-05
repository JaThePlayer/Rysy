using KeraLua;
using Rysy.Helpers;
using Rysy.Selections;

namespace Rysy.LuaSupport;

public static class LuaSerializer {
    private static Lua GetSandboxedLua() => new(openLibs: false);


    /// <summary>
    /// Tries to convert lonn-copied placements into Rysy placements.
    /// </summary>
    public static List<CopypasteHelper.CopiedSelection>? TryGetSelectionsFromLuaString(string selectionString) {
        if (DeserializeToList(selectionString) is { } luaSelections) {
            List<CopypasteHelper.CopiedSelection> copied = new();

            foreach (var obj in luaSelections) {
                if (obj is not Dictionary<string, object> selection)
                    continue;
                
                var layer = LonnLayerToSelectionLayer(selection.GetValueOrDefault("layer") as string);

                if (!selection.TryGetValue("item", out var itemObj) || itemObj is not Dictionary<string, object> item)
                    continue;

                var name = item.GetValueOrDefault("_name") as string;
                // bg and fg decals don't have _name set, let's grab the hardcoded SID instead
                name ??= DefaultSIDForLayer(layer);

                if (layer == SelectionLayer.None || name is not { })
                    continue;

                var nodes = (item.GetValueOrDefault("nodes") as List<object>)?
                            .OfType<Dictionary<string, object>>()
                            .Select(o => new BinaryPacker.Element() {
                                Attributes = o,
                            })
                            .ToArray();

                var data = new BinaryPacker.Element() {
                    Name = name,
                    Attributes = item.Where(kv => kv.Key is not "_type" and not "_name" and not "nodes" and not "_id").ToDictionary(kv => kv.Key, kv => kv.Value),
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
        _ => SelectionLayer.None,
    };

    public static string? DefaultSIDForLayer(SelectionLayer layer) => layer switch {
        SelectionLayer.BGDecals => EntityRegistry.BGDecalSID,
        SelectionLayer.FGDecals => EntityRegistry.FGDecalSID,
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

    private static string? SanitizeCode(string str) {
        if (str is not ['{', .., '}'])
            return null;

        str = $"return {str}";

        return str;
    }

    private static LuaStatus PCallString(Lua lua, string code, string? chunkName = null, int args = 0, int results = 1) {
        var st = lua.LoadString(code, chunkName ?? code);
        if (st != LuaStatus.OK) {
            return st;
        }

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
}
