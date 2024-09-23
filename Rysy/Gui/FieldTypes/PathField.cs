using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Graphics.TextureTypes;
using Rysy.Helpers;
using Rysy.Mods;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Rysy.Gui.FieldTypes;

using TextureCache = Cache<Dictionary<string, string>>;
using RawTextureCache = Cache<List<FoundPath>>;

public record class PathField : Field, IFieldConvertible<string> {
    private static ConditionalWeakTable<object, Dictionary<string, RawTextureCache>> Caches = new();

    private RawTextureCache RawPaths;
    private TextureCache KnownPaths;

    private ComboCache<string> ComboCache = new();

    public bool NullAllowed = false;

    public string Default { get; set; }

    public bool Editable { get; set; }

    private string Search = "";

    private Func<FoundPath, string> _CaptureConverter = (s) => s.Captured;
    public Func<FoundPath, string> CaptureConverter {
        get => _CaptureConverter;
        set {
            _CaptureConverter = value;
            KnownPaths.Clear();
            CreateKnownPathsCache();
        }
    }

    public override bool IsValid(object? value) {
        if (value is null && !NullAllowed)
            return false;

        return true;
    }


    private Func<FoundPath, bool> _Filter = (t) => true;
    public Func<FoundPath, bool> Filter {
        get => _Filter;
        set {
            _Filter = value;
            KnownPaths.Clear();
            CreateKnownPathsCache();
        }
    }

    private Func<string, ModMeta?> ModResolver;

    private void Init(object cacheObject, string regexStr, Func<FoundPath, string>? captureConverter, Func<RawTextureCache> textureFinder, Func<string, ModMeta?> modResolver) {
        ModResolver = modResolver;
        

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
        RawPaths = textureCache;

        CreateKnownPathsCache();
    }

    private void CreateKnownPathsCache() {
        KnownPaths = RawPaths.Chain(textures => textures.Where(Filter).SafeToDictionary(p => {
            var name = CaptureConverter(p);
            var mod = ModResolver(p.Path);
            return (name, mod is { } ? $"{name} [{mod.DisplayName}]" : name);
        }));
    }

    public PathField(string @default, IAtlas atlas, [StringSyntax(StringSyntaxAttribute.Regex)] string regexStr, Func<FoundPath, string>? captureConverter = null) {
        Default = @default;

        Regex? regex = null;
        Init(atlas, regexStr, captureConverter,
            textureFinder: () => atlas.FindTextures(regex ??= new Regex(regexStr, RegexOptions.Compiled)),
            modResolver: (path) => atlas[path] is ModTexture modTexture ? modTexture.Mod : null
        );
    }

    public PathField(string @default, SpriteBank bank, [StringSyntax(StringSyntaxAttribute.Regex)] string regexStr, Func<FoundPath, string>? captureConverter = null) {
        Default = @default;

        Regex? regex = null;
        Init(bank, regexStr, captureConverter,
            () => bank.FindTextures(regex ??= new Regex(regexStr, RegexOptions.Compiled)),
            (path) => bank.Get(path)?.Mod
        );
    }

    public PathField(string @default, IModFilesystem filesystem, string directory, string extension, Func<FoundPath, string>? captureConverter = null) {
        Default = @default;

        var token = new CacheToken();
        RawTextureCache cache = new(token, () => {
            return filesystem.FindFilesInDirectoryRecursive(directory, extension).Select(p => new FoundPath(p, p.TrimStart(directory).TrimStart('/').TrimEnd($".{extension}"))).ToList();
        });

        // TODO: unregister!
        filesystem.RegisterFilewatch(directory, new() { 
            OnChanged = (f) => token.Invalidate()
        });


        Init(filesystem, directory + extension, captureConverter,
            () => cache,
            (p) => ModRegistry.Filesystem.FindFirstModContaining(p));
    }

    public override Field CreateClone() {
        return this with { };
    }

    public override object GetDefault() => Default;
    public override void SetDefault(object newDefault) => Default = newDefault.ToString()!;

    public override object? RenderGui(string fieldName, object value) {
        var strValue = value?.ToString() ?? "";

        var paths = KnownPaths.Value;

        if (Editable) {
            return ImGuiManager.EditableCombo(fieldName, ref strValue, paths, (s) => s, ref Search, Tooltip, ComboCache) ? strValue : null;
        } else {
            return ImGuiManager.Combo(fieldName, ref strValue, paths, ref Search, Tooltip, ComboCache) ? strValue : null;
        }
    }

    /// <summary>
    /// Clears all the caches inside of this path field.
    /// </summary>
    public void ClearCache() {
        KnownPaths.Clear();
        RawPaths.Clear();
        ComboCache.Clear();
    }

    /// <summary>
    /// Allows or disallows the field's value to be edited beyond the values from the dropdown.
    /// </summary>
    /// <returns>this</returns>
    public PathField AllowEdits(bool editable = true) {
        Editable = editable;

        return this;
    }

    public PathField WithConverter(Func<FoundPath, string> captureConverter) {
        CaptureConverter = captureConverter;

        return this;
    }

    public PathField WithFilter(Func<FoundPath, bool> filter) {
        Filter = filter;

        return this;
    }

    public PathField AllowNull() {
        NullAllowed = true;

        return this;
    }

    public string ConvertMapDataValue(object value) => value?.ToString() ?? "";
}
