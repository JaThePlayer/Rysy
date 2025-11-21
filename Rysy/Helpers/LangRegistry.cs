using Rysy.Loading;
using Rysy.Mods;
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

    public static async Task LoadAllAsync(SimpleLoadTask? task) {
        task?.SetMessage("Reading lang files");

        Languages.Clear();
        Languages["en_gb"] = new("en_gb");
        
        await Task.WhenAll(ModRegistry.Mods.Values.Select(LoadFromModAsync));

        FallbackLang = Languages[Persistence.Instance.Get("Language", "en_gb")];
        CurrentLang = Languages["en_gb"];
    }

    public static Task LoadFromModAsync(ModMeta mod) {
        var fs = mod.Filesystem;

        IEnumerable<string> files = fs.FindFilesInDirectoryRecursive(mod.IsRysy ? "lang" : "Loenn/lang", "lang");

        foreach (var file in files.ToList()) {
            var langName = file.FilenameNoExt() ?? "en_gb";
            fs.TryWatchAndOpen(file, stream => {
                try {
                    LoadFromLangFile(langName, stream.ReadAllText());
                } catch (Exception e) {
                    e.LogAsJson();
                }
            });
        }

        return Task.CompletedTask;
        //await Task.WhenAll(files.SelectToTaskRun(f => LoadFromLangFile(f.FilenameNoExt()!, fs.TryReadAllText(f)!)));
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
