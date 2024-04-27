using Rysy.Extensions;
using Rysy.Loading;
using Rysy.Mods;
using Rysy.Scenes;
using System.Collections.Concurrent;

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

    public static string? TranslateOrNull(string keyOrText)
        => TranslateOrNull(StringRef.FromString(keyOrText));
    
    public static string? TranslateOrNull(ReadOnlySpan<char> keyOrText)
        => TranslateOrNull(StringRef.FromSpanIntoShared(keyOrText));
    
    public static string? TranslateOrNull(Interpolator.Handler interpolated)
        => TranslateOrNull(StringRef.FromSharedBuffer(interpolated.Data, interpolated.Length));

    public static string? TranslateOrNull(StringRef keyOrTextRef) {
        if (CurrentLang.GetOrNull(keyOrTextRef) is { } currLang)
            return currLang;
        
        return CurrentLang != FallbackLang ? FallbackLang.GetOrNull(keyOrTextRef) : null;
    }

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
        var files = fs.FindFilesInDirectoryRecursive("Loenn/lang", "lang")
            .Concat(fs.FindFilesInDirectoryRecursive("lang", "lang"));

        foreach (var file in files.ToList()) {
            var langName = file.FilenameNoExt()!;
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

        foreach (var line in langFileContents.Split('\n')) {
            if (line.StartsWith('#'))
                continue;

            if (line.Split('=', count: 2, StringSplitOptions.RemoveEmptyEntries) is not [var key, var value]) {
                continue;
            }

            value = value?.Replace(@"\n", "\n", StringComparison.Ordinal).Trim() ?? "";
            lang.Translations[StringRef.FromString(key)] = value;
        }
    }
}

public class Lang {
    public string Name { get; set; }

    public Dictionary<StringRef, string> Translations { get; set; } = new();

    public Lang(string name) { 
        Name = name;
    }

    public string? GetOrNull(string key) => Translations.GetValueOrDefault(StringRef.FromString(key));
    
    public string? GetOrNull(ReadOnlySpan<char> key) => Translations.GetValueOrDefault(StringRef.FromSpanIntoShared(key));
    
    public string? GetOrNull(StringRef key) => Translations.GetValueOrDefault(key);
}
