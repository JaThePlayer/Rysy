using Hexa.NET.ImGui;
using Markdig;
using Markdig.Syntax;
using Rysy.Graphics;
using Rysy.Graphics.TextureTypes;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;
using Rysy.Mods;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml;
using Rysy.Layers;

namespace Rysy.Gui.Windows;

public sealed class AssetDriveTilesetImportWindow : Window {
    private Task<List<AssetDriveTileset>> _bg;
    private Task<List<AssetDriveTileset>> _fg;

    private readonly EditorState _editorState;
    private readonly TileEditorLayer _layer;
    
    private string _searchText = "";
    private ComboCache<AssetDriveTileset> _comboCache = new();

    private AssetDriveTileset? _selected;
    private readonly MarkdownDocument _tip;

    public AssetDriveTilesetImportWindow(EditorState editorState, TileEditorLayer layer) : base("rysy.tilesetImport.fromAssetDrive".Translate(), new(640, 450))
    {
        _bg = MaddieAssetDriveTilesetApi.Bg.GetResourceAsync();
        _fg = MaddieAssetDriveTilesetApi.Fg.GetResourceAsync();
        
        _tip = Markdown.Parse("rysy.tilesetImport.browserTip".Translate(), ImGuiMarkdown.MarkdownPipeline);
        _editorState = editorState;
        _layer = layer;
    }

    private void TabChanged() {
        _comboCache.Clear();
        _selected = null;
        _searchText = "";
    }

    protected override void Render() {
        base.Render();
        
        ImGuiMarkdown.RenderMarkdown(_tip);
        
        RenderTab();
    }

    private void RenderPreview(string id, AssetDriveTileset tileset) {
        var previewTask = Gfx.GetTextureFromWebAsync(tileset.ImageUri, CancellationToken.None);
        if (previewTask.IsCompletedSuccessfully) {
            ImGuiManager.XnaWidgetSprite(id,
                ISprite.FromTexture(default, previewTask.Result) with { Scale = new(2f, 2f) });
        } else if (previewTask.IsCompleted) {
            ImGui.Text("Failed to load preview!");
        } else {
            ImGui.Text("Loading preview...");
        }
    }

    private void RenderTab() {
        var tilesetsTask = _layer.TileLayer is TileLayer.Bg ? _bg : _fg;
        if (!tilesetsTask.IsCompleted) {
            ImGui.Text("Loading...");
            return;
        }
        var tilesets = tilesetsTask.Result;
        _selected ??= tilesets.FirstOrDefault();
        if (_selected is null) {
            ImGui.Text("Failed to load tileset list!");
            return;
        }
        
        ImGuiManager.Combo("Tileset", ref _selected, tilesets, 
            x => new Searchable(x.Name ?? "", mods: [], tags: x.Tags), ref _searchText, tooltip: default, _comboCache,
            renderMenuItem: (tileset, s) => {
                var ret = s.RenderImGuiMenuItem();
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.ForTooltip) && ImGui.BeginTooltip()) {
                    RenderPreview("tileset-import-preview-tooltip", tileset);
                    s.RenderImGuiInfo(_editorState);
                    ImGui.EndTooltip();
                }
                
                return ret;
            });

        ImGui.Columns(2);
        RenderPreview("tileset-import-preview", _selected);
        ImGui.NextColumn();
        
        ImGui.SeparatorText(_selected.Name);
        ImGui.Text($"Author: {_selected.Author}");
        ImGui.Text($"Template: {_selected.TemplateName}");
        Searchable.RenderTagList(_selected.Tags);

        var readme = _selected.Readme;
        ImGui.SeparatorText("Description");
        if (readme.IsCompletedSuccessfully) {
            ImGui.BeginChild("desc");
            ImGui.TextWrapped(readme.Result);
            ImGui.EndChild();
        } else if (readme.IsCompleted) {
            ImGui.Text("Failed to load readme!");
        } else {
            ImGui.Text("Loading readme...");
        }
        
        
        ImGui.Columns();
        
    }

    public override bool HasBottomBar => true;

    public override void RenderBottomBar() {
        var previewTask = _selected is not null ? Gfx.GetTextureFromWebAsync(_selected.ImageUri, CancellationToken.None) : null;
        var valid = _selected is not null && previewTask is { IsCompletedSuccessfully: true };

        using var _ = new ScopedImGuiDisabled(!valid);
        
        if (ImGuiManager.TranslatedButton("rysy.tilesetImport.import") && valid)
        {
            var templates = Registry!.GetRequired<TilesetTemplates>();
            RysyState.Scene.AddWindow(new CreateTilesetWindow(templates, _editorState, new ImportedTileset {
                Name = _selected!.Name,
                Template = templates.FindTemplateByAssetDriveName(_selected.Template) ?? new CustomTilesetTemplate(_selected.Template),
                Layer = _layer,
                CreateTextureClone = true,
                Texture = previewTask!.Result,
            }));
            RemoveSelf();
        }
    }
}

