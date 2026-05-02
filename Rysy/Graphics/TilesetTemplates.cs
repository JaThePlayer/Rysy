using Rysy.Components;
using Rysy.Helpers;
using Rysy.Mods;
using System.Xml.Linq;
using Rysy.Layers;

namespace Rysy.Graphics;

public interface ITilesetTemplate {
    /// <summary>
    /// The internal name of this template.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Name used when displaying this template in UI.
    /// </summary>
    public string DisplayName => Name.TranslateOrNull("rysy.template.name")
                                 ?? Name.TrimPrefix("Rysy/tileset_templates/").TrimPostfix(".xml").Humanize();
    
    /// <summary>
    /// The names of this template on the Community Asset Drive
    /// </summary>
    public IReadOnlySet<string> AssetDriveNames { get; }
    
    /// <summary>
    /// Whether this template is applicable to the given layer.
    /// </summary>
    public bool CanApplyToLayer(TileEditorLayer layer);

    /// <summary>
    /// Creates the XML contents for this template, which should contain a `Tileset` XML element.
    /// </summary>
    /// <param name="tilesetId">The id of the tileset which will define this template.</param>
    public string? CreateXmlStringForId(char tilesetId);

    /// <summary>
    /// Searches for an existing tileset which defines this template, or null if the template is not yet defined.
    /// </summary>
    public TilesetData? FindTilesetDefiningThisTemplate(IEnumerable<TilesetData> tilesets);
}

public sealed class CustomTilesetTemplate(string contents) : ITilesetTemplate
{
    public string Name => "(custom)";

    public IReadOnlySet<string> AssetDriveNames { get; } = (HashSet<string>)[];

    public string Contents { get; set; } = contents;
    
    public bool CanApplyToLayer(TileEditorLayer layer)
    {
        return true;
    }

    public string CreateXmlStringForId(char tilesetId)
    {
        return Contents.Replace("{id}", tilesetId.ToString());
    }

    public TilesetData? FindTilesetDefiningThisTemplate(IEnumerable<TilesetData> tilesets)
    {
        return null;
    }
}

public sealed class XmlFileTilesetTemplate : ITilesetTemplate, IDisposable {
    private IDisposable? _watcher;
    
    public string Name { get; }

    public IReadOnlySet<string> AssetDriveNames { get; private set; } = (HashSet<string>)[];

    private string? TilesetDefinition { get; set; }

    private string? TilesetDefinitionTexturePath { get; set; }

    private TileLayer? TileLayer { get; set; }

    public XmlFileTilesetTemplate(IModFilesystem fs, string path) {
        Name = path;
        fs.TryWatchAndOpen(path, ReadTemplateXml, out _watcher);
    }

    private void ReadTemplateXml(Stream stream) {
        try
        {
            var doc = XDocument.Load(stream);
            var root = doc.Root;
            if (root is null || root.Element("Tileset") is not { } tilesetElement)
            {
                return;
            }

            AssetDriveNames = root.Element("AssetDriveNames")?.Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet() ?? [];
            TilesetDefinition = tilesetElement.ToString();
            TilesetDefinitionTexturePath = tilesetElement.Attribute("path")?.Value ??
                                           throw new Exception("Tileset Template definition texture path is missing");

            TileLayer = root.Element("TileLayer")?.Value is { } tileLayerStr &&
                        Enum.TryParse(tileLayerStr, out TileLayer parsedTileLayer)
                ? parsedTileLayer
                : null;
        }
        catch (Exception ex)
        {
            Logger.Error("XmlFileTilesetTemplate", ex, $"Failed to read tileset template '{Name}'");
            TilesetDefinition = null;
            TilesetDefinitionTexturePath = null;
        }
    }

    public bool CanApplyToLayer(TileEditorLayer layer)
    {
        return TileLayer is null || layer.TileLayer == TileLayer;
    }

    public string? CreateXmlStringForId(char tilesetId) {
        return TilesetDefinition?.Replace("{id}", tilesetId.ToString());
    }

    public TilesetData? FindTilesetDefiningThisTemplate(IEnumerable<TilesetData> tilesets) {
        return tilesets.FirstOrDefault(t => t.Filename == TilesetDefinitionTexturePath);
    }

    public void Dispose() {
        _watcher?.Dispose();
        _watcher = null;
    }
}

public sealed class XmlTilesetTemplateDirectory : IItemProvider<ITilesetTemplate>, IDisposable {
    private readonly IModFilesystem _filesystem;
    private readonly string _path;
    private IDisposable? _fileWatcher;
    
    public Cache<IReadOnlyList<ITilesetTemplate>> ElementCache { get; }

    public XmlTilesetTemplateDirectory(IModFilesystem filesystem, string path) {
        _filesystem = filesystem;
        _path = path;
        ElementCache = new Cache<IReadOnlyList<ITilesetTemplate>>(new CacheToken(), Generator);
        
        _fileWatcher = filesystem.RegisterFilewatch(path, new WatchedAsset {
            OnChanged = FileWatcherCallback,
            OnCreated = FileWatcherCallback,
            OnRemoved = FileWatcherCallback,
        });
    }

    private IReadOnlyList<ITilesetTemplate> Generator() {
        return _filesystem.FindFilesInDirectoryRecursive(_path, "xml")
            .Select(xmlFilePath => new XmlFileTilesetTemplate(_filesystem, xmlFilePath))
            .ToList();
    }

    private void FileWatcherCallback(string path) {
        ElementCache.Token.InvalidateThenReset();
    }

    public void Dispose() {
        _fileWatcher?.Dispose();
        _fileWatcher = null;
    }
}

public sealed class TilesetTemplates : IHasComponentRegistry {
    public static void RegisterDefaultTemplates(IModFilesystem fs, IComponentRegistry registry) {
        registry.AddIfMissing<TilesetTemplates>();
        registry.Add(new XmlTilesetTemplateDirectory(fs, "Rysy/tileset_templates/"));
    }

    public IReadOnlyList<ITilesetTemplate> GetTemplates() {
        return Registry?.GetAllIncludingProvidersCache<ITilesetTemplate>().Value ?? [];
    }

    public TilesetData? FindTilesetImplementingTemplate(Autotiler autotiler, ITilesetTemplate template) {
        if (template.FindTilesetDefiningThisTemplate(autotiler.Tilesets.Select(kv => kv.Value)) is { } tileset)
            return tileset;

        return null;
    }
    
    public ITilesetTemplate? FindTemplateByAssetDriveName(string name) {
        foreach (var template in GetTemplates()) {
            if (template.AssetDriveNames.Contains(name))
                return template;
        }

        return null;
    }

    public IComponentRegistry? Registry { get; set; }
}