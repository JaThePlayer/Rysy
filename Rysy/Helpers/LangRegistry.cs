using Rysy.Extensions;
using Rysy.Mods;
using Rysy.Scenes;

namespace Rysy.Helpers;

public static class LangRegistry {
    public static Dictionary<string, Lang> Languages { get; } = new();

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

    public static string? TranslateOrNull(string keyOrText) {
        if (CurrentLang == FallbackLang) {
            return CurrentLang.GetOrNull(keyOrText);
        }

        return CurrentLang.GetOrNull(keyOrText)
            ?? FallbackLang.GetOrNull(keyOrText);
    }

    public static async Task LoadAllAsync() {
        LoadingScene.Text = "Reading lang files";

        Languages.Clear();
        
        await Task.WhenAll(ModRegistry.Mods.Values.Select(LoadFromModAsync));

        FallbackLang = Languages[Persistence.Instance.Get("Language", "en_gb")];
        CurrentLang = Languages["en_gb"];
    }

    public static Task LoadFromModAsync(ModMeta mod) {
        var fs = mod.Filesystem;
        var files = fs.FindFilesInDirectoryRecursive("Loenn/lang", "lang")
            .Concat(fs.FindFilesInDirectoryRecursive("lang", "lang"));

        foreach (var file in files) {
            var langName = file.FilenameNoExt()!;
            fs.TryWatchAndOpen(file, stream => {
                LoadFromLangFile(langName, stream.ReadAllText());
            });
        }

        return Task.CompletedTask;
        //await Task.WhenAll(files.SelectToTaskRun(f => LoadFromLangFile(f.FilenameNoExt()!, fs.TryReadAllText(f)!)));
    }

    public static void LoadFromLangFile(string name, string langFileContents) {
        if (string.IsNullOrWhiteSpace(langFileContents))
            return;

        Lang? lang;
        lock (Languages) {
            if (!Languages.TryGetValue(name, out lang)) {
                lang = new(name);
                Languages[name] = lang;
            }
        }

        foreach (var line in langFileContents.Split('\n')) {
            if (line.StartsWith('#'))
                continue;

            if (line.Split('=', count: 2, StringSplitOptions.RemoveEmptyEntries) is not [var key, var value]) {
                continue;
            }

            lock (lang)
                lang.Translations[key] = value.Replace(@"\n", "\n", StringComparison.Ordinal).Trim();
        }
    }
}

public class Lang {
    public string Name { get; set; }

    public Dictionary<string, string> Translations { get; set; } = new(StringComparer.Ordinal);

    public Lang(string name) { 
        Name = name;
    }

    public string? GetOrNull(string key) => Translations.GetValueOrDefault(key);
}