internal sealed partial class CreateTilesetWindow : Window {
    private readonly EditorState? _editorState;
    private readonly ImportedTileset _tileset;

    private char _id;
    private char _copyFromId;

    private readonly TileEditorLayer _layer;

    private bool _wasInvalid;

    private string _displayName;
    
    private string _path;

    [GeneratedRegex(@"\(.*\)")]
    private static partial Regex RedundantInfoRegex();
    
    [GeneratedRegex(@"^<Tileset.*?>(.*?)</Tileset>$", RegexOptions.Singleline)]
    private static partial Regex RedundantTilesetTagRegex();

    private readonly Field _idField;
    private readonly DropdownField<string> _copyFromField;
    private readonly Field _displayNameField;
    private readonly Field _pathField;
    
    public CreateTilesetWindow(TilesetTemplates templates, EditorState? editorState, ImportedTileset tileset) : base("rysy.tilesetImport.createWindow".Translate(), new(620, 400)) {
        _layer = tileset.Layer;
        _editorState = editorState;
        _tileset = tileset;

        _path = _tileset.TexturePath ?? RedundantInfoRegex().Replace(tileset.Name.ToValidFilePath(), "").Trim().TrimPostfix(".png");
        _displayName = _tileset.DefaultDisplayName ?? "";

        if (editorState?.Map is { Mod: { } mod, Filepath: not null }) {
            var map = editorState.Map;

            if (_tileset.TexturePath is null) {
                var path = map.GetDefaultAssetSubdirectory();
                _path = $"{path}/{_path}".ToValidFilePath();
            }

            var autotiler = GetAutotiler(map);
            if (autotiler is null)
                return;
            
            if (tileset.CopyFrom is { } knownCopyFrom) {
                _copyFromId = knownCopyFrom;
            } else if (templates.FindTilesetImplementingTemplate(autotiler, tileset.Template) is { } template) {
                _copyFromId = template.Id;
            } else if (tileset.Template is CustomTilesetTemplate) {
                // The asset has provided a custom xml.
            } else {
                // The asset uses a named template which hasn't been imported into the map yet, import it.
                var newId = autotiler.GetNextFreeTilesetId();
                var templateString = tileset.Template.CreateXmlStringForId(newId);
                if (templateString is not null) {
                    using var memStream = new MemoryStream();
                    memStream.Write(Encoding.UTF8.GetBytes(templateString));
                    memStream.Seek(0, SeekOrigin.Begin);

                    try {
                        using var reader = XmlReader.Create(memStream);
                        var node = autotiler.Xml.ReadNode(reader)!;

                        autotiler.ReadTilesetNode(node, addToXml: true);
                    } catch (Exception ex) {
                        Rysy.Logger.Error(nameof(CreateTilesetWindow), ex, $"Failed to read template {tileset.Template.Name}");
                    }
                    
                    _copyFromId = newId;
                }
            }
        }

        _copyFromField = Fields.TileDropdown(_copyFromId, _layer, addDontCopyOption: true);
        _copyFromField.WithTooltipTranslated("rysy.tilesetImport.copyFromId.tooltip");

        _idField = Fields.Char(_id).WithValidator((x) => {
            if (x is not char c)
                return ValidationResult.MustBeChar;
            var map = editorState?.Map;
            if (map is null)
                return ValidationResult.Ok;
            var autotiler = GetAutotiler(map);

            if (autotiler.IsInvalidTilesetId(c))
                return ValidationResult.InvalidTilesetId;

            if (!autotiler.IsFreeTilesetId(c))
                return ValidationResult.MustBeFreeTilesetId;
            
            return ValidationResult.Ok;
        }).WithTooltipTranslated("rysy.tilesetImport.id.tooltip");

        _displayNameField = Fields.TilesetDisplayName(_displayName, () => _layer, selfIsTileset: false).WithTooltipTranslated("rysy.tilesetImport.displayName.tooltip");
        _pathField = Fields.NewAtlasPath("", "tilesets/").WithTooltipTranslated("rysy.tilesetImport.path.tooltip");
    }

