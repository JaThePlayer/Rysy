using KeraLua;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.LuaSupport;
using System.Text;
using System.Text.RegularExpressions;

namespace Rysy.Gui.FieldTypes.Modded;

internal sealed record FrostHelperAttachGroup : DropdownField<int>, ILonnField {
    public static string Name => "FrostHelper.attachGroup";

    public FrostHelperAttachGroup(string def) {
        Default = int.TryParse(def, CultureInfo.InvariantCulture, out var i) ? i : 0;
        Editable = true;
        Values = _ => EditorState.CurrentRoom is { } room
            ? room.Entities.Concat(room.Triggers)
                .Where(e => e.Has("attachGroup"))
                .Select(e => e.Int("attachGroup"))
                .SafeToDictionary(e => (e, e.ToString(CultureInfo.InvariantCulture)))
            : [];
    }
    
    public static Field Create(object? def, IUntypedData fieldInfoEntry) => new FrostHelperAttachGroup(def?.ToString() ?? "");
}

internal sealed record FrostHelperTexturePath : ILonnField {
    public static string Name => "FrostHelper.texturePath";
    
    public static Field Create(object? def, IUntypedData fieldInfoEntry) {
        var luaPattern = fieldInfoEntry.Attr("pattern");
        var langDir = fieldInfoEntry.Attr("langDir");

        Func<FoundPath, string>? captureConverter = null;
        if (fieldInfoEntry.TryGetValue("captureConverter", out var obj) && obj is LuaFunctionRef captureFunc) {
            captureConverter = (FoundPath p) => {
                if (p.Match is null)
                    return p.Captured;
                
                var lua = captureFunc.Lua;
                captureFunc.PushToStack();

                var args = PushCapturesToLua(p, lua);
                
                lua.Call(args, 1);

                var ret = lua.ToString(lua.GetTop());
                lua.Pop(1);
                
                return ret;
            };
        }
        
        Func<FoundPath, bool>? filter = null;
        if (fieldInfoEntry.TryGetValue("filter", out obj) && obj is LuaFunctionRef filterFunc) {
            filter = (FoundPath p) => {
                if (p.Match is null)
                    return true;
                
                var lua = filterFunc.Lua;
                filterFunc.PushToStack();

                var args = PushCapturesToLua(p, lua);
                
                lua.Call(args, 1);

                var ret = lua.ToBoolean(lua.GetTop());
                lua.Pop(1);
                
                return ret;
            };
        }

        Func<FoundPath, string, string?>? displayConverter = null;
        if (fieldInfoEntry.TryGetValue("displayConverter", out obj) && obj is LuaFunctionRef displayNameFunc) {
            displayConverter = (FoundPath p, string saved) => {
                if (LangRegistry.TranslateOrNull($"FrostHelper.paths.{langDir}.{saved}") is { } manuallyTranslated)
                    return manuallyTranslated;
                
                if (p.Match is null)
                    return p.Captured;
                
                var lua = displayNameFunc.Lua;
                displayNameFunc.PushToStack();

                var args = PushCapturesToLua(p, lua);
                
                lua.Call(args, 1);

                var ret = lua.ToString(lua.GetTop());
                lua.Pop(1);
                
                return ret;
            };
        } else {
            displayConverter = (p, saved) => LangRegistry.TranslateOrNull($"FrostHelper.paths.{langDir}.{saved}");
        }

        var regex = LuaPatternToRegex(luaPattern);
        return Fields.AtlasPath(def?.ToString() ?? "", regex, captureConverter) with {
            DisplayNameGetter = displayConverter,
            Filter = filter ?? (_ => true),
        };
    }
    
    private static int PushCapturesToLua(FoundPath p, Lua lua) {
        var args = 0;
        if (p.Match is { Groups: {} groups }) {
            for (int i = 1; i < groups.Count; i++) {
                Group g = groups[i];
                lua.PushString(g.Value);
                args++;
            }
        } else {
            lua.PushString(p.Captured);
            args++;
        }

        return args;
    }
    
    private static readonly System.Buffers.SearchValues<char> LuaPatternMagicChars = System.Buffers.SearchValues.Create(".%[]()^*?");
    private static readonly System.Buffers.SearchValues<char> LuaPatternMagicCharsInGroup = System.Buffers.SearchValues.Create(".%[]()");
    
    private static readonly Dictionary<string, string> CachedLuaPatterns = new();
    
