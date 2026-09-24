using Rysy.Loading;
using Rysy.Mods;
using Rysy.Platforms;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace Rysy.Helpers;

public static class LangRegistry {
    public static ConcurrentDictionary<string, Lang> Languages { get; } = new();

    public static Lang FallbackLang { get; set; } = new("en_gb");

    public static Lang CurrentLang { get; set; } = new("en_gb");

    public static string Translate(string keyOrText) {
        if (CurrentLang == FallbackLang) {
            return CurrentLang.GetOrNull(keyOrText) ?? keyOrText;
        }

        return CurrentLang.GetOrNull(keyOrText)
            ?? FallbackLang.GetOrNull(keyOrText)
            ?? keyOrText;
    }

    public static string? TranslateOrNull(string? keyOrText)
        => TranslateOrNull(keyOrText.AsSpan());

    public static string? TranslateOrNull(ReadOnlySpan<char> keyOrText) {
        if (CurrentLang.GetOrNull(keyOrText) is { } currLang)
            return currLang;
        
        return CurrentLang != FallbackLang ? FallbackLang.GetOrNull(keyOrText) : null;
    }
    
    public static string? TranslateOrNull(Interpolator.Handler interpolated)
        => TranslateOrNull(interpolated.Result);

    private static void Initialize() {
        LangFileWatchers.DisposeAllAndClear();
        Languages.Clear();
        var enGb = new Lang("en_gb");
        Languages[enGb.Name] = enGb;

        if (Persistence.Instance?.Get("Language", enGb.Name) is { } currentLang && currentLang != enGb.Name) {
            Languages[currentLang] = CurrentLang = new Lang(currentLang);
        } else {
            CurrentLang = enGb;
        }
        
        FallbackLang = enGb;
    }
    
    public static async Task LoadAllAsync(SimpleLoadTask? task) {
        task?.SetMessage("Reading lang files");

        Initialize();
        
        await Task.WhenAll(ModRegistry.Mods.Values.Select(LoadFromModAsync));
    }

    private static readonly ConcurrentDictionary<(IModFilesystem, string), IDisposable> LangFileWatchers = [];

    internal static Task LoadRysyBuiltinAsync() {
        Initialize();
        
        return LoadFromModAsync(RysyPlatform.Current.GetRysyFilesystem(), isRysy: true);
    }

    internal static Task LoadFromModAsync(IModFilesystem fs, bool isRysy) {
        IEnumerable<string> files = fs.FindFilesInDirectoryRecursive(isRysy ? "lang" : "Loenn/lang", "lang");

        foreach (var file in files.ToList()) {
            var langName = file.FilenameNoExt() ?? "en_gb";
            fs.TryWatchAndOpen(file, stream => {
                try {
                    LoadFromLangFile(langName, stream.ReadAllText());
                } catch (Exception e) {
                    e.LogAsJson();
                }
            }, out var watcher);
            LangFileWatchers.SetAndDisposeOld((fs, file), watcher);
        }

        return Task.CompletedTask;
    }
    
    public static Task LoadFromModAsync(ModMeta mod) {
        return LoadFromModAsync(mod.Filesystem, mod.IsRysy);
    }

    public static void LoadFromLangFile(string name, string langFileContents) {
        if (string.IsNullOrWhiteSpace(langFileContents))
            return;

        Lang? lang;
        if (!Languages.TryGetValue(name, out lang)) {
            lang = new(name);
            Languages[name] = lang;
        }

        var enumerator = langFileContents.AsSpan().EnumerateSplits('\n');
        while (enumerator.MoveNext()) {
            var line = enumerator.Current;
            if (line.StartsWith('#'))
                continue;

            var splitIdx = line.IndexOf('=');
            if (splitIdx < 0)
                continue;
            var key = line[..splitIdx];
            var value = line[(splitIdx + 1)..].Trim().ToString().Replace(@"\n", "\n", StringComparison.Ordinal) ?? "";
            if (value is "\"\"\"") {
                // RYSY EXTENSION: """ for multiline lang entries
                StringBuilder builder = new();
                while (enumerator.MoveNext() && (line = enumerator.Current.TrimEnd()) is not "\"\"\"") {
                    builder.Append(CultureInfo.InvariantCulture, $"{line}\n");
                }

                value = builder.ToString().Trim();
            }
            lang.Translations[key.ToString()] = value;
        }
    }
}

public sealed class Lang {
    public string Name { get; set; }

    public ListenableDictionary<string, string> Translations { get; }
    
    private readonly Dictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> _translationsSpanLookup;

    public Lang(string name) { 
        Name = name;
        Translations = new();
        _translationsSpanLookup = Translations.GetSpanAlternateLookup();
    }

    public string? GetOrNull(string key)
        => Translations.GetValueOrDefault(key);
    
    public string? GetOrNull(ReadOnlySpan<char> key)
        => _translationsSpanLookup.TryGetValue(key, out var translated) ? translated : null;
}

public struct LangKey {
    public string Key { get; }

    private object[] _args = [];

    public object[] Args => _args;
    
    public LangKey(string key) {
        Key = key;
    }

    public LangKey(string key, params object[] args) {
        Key = key;
        _args = args;
    }
    
    public static LangKey Formatted(string key, params object[] args) => new LangKey(key, args);

    /// <summary>
    /// Gets the translated value of this lang key.
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
        if (Args.Length == 0)
            return Key.Translate();
        return Key.TranslateFormatted(Args);
    }

    public void SetArg(int argIndex, object value) {
        if (argIndex < 0)
            return;

        if (argIndex < Args.Length) {
            Args[argIndex] = value;
            return;
        }
        
        Array.Resize(ref _args, argIndex + 1);
        _args[argIndex] = value;
    }
    
    public static implicit operator LangKey(string str) => new LangKey(str);
}