    protected override void Render() {
        if (_editorState?.Map?.Mod is not { } mod) {
            _wasInvalid = true;
            ImGui.Text("Need to be in a packaged mod to import tilesets!");
            return;
        }
        var map = _editorState.Map;
        var autotiler = GetAutotiler(map);
        if (autotiler is null)
            return;
        
        if (_id == 0) {
            _id = autotiler.GetNextFreeTilesetId();
        }

        _wasInvalid = false;
        
        ImGui.Text($"Name: {_tileset.Name}");
        ImGui.Text($"Template Name: {_tileset.TemplateName}");
        ImGui.Text($"Editing xml at: {GetXmlPath(map)}");

        var isValid = _idField.IsValid(_id);
        _id = _idField.RenderGuiWithValidation("rysy.tilesetImport.id".Translate(), _id, isValid) is char c ? c : _id;
        _wasInvalid |= !isValid.IsOk;

        if (_tileset.CopyFrom is null) {
            var idString = _copyFromId.ToString();
            isValid = _copyFromField.IsValid(idString);
            if (_copyFromField.RenderGuiWithValidation("rysy.tilesetImport.copyFromId".Translate(), idString, isValid)
                is string newId) {
                _copyFromId = newId.Length > 0 ? newId[0] : '\0';
            }
            _wasInvalid |= !isValid.IsOk;
        }
        
        isValid = _displayNameField.IsValid(_displayName);
        _displayName = _displayNameField.RenderGuiWithValidation("rysy.tilesetImport.displayName".Translate(), _displayName, isValid) as string ?? _displayName;
        _wasInvalid |= !isValid.IsOk;

        if (_tileset.CreateTextureClone) {
            isValid = _pathField.IsValid(_path);
            _path = _pathField.RenderGuiWithValidation("rysy.tilesetImport.path".Translate(), _path, isValid) as string ?? _path;
            _wasInvalid |= !isValid.IsOk;
            
            var fileTree = FileStructureInfo.FromPath($"Graphics/Atlases/Gameplay/tilesets/{_path}.png");

            ImGui.BeginChild("tileset-import-tree");
            ImGuiManager.RenderFileStructure(fileTree);
            ImGui.EndChild();
        }
        
        base.Render();
    }

    private Autotiler? GetAutotiler(Map map) => _layer.GetAutotiler(map);

    private string? GetXmlPath(Map map) => _layer.TileLayer is TileLayer.Bg ? map.Meta.BackgroundTiles : map.Meta.ForegroundTiles;

    public override bool HasBottomBar => true;

