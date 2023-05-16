using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Graphics.TextureTypes;
using Rysy.Helpers;
using Rysy.Mods;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Rysy.Gui.FieldTypes;

using TextureCache = Lazy<Cache<Dictionary<string, string>>>;
using RawTextureCache = Cache<List<FoundTexture>>;

public record class PathField : Field {
    private static ConditionalWeakTable<object, Dictionary<string, RawTextureCache>> Caches = new();

    private TextureCache KnownPaths;

    public string Default { get; set; }

    public bool Editable { get; set; }

    private string Search = "";

    private Func<FoundTexture, string> _CaptureConverter = (s) => s.Captured;
    public Func<FoundTexture, string> CaptureConverter {
        get => _CaptureConverter;
        set {
            _CaptureConverter = value;
            if (KnownPaths.IsValueCreated) {
                KnownPaths.Value.Clear();
            }
        }
    }


    private Func<FoundTexture, bool> _Filter = (t) => true;
    public Func<FoundTexture, bool> Filter {
        get => _Filter;
        set {
            _Filter = value;
            if (KnownPaths.IsValueCreated) {
                KnownPaths.Value.Clear();
            }
        }
    }

    private void Init(object cacheObject, string regexStr, Func<FoundTexture, string>? captureConverter, Func<RawTextureCache> textureFinder, Func<string, ModMeta?> modResolver) {
        if (captureConverter is { })
            CaptureConverter = captureConverter;

        if (!Caches.TryGetValue(cacheObject, out var cache)) {
            cache = new();
            Caches.TryAdd(cacheObject, cache);
        }

        if (!cache.TryGetValue(regexStr, out var textureCache)) {
            textureCache = textureFinder();
            cache[regexStr] = textureCache;
        }

        KnownPaths = new(() => {
            return textureCache.Chain(textures => textures.Where(Filter).SafeToDictionary(p => {
                var name = CaptureConverter(p);
                var mod = modResolver(p.Path);
                return (name, mod is { } ? $"{name} [{mod.Name}]" : name);
            }));
        });
    }

    public PathField(string @default, IAtlas atlas, [StringSyntax(StringSyntaxAttribute.Regex)] string regexStr, Func<FoundTexture, string>? captureConverter = null) {
        Default = @default;

        var regex = new Regex(regexStr, RegexOptions.Compiled);
        Init(atlas, regexStr, captureConverter,
            textureFinder: () => atlas.FindTextures(regex),
            modResolver: (path) => atlas[path] is ModTexture modTexture ? modTexture.Mod : null
        );
    }

    public PathField(string @default, SpriteBank bank, [StringSyntax(StringSyntaxAttribute.Regex)] string regexStr, Func<FoundTexture, string>? captureConverter = null) {
        Default = @default;

        var regex = new Regex(regexStr, RegexOptions.Compiled);
        Init(bank, regexStr, captureConverter,
            () => bank.FindTextures(regex),
            (path) => bank.Get(path)?.Mod
        );
    }

    public override Field CreateClone() {
        return this with { };
    }

    public override object GetDefault() => Default;
    public override void SetDefault(object newDefault) => Default = newDefault.ToString()!;

    public override object? RenderGui(string fieldName, object value) {
        var strValue = value.ToString() ?? "";

        var paths = KnownPaths.Value.Value;

        if (Editable) {
            return ImGuiManager.EditableCombo(fieldName, ref strValue, paths, (s) => s, ref Search, Tooltip) ? strValue : null;
        } else {
            return ImGuiManager.Combo(fieldName, ref strValue, paths, ref Search, Tooltip) ? strValue : null;
        }
    }

    /// <summary>
    /// Allows or disallows the field's value to be edited beyond the values from the dropdown.
    /// </summary>
    /// <returns>this</returns>
    public PathField AllowEdits(bool editable = true) {
        Editable = editable;

        return this;
    }

    public PathField WithConverter(Func<FoundTexture, string> captureConverter) {
        CaptureConverter = captureConverter;

        return this;
    }

    public PathField WithFilter(Func<FoundTexture, bool> filter) {
        Filter = filter;

        return this;
    }
}