    private static string LuaPatternToRegex(string pattern)
    {
        if (CachedLuaPatterns.TryGetValue(pattern, out var cached))
            return cached;

        var format = pattern.AsSpan();
        var ret = new StringBuilder();

        var inCharGroup = 0;

        if (format is ['^', ..])
        {
            ret.Append('^');
            format = format[1..];
        }
        
        while (true)
        {
            var nextHoleIdx = format.IndexOfAny(inCharGroup > 0 ? LuaPatternMagicCharsInGroup : LuaPatternMagicChars);
            if (nextHoleIdx < 0)
            {
                if (inCharGroup > 1)
                    ret.Append('|');
                ret.Append(format);
                break;
            }
            
            if (nextHoleIdx > 0)
            {
                if (inCharGroup == 1)
                {
                    ret.Append(format[..nextHoleIdx]);
                }
                else if (inCharGroup > 1)
                {
                    ret.Append("]|[");
                    ret.Append(format[..nextHoleIdx]);
                }
                else
                {
                    ret.Append(Regex.Escape(format[..nextHoleIdx].ToString()));
                }
                
                if (inCharGroup == 1)
                    inCharGroup = 2;
            }
            
            switch (format[nextHoleIdx])
            {
                case '.':
                    ret.Append(format[nextHoleIdx]);
                    if (nextHoleIdx + 1 < format.Length)
                    {
                        format = format[(nextHoleIdx + 1)..];
                        format = AppendModifier(format);
                    }
                    else
                        format = [];

                    continue;
                case '(' or '^':
                    ret.Append(format[nextHoleIdx]);
                    if (nextHoleIdx + 1 < format.Length)
                    {
                        format = format[(nextHoleIdx + 1)..];
                    }
                    else
                        format = [];
                    continue;
                case ')':
                    ret.Append(format[nextHoleIdx]);
                    if (nextHoleIdx + 1 < format.Length)
                    {
                        format = format[(nextHoleIdx + 1)..];
                        format = AppendModifier(format);
                    }
                    else
                        format = [];
                    continue;
                case '[':
                    ret.Append("(?:[");
                    inCharGroup = 1;
                    if (nextHoleIdx + 1 < format.Length)
                    {
                        format = format[(nextHoleIdx + 1)..];
                    }
                    else
                        format = [];
                    continue;
                case ']':
                    inCharGroup = 0;
                    ret.Append("])");
                    if (nextHoleIdx + 1 < format.Length)
                    {
                        format = format[(nextHoleIdx + 1)..];
                        format = AppendModifier(format);
                    }
                    else
                        format = [];
                    continue;
                case '*' or '?':
                    ret.Append(format[nextHoleIdx]);
                    if (nextHoleIdx + 1 < format.Length)
                    {
                        format = format[(nextHoleIdx + 1)..];
                    }
                    else
                        format = [];
                    continue;
            }

            if (nextHoleIdx + 1 >= format.Length)
                throw new Exception("A '%' character may not appear at the end of a pattern string");

            var formatType = format[nextHoleIdx + 1];
            format = format[(nextHoleIdx + 2)..];
            
            if (formatType == '%') // escape char
            {
                ret.Append('%');
                continue;
            }

            var regexFormat = formatType switch
            {
                'a' => @"\p{L}", //@"[a-zA-Z]", // letters
                'A' => @"\P{L}", // @"[^a-zA-Z]",
                'c' => @"[\x00-\x1F\x7F]", // control
                'C' => @"[^\x00-\x1F\x7F]",
                'd' => @"\d",
                'D' => @"\D",
                'l' => "[a-z]", // lowercase
                'L' => "[^a-z]",
                'p' => @"[\!-/:-@\[-\`\{-\~]", // punctuation
                'P' => @"[^\!-/:-@\[-\`\{-\~]",
                's' => @"\s",
                'S' => @"\S",
                'u' => @"[A-Z]",
                'U' => @"[^A-Z]",
                'w' => @"\w",
                'W' => @"\W",
                'x' => @"[0-9a-fA-F]",
                'X' => @"[^0-9a-fA-F]",
                'z' => @"\x00",
                'Z' => @"[^\x00]",
                'b' => throw new NotImplementedException("%b"), // balanced string
                '0' or '1' or '2' or '3' or '4' or '5' or '6' or '7' or '8' or '9' 
                    => throw new NotImplementedException("Capture references in pattern"),
                '[' => @"\[",
                _ => formatType.ToString() // % as an escape char
            };

            if (regexFormat.StartsWith('['))
            {
                if (inCharGroup == 0)
                {
                    ret.Append(regexFormat);
                } else if (inCharGroup == 1)
                {
                    inCharGroup = 2;
                    ret.Append(regexFormat[1..^1]);
                }
                else
                {
                    ret.Append("]|");
                    ret.Append(regexFormat[..^1]);
                }
            }
            else
            {
                ret.Append(regexFormat);
            }

            format = AppendModifier(format);
        }

        var finishedStr = ret.ToString();
        CachedLuaPatterns[pattern] = finishedStr;
        return finishedStr;

        ReadOnlySpan<char> AppendModifier(ReadOnlySpan<char> format)
        {
            if (format.Length > 0)
            {
                var modifier = format[0] switch
                {
                    '+' => "+",
                    '*' => "*",
                    '-' => "*?",
                    '?' => "?",
                    _ => null,
                };
                if (modifier is null)
                    return format;
                ret.Append(modifier);
                format = format[1..];
            }

            return format;
        }
    }
}
