using Neo.IronLua;
using Rysy.Graphics;
using Rysy.Mods;
using System.Text;
using System.Text.RegularExpressions;

namespace Rysy.NeoLuaSupport;

internal sealed class Env : LuaGlobal {
    public Env(Lua lua) : base(lua) {
    }

    protected override object OnIndex(object key) {
        var r = base.OnIndex(key);
        if (r is null) {
            //throw new MissingGlobalException(key);
            Console.WriteLine(new MissingGlobalException(key));
            return null!;
        }

        if (key is "string") {
            Console.WriteLine(key);
        }

        return r;
    }

    public sealed class MissingGlobalException : Exception {
        public object Key;

        public MissingGlobalException(object key) : base() {
            Key = key;
        }

        public override string Message => $"Tried to access global {Key} that doesn't exist";
    }
}

public static class LuaLoader {
    private static readonly string[] RequireSearchPaths = new string[] {
        "?.lua",
        "Assets/lonnShims/?.lua"
    };

    private static readonly Dictionary<string, object> LoadedModules = new(StringComparer.Ordinal);

    private static Lua? _Lua;

    public static Lua Lua => _Lua ??= Init();

    private static LuaGlobal? _G;
    public static LuaGlobal G {
        get {
            if (_G is null) {
#pragma warning disable CA2000 // Dispose objects before losing scope - the lua instance here doesn't lose scope
                Init();
#pragma warning restore CA2000
            }

            return _G!;
        }
    }

    public static string? CurrentMod {
        get => G["_RYSY_CURRENT_MOD"].ToString();
        set => G["_RYSY_CURRENT_MOD"] = value;
    }

    private static Lua Init() {
        var l = new Lua();
        _G = l.CreateEnvironment<Env>();

        G["NEO_LUA"] = true;

        G.DefineFunction("require", (string module) => {
            if (LoadedModules.TryGetValue(module, out var cached)) {
                if (cached is RequireFailException fail) {
                    throw fail;
                }
                return cached;
            }
            string fixedModule = module.Replace('.', '/');

            string? foundPath = null;
            foreach (var searchPath in RequireSearchPaths) {
                var path = CalcPath(searchPath);

                if (File.Exists(path)) {
                    foundPath = path;
                    break;
                }
            }

            if (foundPath is null) {
                throw new Exception($"Lua module {module} not found. Attempted paths:\n{string.Join('\n', RequireSearchPaths.Select(p => CalcPath(p)))}");
            }

            //("require", module).LogAsJson();
            var libString = File.ReadAllText(foundPath);
            try {
                var ret = DoString(libString, module);
                LoadedModules[module] = ret;
                return ret;
            } catch (Exception ex) {
                var e = new RequireFailException(module, FixupLua(libString), ex);
                LoadedModules[module] = e;
                throw e;
            }

            string CalcPath(string searchPath) => searchPath.Replace("?", fixedModule, StringComparison.Ordinal);
        });

        G.DefineFunction("_RYSY_INTERNAL_requireFromPlugin", (string lib, string modName) => {
            if (ModRegistry.GetModByName(modName) is not { } mod) {
                throw new Exception($"Mod {modName} is not loaded, but tried to access lib {lib} from it!");
            }
            var module = $"_{modName}_{lib}";
            if (LoadedModules.TryGetValue(module, out var cached)) {
                if (cached is RequireFailException fail) {
                    throw fail;
                }
                return cached;
            }

            var path = $"Loenn/{lib.Replace('.', '/')}.lua";

            if (mod.Filesystem.TryReadAllText(path) is not { } libString) {
                return null;
            }

            //(lib).LogAsJson();

            try {
                var ret = DoString(libString, module);
                LoadedModules[module] = ret;
                return ret;
            } catch (Exception ex) {
                var e = new RequireFailException(module, FixupLua(libString), ex);
                LoadedModules[module] = e;
                throw e;
            }
        });

        G.DefineFunction("_RYSY_INTERNAL_getModSetting", (string modName, string settingName) => {
            var mod = ModRegistry.GetModByName(modName);

            if (mod is not { Settings: { } settings }) {
                return null;
            }

            if (settings.OtherValues.TryGetValue(settingName, out var value)) {
                return value;
            }

            var bindings = LonnBindingHelper.GetAllBindings(settings.GetType());
            if (bindings.TryGetValue(settingName, out var prop)) {
                return prop.GetValue(settings);
            }
            return null;
        });

        G.DefineFunction("_RYSY_INTERNAL_setModSetting", (string modName, string settingName, object value) => {
            var mod = ModRegistry.GetModByName(modName);

            if (mod is not { Settings: { } settings }) {
                return;
            }

            var bindings = LonnBindingHelper.GetAllBindings(settings.GetType());
            if (bindings.TryGetValue(settingName, out var prop)) {
                //try {
                prop.SetValue(settings, value);
                settings.Save();
                return;
                //} catch (Exception e) {

                //}
            }

            // todo: handle tables being passed here - they need to get a metatable (or we handle that in lua...)
            settings.OtherValues[settingName] = value;
            settings.Save();
        });

        G.DefineFunction("_RYSY_log", (string status, string message) => {
            var logLevel = status switch {
                "DEBUG" => LogLevel.Debug,
                "INFO" => LogLevel.Info,
                "WARNING" => LogLevel.Warning,
                "ERROR" => LogLevel.Error,
                _ => LogLevel.Debug,
            };

            Logger.Write("Lua", logLevel, message);
        });

        G.DefineFunction("_RYSY_MODS_find", (string modName) => {
            return ModRegistry.GetModByName(modName);
        });

        G.DefineFunction("_RYSY_bit_lshift", (int x, int n) => {
            return x << n;
        });

        G.DefineFunction("_RYSY_DRAWABLE_exists", (string tex) => {
            return GFX.Atlas.Exists(tex);
        });

        //_RYSY_DRAWABLE_getTextureSize(texturePath) -> number, number, number, number, number, number
        G.DefineFunction("_RYSY_DRAWABLE_getTextureSize", (string tex) => {
            var texture = GFX.Atlas[tex];
            var clipRect = texture.ClipRect;

            //x,y,w,h,offX,offY
            return new LuaResult(clipRect.X, clipRect.Y, clipRect.Width, clipRect.Height, texture.DrawOffset.X, texture.DrawOffset.Y);
        });

        G.DoChunk("Assets/lua/rysy_meta.lua");

        G.DefineFunction("__string_find", Find);

        G.DoChunk("""
            --string.find = __string_find

            print("selene")
            selene = require("Assets.lua.selene_parser")
            print(selene)
            --[[

            if selene and selene.load then
                selene.load(nil, true)
            end
            _G.selene = selene
            --]]
        """, "selene_loader");

        // todo: load internal funcs for c#-lua interop

        // todo: disable clr interop

        return l;
    }

