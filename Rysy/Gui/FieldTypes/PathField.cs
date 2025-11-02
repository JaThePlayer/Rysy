using Hexa.NET.ImGui;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Graphics.TextureTypes;
using Rysy.Helpers;
using Rysy.Mods;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Rysy.Gui.FieldTypes;

using TextureCacheKey = (string saved, Searchable searchable, FoundPath path);
using TextureCache = Cache<List<(string saved, Searchable searchable, FoundPath path)>>;
using RawTextureCache = Cache<List<FoundPath>>;

public partial record class PathField : Field, IFieldConvertible<string> {
    private static ConditionalWeakTable<object, Dictionary<string, RawTextureCache>> Caches = new();

    private RawTextureCache RawPaths;
    private TextureCache? KnownPaths;

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
            KnownPaths?.Clear();
            KnownPaths = null;
        }
    }

    public override ValidationResult IsValid(object? value) {
        if (value is null && !NullAllowed)
            return ValidationResult.CantBeNull;

        return base.IsValid(value);
    }


    private Func<FoundPath, bool> _Filter = (t) => true;
    public Func<FoundPath, bool> Filter {
        get => _Filter;
        set {
            _Filter = value;
            KnownPaths?.Clear();
            KnownPaths = null;
        }
    }

    private Func<FoundPath, ModMeta?> _modResolver;
    
    public Func<FoundPath, ModMeta?> ModResolver {
        get => _modResolver;
        set {
            _modResolver = value;
            KnownPaths?.Clear();
            KnownPaths = null;
        }
    }

    private readonly Regex _regex;

    private Func<FoundPath, string, string?>? _DisplayNameGetter;

    public Func<FoundPath, string, string?>? DisplayNameGetter {
        get => _DisplayNameGetter;
        set {
            _DisplayNameGetter = value;
            KnownPaths?.Clear();
            KnownPaths = null;
        }
    }
    
    public Func<FoundPath, ISprite?>? PreviewSpriteGetter { get; set; } = static path => ISprite.FromTexture(path.Path);

    private IEnumerable<FoundPath> _additionalEntries = [];
    public IEnumerable<FoundPath> AdditionalEntries {
        get => _additionalEntries;
        set {
            _additionalEntries = value;
            KnownPaths?.Clear();
            KnownPaths = null;
        }
    }

    public PathField WithPreviewSprites(Func<FoundPath, ISprite?> getter) {
        PreviewSpriteGetter = getter;

        return this;
    }

    private void Init(object cacheObject, string regexStr, Func<FoundPath, string>? captureConverter, Func<RawTextureCache> textureFinder, Func<string, ModMeta?> modResolver) {
        ModResolver = (p) => modResolver(p.Path);
        

        if (captureConverter is { })
            _CaptureConverter = captureConverter;

        if (!Caches.TryGetValue(cacheObject, out var cache)) {
            cache = new();
            Caches.TryAdd(cacheObject, cache);
        }

        if (!cache.TryGetValue(regexStr, out var textureCache)) {
            textureCache = textureFinder();
            cache[regexStr] = textureCache;
        }
        RawPaths = textureCache;
    }

    private TextureCacheKey CreateKnownPathsEntry(FoundPath p) {
        var name = CaptureConverter(p);
        var mod = ModResolver(p);

        var displayName = DisplayNameGetter?.Invoke(p, name) ?? name;
        return (name, new Searchable(displayName, mod), p);
    }
    
    private TextureCache CreateKnownPathsCache() {
        return RawPaths.Chain(textures => textures
            .Where(Filter)
            .Concat(AdditionalEntries)
            .Select(CreateKnownPathsEntry)
            .DistinctBy(p => p.saved)
            .ToList());
    }
    
    private static readonly ConcurrentDictionary<string, Regex> _regexCache = new();

    public PathField(string @default, IAtlas atlas, [StringSyntax(StringSyntaxAttribute.Regex)] string regexStr, Func<FoundPath, string>? captureConverter = null) {
        Default = @default;
        _regex = _regexCache.GetOrAdd(regexStr, static regexStr => new Regex(regexStr, RegexOptions.Compiled));

        Init(atlas, regexStr, captureConverter,
            textureFinder: () => atlas.FindTextures(_regex),
            modResolver: (path) => atlas[path] is ModTexture modTexture ? modTexture.Mod : null
        );
    }

    public PathField(string @default, SpriteBank bank, [StringSyntax(StringSyntaxAttribute.Regex)] string regexStr, Func<FoundPath, string>? captureConverter = null) {
        Default = @default;
        _regex = _regexCache.GetOrAdd(regexStr, static regexStr => new Regex(regexStr, RegexOptions.Compiled));
        
        Init(bank, regexStr, captureConverter,
            () => bank.FindTextures(_regex),
            (path) => bank.Get(path)?.Mod
        );
    }

    public PathField(string @default, IModFilesystem filesystem, string directory, string extension, 
        Func<FoundPath, string>? captureConverter = null,
        Func<FoundPath, bool>? filter = null) {
        Default = @default;
        _regex = EmptyRegex();
        PreviewSpriteGetter = null;

        var token = new CacheToken();
        RawTextureCache cache = new(token, () => {
            return filesystem.FindFilesInDirectoryRecursive(directory, extension)
                .Select(p => new FoundPath(p, p.TrimStart(directory).TrimStart('/').TrimEnd($".{extension}", StringComparison.OrdinalIgnoreCase), null))
                .Where(filter ?? (_ => true))
                .ToList();
        });

        // TODO: unregister!
        filesystem.RegisterFilewatch(directory, new() { 
            OnChanged = (f) => token.Invalidate()
        });


        Init(filesystem, directory + extension, captureConverter,
            () => cache,
            (p) => (filesystem is LayeredFilesystem layeredFilesystem) ? layeredFilesystem.FindFirstModContaining(p) : null);
    }

    public override Field CreateClone() {
        return this with { };
    }

    public override object GetDefault() => Default;
    public override void SetDefault(object newDefault) => Default = newDefault.ToString()!;


    private bool RenderMenuItem(TextureCacheKey key, Searchable displayPath) {
        var clicked = ImGui.MenuItem(displayPath.TextWithMods);

        if (ImGui.IsItemHovered(ImGuiHoveredFlags.ForTooltip)) {
            if (PreviewSpriteGetter is not null) {
                var sprite = PreviewSpriteGetter(key.path);
                ImGuiManager.SpriteTooltip("path_field_preview", sprite);
            }

            if (ImGui.BeginTooltip()) {
                displayPath.RenderImGuiInfo();
                ImGui.EndTooltip();
            }
        }
        
        return clicked;
    }

    private ComboCache<TextureCacheKey> _comboCache = new();
    private TextureCacheKey _lastChosen;
    
    public override object? RenderGui(string fieldName, object value) {
        var strValue = value?.ToString() ?? "";

        KnownPaths ??= CreateKnownPathsCache();

        var paths = KnownPaths.Value;
        Func<TextureCacheKey, Searchable, bool>? menuItemRenderer = PreviewSpriteGetter is { } 
            ? RenderMenuItem
            : null;

        TextureCacheKey chosen;
        if (strValue == _lastChosen.saved)
            chosen = _lastChosen;
        else
            chosen = paths.Find(p => p.saved == strValue);
        
        if (chosen.saved == default!)
            chosen = CreateKnownPathsEntry(FoundPath.Create(strValue, _regex) ?? new FoundPath(strValue, strValue, null));

        _lastChosen = chosen;
        
        if (Editable) {
            if (ImGuiManager.EditableCombo(fieldName, ref chosen, paths, x => x.searchable, 
                    str => CreateKnownPathsEntry(FoundPath.CreateMaybeInvalid(str, _regex)), tooltip: Tooltip,
                    search: ref Search, cache: _comboCache, renderMenuItem: menuItemRenderer, textInputStringGetter: x => x.saved)) {
                return chosen.saved;
            }
        } else {
            if (ImGuiManager.Combo(fieldName, ref chosen, paths, x => x.searchable, tooltip: Tooltip,
                    search: ref Search, cache: _comboCache, renderMenuItem: menuItemRenderer)) {
                return chosen.saved;
            }
        }

        return null;
    }

    /// <summary>
    /// Clears all the caches inside of this path field.
    /// </summary>
    public void ClearCache() {
        KnownPaths?.Clear();
        KnownPaths = null;
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
    
    
    [GeneratedRegex(".*", RegexOptions.Compiled)]
    private static partial Regex EmptyRegex();
}