    public override void RenderBottomBar()
    {
        using var _ = new ScopedImGuiDisabled(_wasInvalid || _tileset.Texture.Texture is null);

        if (ImGuiManager.TranslatedButton("rysy.tilesetImport.import")) {
            if (_editorState?.Map?.Mod is not { Filesystem: IWriteableModFilesystem fs } mod) {
                return;
            }

            if (_tileset.CreateTextureClone) {
                fs.TryWriteToFile($"Graphics/Atlases/Gameplay/tilesets/{_path}.png", stream => {
                    var t = _tileset.Texture.Texture!;
                    t.SaveAsPng(stream, t.Width, t.Height);
                });
            }
            
            var map = _editorState.Map;
            var autotiler = GetAutotiler(map);
            if (autotiler is null)
                return;

            var newEl = autotiler.Xml.CreateElement("Tileset");
            newEl.SetAttribute("id", _id.ToString());
            if (_copyFromId != '\0')
                newEl.SetAttribute("copy", _copyFromId.ToString());
            newEl.SetAttribute("path", _path);
            
            if (!string.IsNullOrWhiteSpace(_displayName)) {
                newEl.SetAttribute("displayName", _displayName);
            }

            if (_tileset.Template is CustomTilesetTemplate { Contents: { } template }) {
                if (RedundantTilesetTagRegex().Match(template) is { Success: true } m) {
                    var sourceXml = new XmlDocument();
                    sourceXml.LoadXml(template);
                    var main = sourceXml.DocumentElement!;
                    if (main.Attributes?["sound"]?.Value is {} sound)
                        newEl.SetAttribute("sound", sound);
                    if (main.Attributes?["scanWidth"]?.Value is {} scanWidth)
                        newEl.SetAttribute("scanWidth", scanWidth);
                    if (main.Attributes?["scanHeight"]?.Value is {} scanHeight)
                        newEl.SetAttribute("scanHeight", scanHeight);
                    
                    template = m.Groups[1].Value;
                }

                template = template.Replace("\n<",    "\n    <", StringComparison.Ordinal);
                template = template.Replace("\n <",   "\n    <", StringComparison.Ordinal);
                template = template.Replace("\n  <",  "\n    <", StringComparison.Ordinal);
                template = template.Replace("\n   <", "\n    <", StringComparison.Ordinal);
                if (!template.EndsWith("\n  ", StringComparison.Ordinal))
                    template = template.TrimEnd() + "\n  ";
                
                newEl.InnerXml = template;
            }
            
            
            autotiler.ReadTilesetNode(newEl, clearCache: true, addToXml: true);
            
            map.SaveTilesetXml(_layer);

            RemoveSelf();
        }
    }
}

internal sealed record ImportedTileset {
    public required ITilesetTemplate Template { get; init; }
    
    public required string Name { get; init; }
    
    public required TileEditorLayer Layer { get; init; }
    
    public required VirtTexture Texture { get; init; }
    
    public required bool CreateTextureClone { get; init; }

    public char? CopyFrom { get; init; }

    public string? TexturePath { get; init; }
    
    public string? DefaultDisplayName { get; init; }

    public string TemplateName => Template.Name;
}

internal sealed record AssetDriveTileset {
    [JsonPropertyName("template")]
    public string Template { get; set; } = "";

    [JsonIgnore]
    public string TemplateName =>
        Template.Contains('<', StringComparison.Ordinal) ? "(custom)" : Template;
    
    [JsonPropertyName("folder")]
    public string Folder { get; set; }
    
    [JsonPropertyName("author")]
    public string Author { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("readme")]
    public string? ReadmeId { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    [JsonIgnore]
    public Uri ImageUri => field ??= new($"https://maddie480.ovh/celeste/asset-drive/files/{Id}");


    [JsonIgnore]
    public Task<string> Readme
        => field ??= ReadmeId is {} 
            ? MaddieAssetDriveTilesetApi.Descriptions.GetResource(ReadmeId).GetResourceAsync()
            : Task.FromResult("(No Readme provided)");
}

internal sealed class MaddieAssetDriveTilesetApi(string uri) : StaticWebResource<List<AssetDriveTileset>>(uri) {
    public override Formats Format => Formats.Json;
    
    protected override List<AssetDriveTileset> GetFallback() => [];
    
    public static MaddieAssetDriveTilesetApi Fg { get; } = new("https://maddie480.ovh/celeste/asset-drive/list/fgtilesets");
    public static MaddieAssetDriveTilesetApi Bg { get; } = new("https://maddie480.ovh/celeste/asset-drive/list/bgtilesets");

    public static TextWebResourceRepo Descriptions { get; } = new(CompositeFormat.Parse("https://maddie480.ovh/celeste/asset-drive/files/{0}"));
}