    private static Dictionary<string, Regex> LuaRegexes = new(StringComparer.Ordinal);

    public static object[] Find(string s, string pattern, int init = 1, bool plain = false, bool dontReturnMatches = false) {
        if (string.IsNullOrEmpty(s))
            return !string.IsNullOrEmpty(pattern) || init != 1 ? LuaResult.Empty : new LuaResult((object) 1);
        if (string.IsNullOrEmpty(pattern))
            return LuaResult.Empty;
        if (init < 0)
            init = s.Length + init + 1;
        if (init <= 0)
            init = 1;
        if (plain) {
            int num = s.IndexOf(pattern, init - 1, StringComparison.Ordinal);
            if (num == -1)
                return null!;
            return new LuaResult(new object[2]
            {
                (num + 1),
                (num + pattern.Length)
            });
        }
        Match match = CreateRegexFromLuaPattern(pattern).Match(s, init - 1);
        if (!match.Success)
            return LuaResult.Empty;
        object[] objArray = new object[dontReturnMatches ? 2 : match.Groups.Count + 1];
        objArray[0] = (match.Index + 1);
        objArray[1] = (match.Index + match.Length);
        if (!dontReturnMatches)
            for (int groupnum = 1; groupnum < match.Groups.Count; ++groupnum)
                objArray[groupnum + 1] = (object) match.Groups[groupnum].Value;

        return objArray;
        //return (LuaResult) objArray;
    }

    public static Regex CreateRegexFromLuaPattern(string pattern) {
        if (LuaRegexes.TryGetValue(pattern, out var cached))
            return cached;

        var cSharpPattern = TranslateRegularExpression(pattern).Item1;
        //Console.WriteLine($@"Translated {pattern} to {cSharpPattern}");
        var regex = new Regex(cSharpPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
        LuaRegexes[pattern] = regex;

        return regex;
    }

    private static Tuple<string, bool[]> TranslateRegularExpression(ReadOnlySpan<char> regEx) {
        //if (!LuaLibraryString.translateRegEx)
        //    return new Tuple<string, bool[]>(regEx, (bool[]) null);
        StringBuilder stringBuilder = new StringBuilder();
        bool flag1 = false;
        bool flag2 = false;
        List<bool> boolList = new List<bool>() { false };
        for (int index = 0; index < regEx.Length; ++index) {
            char ch1 = regEx[index];
            if (flag1) {
                switch (ch1) {
                    case '%':
                        stringBuilder.Append('%');
                        flag1 = false;
                        continue;
                    case 'A':
                        stringBuilder.Append("\\P{L}");
                        break;
                    case 'C':
                        stringBuilder.Append("[\\P{C}]");
                        break;
                    case 'D':
                        stringBuilder.Append("\\D");
                        break;
                    case 'G':
                        stringBuilder.Append("[\\p{C}\\s]");
                        break;
                    case 'L':
                        stringBuilder.Append("\\P{Ll}");
                        break;
                    case 'P':
                        stringBuilder.Append("\\P{P}");
                        break;
                    case 'S':
                        stringBuilder.Append("\\S");
                        break;
                    case 'U':
                        stringBuilder.Append("\\P{Lu}");
                        break;
                    case 'W':
                        stringBuilder.Append("\\W");
                        break;
                    case 'X':
                        stringBuilder.Append("[^0-9A-Fa-f]");
                        break;
                    case 'a':
                        stringBuilder.Append("\\p{L}");
                        break;
                    case 'b':
                        if (index >= regEx.Length - 2)
                            throw new Exception();
                        char ch2 = regEx[index + 1];
                        char ch3 = regEx[index + 2];
                        stringBuilder.Append('(');
                        stringBuilder.Append(Regex.Escape(ch2.ToString()));
                        stringBuilder.Append("(?>(?<n>");
                        stringBuilder.Append(Regex.Escape(ch2.ToString()));
                        stringBuilder.Append(")|(?<-n>");
                        stringBuilder.Append(Regex.Escape(ch3.ToString()));
                        stringBuilder.Append(")|(?:[^");
                        stringBuilder.Append(Regex.Escape(ch2.ToString()));
                        stringBuilder.Append(Regex.Escape(ch3.ToString()));
                        stringBuilder.Append("]*))*");
                        stringBuilder.Append(Regex.Escape(ch3.ToString()));
                        stringBuilder.Append("(?(n)(?!)))");
                        index += 2;
                        break;
                    case 'c':
                        stringBuilder.Append("\\p{C}");
                        break;
                    case 'd':
                        stringBuilder.Append("\\d");
                        break;
                    case 'g':
                        stringBuilder.Append("[^\\p{C}\\s]");
                        break;
                    case 'l':
                        stringBuilder.Append("\\p{Ll}");
                        break;
                    case 'p':
                        stringBuilder.Append("\\p{P}");
                        break;
                    case 's':
                        stringBuilder.Append("\\s");
                        break;
                    case 'u':
                        stringBuilder.Append("\\p{Lu}");
                        break;
                    case 'w':
                        stringBuilder.Append("\\w");
                        break;
                    case 'x':
                        stringBuilder.Append("[0-9A-Fa-f]");
                        break;
                    default:
                        stringBuilder.Append('\\');
                        stringBuilder.Append(ch1);
                        break;
                }
                flag1 = false;
            } else {
                switch (ch1) {
                    case '%':
                        flag1 = true;
                        continue;
                    case '\\':
                        stringBuilder.Append("\\\\");
                        continue;
                    default:
                        if (flag2) {
                            if (ch1 == ']')
                                flag2 = false;
                            stringBuilder.Append(ch1);
                            continue;
                        }
                        switch (ch1) {
                            case '-':
                                stringBuilder.Append("*?");
                                continue;
                            case '[':
                                stringBuilder.Append('[');
                                flag2 = true;
                                continue;
                            default:
                                if (ch1 == '^' && !flag2) {
                                    stringBuilder.Append("\\G");
                                    continue;
                                }
                                if (ch1 == '(') {
                                    stringBuilder.Append('(');
                                    boolList.Add(index + 1 < regEx.Length && regEx[index + 1] == ')');
                                    continue;
                                }
                                stringBuilder.Append(ch1);
                                continue;
                        }
                }
            }
        }
        return new Tuple<string, bool[]>(stringBuilder.ToString(), boolList.ToArray());
    }

    public static LuaResult DoString(string lua, string filename, params KeyValuePair<string, object>[] args) {
        lua = FixupLua(lua);

        var res = G.DoChunk(lua, filename, args);

        return res;
    }

    public static string RunSelene(string lua) {
        if (G["selene"] is LuaTable selene) {
            dynamic s = selene;
            lua = s.parse(lua);
        }

        return lua;
    }

    public static string FixupLua(string lua) {
        lua = RunSelene(lua);

        lua = new Regex(@"\^ -([^\(\)]*)").Replace(lua, @"^ (-$1)");

        return lua;
    }
}

public class RequireFailException : Exception {
    private static string PrepMessage(string module, string code, Exception ex) {
        if (ex is LuaParseException parse) {
            return $"Syntax error in module {module} at {parse.Line}:{parse.Column}: {code}";

        } else {
            return $"Failed to require module {module}";
        }
    }

    public RequireFailException(string module, string code, Exception inner) : base(PrepMessage(module, code, inner), inner) {

    }
}
