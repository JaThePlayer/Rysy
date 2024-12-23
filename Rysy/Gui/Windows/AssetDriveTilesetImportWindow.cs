﻿using ImGuiNET;
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

namespace Rysy.Gui.Windows;

public class AssetDriveTilesetImportWindow : Window {
    private Task<List<AssetDriveTileset>> _bg;
    private Task<List<AssetDriveTileset>> _fg;

    private bool _isBg;
    
    private string _searchText = "";
    private ComboCache<AssetDriveTileset> _comboCache = new();

    private AssetDriveTileset? _selected;
    private readonly MarkdownDocument _tip;

    public AssetDriveTilesetImportWindow(bool bg) : base("rysy.tilesetImport.fromAssetDrive".Translate(), new(640, 450))
    {
        _bg = MaddieAssetDriveTilesetApi.Bg.GetResourceAsync();
        _fg = MaddieAssetDriveTilesetApi.Fg.GetResourceAsync();
        
        _tip = Markdown.Parse("rysy.tilesetImport.browserTip".Translate(), ImGuiMarkdown.MarkdownPipeline);
        _isBg = bg;
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
        return;

        ImGui.BeginTabBar("Layer");

        bool isBg = _isBg;
        bool isFg = !_isBg;
        
        if (ImGui.BeginTabItem("Fg", ref isFg)) {
            if (_isBg)
                TabChanged();
            _isBg = false;
            RenderTab();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Bg", ref isBg)) {
            if (!_isBg)
                TabChanged();
            _isBg = true;
            RenderTab();
            ImGui.EndTabItem();
        }
        
        ImGui.EndTabBar();
    }

    private void RenderTab() {
        var tilesetsTask = _isBg ? _bg : _fg;
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
        
        ImGuiManager.Combo("Tileset", ref _selected, tilesets, x => x.Name ?? "", ref _searchText, tooltip: null, _comboCache);

        ImGui.Columns(2);
        ImGui.SetColumnWidth(0, 230);
        var previewTask = GFX.GetTextureFromWebAsync(_selected.ImageUri, CancellationToken.None);
        if (previewTask.IsCompletedSuccessfully) {
            ImGuiManager.XnaWidgetSprite("tileset-import-preview",
                ISprite.FromTexture(default, previewTask.Result) with { Scale = new(2f, 2f) });
        } else if (previewTask.IsCompleted) {
            ImGui.Text("Failed to load preview!");
        } else {
            ImGui.Text("Loading preview...");
        }
        ImGui.NextColumn();
        
        ImGui.SeparatorText(_selected.Name);
        ImGui.Text($"Author: {_selected.Author}");
        ImGui.Text($"Template: {_selected.TemplateName}");
        ImGui.Text($"Tags: {string.Join(',', _selected.Tags)}");

        var readme = _selected.Readme;
        ImGui.SeparatorText("Description");
        if (readme.IsCompletedSuccessfully) {
            if (ImGui.BeginChild("desc")) {
                ImGui.TextWrapped(readme.Result);
                ImGui.EndChild();
            }
        } else if (previewTask.IsCompleted) {
            ImGui.Text("Failed to load readme!");
        } else {
            ImGui.Text("Loading readme...");
        }
        
        
        ImGui.Columns();
        
    }

    public override bool HasBottomBar => true;

    public override void RenderBottomBar() {
        var previewTask = _selected is not null ? GFX.GetTextureFromWebAsync(_selected.ImageUri, CancellationToken.None) : null;
        var valid = _selected is not null && previewTask is { IsCompletedSuccessfully: true };
        if (!valid)
            ImGui.BeginDisabled();
        
        if (ImGuiManager.TranslatedButton("rysy.tilesetImport.import") && valid) {
            RysyState.Scene.AddWindow(new CreateTilesetWindow(new() {
                Name = _selected!.Name,
                Template = _selected.Template,
                IsBg = _isBg,
                CreateTextureClone = true,
                Texture = previewTask!.Result,
            }));
        }
        
        ImGui.EndDisabled();
    }
}

internal partial class CreateTilesetWindow : Window {
    private readonly ImportedTileset _tileset;

    private char _id;
    private char _copyFromId;

    private readonly bool _isBg;

    private bool _wasInvalid;

    private string _displayName;
    
    private string _path;

    [GeneratedRegex(@"\(.*\)")]
    private static partial Regex RedundantInfoRegex();
    
    [GeneratedRegex(@"^<Tileset.*?>(.*?)</Tileset>$", RegexOptions.Singleline)]
    private static partial Regex RedundantTilesetTagRegex();

    private readonly DropdownField<string> _copyFromField;
    
    public CreateTilesetWindow(ImportedTileset tileset) : base("rysy.tilesetImport.createWindow".Translate(), new(620, 400)) {
        _isBg = tileset.IsBg;
        _tileset = tileset;

        _path = _tileset.TexturePath ?? RedundantInfoRegex().Replace(tileset.Name.ToValidFilePath(), "").Trim().TrimPostfix(".png");
        _displayName = _tileset.DefaultDisplayName ?? "";

        if (EditorState.Map?.Mod is { } mod && EditorState.Map?.Filepath is {}) {
            var map = EditorState.Map;

            if (_tileset.TexturePath is null) {
                var path = Path.GetDirectoryName(Path.GetRelativePath(Path.Combine(mod.Filesystem.Root, "Maps"), map.Filepath))!.Unbackslash();
                _path = $"{path}/{_path}".ToValidFilePath();
            }

            var autotiler = GetAutotiler(map);
            if (tileset.CopyFrom is { } knownCopyFrom) {
                _copyFromId = knownCopyFrom;
            } else if (autotiler.FindTemplate(tileset.Template) is { } template) {
                _copyFromId = template;
            } else if (tileset.Template.Contains('<', StringComparison.Ordinal)) {
                
            } else {
                var newId = autotiler.GetNextFreeTilesetId();
                var templateString =
                    TilesetTemplates.CreateTemplate(newId, tileset.Template);
                if (templateString is not null) {
                    using var memStream = new MemoryStream();
                    memStream.Write(Encoding.UTF8.GetBytes(templateString));
                    memStream.Seek(0, SeekOrigin.Begin);
                    
                    using var reader = XmlReader.Create(memStream);
                    var node = autotiler.Xml.ReadNode(reader)!;
                    
                    autotiler.ReadTilesetNode(node, addToXml: true);
                    _copyFromId = newId;
                }
            }
        }

        _copyFromField = Fields.TileDropdown(_copyFromId, _isBg, addDontCopyOption: true);
        _copyFromField.Tooltip = "rysy.tilesetImport.copyFromId.tooltip".Translate();
    }

    protected override void Render() {
        if (EditorState.Map?.Mod is not { } mod) {
            _wasInvalid = true;
            ImGui.Text("Need to be in a packaged mod to import tilesets!");
            return;
        }
        var map = EditorState.Map;
        var autotiler = GetAutotiler(map);
        
        if (_id == default) {
            _id = autotiler.GetNextFreeTilesetId();
        }

        _wasInvalid = false;
        
        ImGui.Text($"Name: {_tileset.Name}");
        ImGui.Text($"Template Name: {_tileset.TemplateName}");
        ImGui.Text($"Editing xml at: {GetXmlPath(map)}");

        var idString = _id.ToString();
        _wasInvalid |= ImGuiManager.PushInvalidStyleIf(!autotiler.IsFreeTilesetId(_id));
        if (ImGuiManager.TranslatedInputText("rysy.tilesetImport.id", ref idString, 1)) {
            if (idString.Length > 0) {
                _id = idString[0];
            }
        }
        ImGuiManager.PopInvalidStyle();

        if (_tileset.CopyFrom is null) {
            idString = _copyFromId.ToString();
            _wasInvalid |= ImGuiManager.PushInvalidStyleIf(autotiler.IsFreeTilesetId(_copyFromId));

            if (_copyFromField.RenderGui("rysy.tilesetImport.copyFromId".Translate(), idString) is string newId) {
                _copyFromId = newId.Length > 0 ? newId[0] : '\0';
            }
            ImGuiManager.PopInvalidStyle();
        }
        
        _wasInvalid |= ImGuiManager.PushInvalidStyleIf(autotiler.Tilesets.Any(kv => kv.Value.GetDisplayName() == _displayName));
        ImGuiManager.TranslatedInputText("rysy.tilesetImport.displayName", ref _displayName, 128);
        ImGuiManager.PopInvalidStyle();

        if (_tileset.CreateTextureClone) {
            _wasInvalid |= ImGuiManager.PushInvalidStyleIf(string.IsNullOrWhiteSpace(_path) || GFX.Atlas.TryGet($"tilesets/{_path}", out _));
            var prevPath = _path;
            ImGuiManager.TranslatedInputText("rysy.tilesetImport.path", ref _path, 128);
            if (_path != prevPath) {
                _path = _path.Unbackslash().ToValidFilePath().TrimPostfix(".png");
            }
            ImGuiManager.PopInvalidStyle();
            
            var fileTree = FileStructureInfo.FromPath($"Graphics/Atlases/Gameplay/tilesets/{_path}.png");

            if (ImGui.BeginChild("tileset-import-tree")) {
                ImGuiManager.RenderFileStructure(fileTree);
                ImGui.EndChild();
            }
        }
        
        base.Render();
    }

    private Autotiler GetAutotiler(Map map) => _isBg ? map.BGAutotiler : map.FGAutotiler;

    private string GetXmlPath(Map map) => _isBg ? map.Meta.BackgroundTiles : map.Meta.ForegroundTiles;

    public override bool HasBottomBar => true;

    public override void RenderBottomBar() {
        ImGui.BeginDisabled(_wasInvalid || _tileset.Texture.Texture is null);

        if (ImGuiManager.TranslatedButton("rysy.tilesetImport.import")) {
            if (EditorState.Map?.Mod is not { Filesystem: IWriteableModFilesystem fs } mod) {
                return;
            }

            if (_tileset.CreateTextureClone) {
                fs.TryWriteToFile($"Graphics/Atlases/Gameplay/tilesets/{_path}.png", stream => {
                    var t = _tileset.Texture.Texture!;
                    t.SaveAsPng(stream, t.Width, t.Height);
                });
                var virtPath = $"tilesets/{_path}";
#pragma warning disable CA2000
                GFX.Atlas.AddTexture(virtPath, new ModTexture(mod, virtPath));
#pragma warning restore CA2000

            }
            
            var map = EditorState.Map;
            var autotiler = GetAutotiler(map);

            var newEl = autotiler.Xml.CreateElement("Tileset");
            newEl.SetAttribute("id", _id.ToString());
            if (_copyFromId != '\0')
                newEl.SetAttribute("copy", _copyFromId.ToString());
            newEl.SetAttribute("path", _path);
            
            if (!string.IsNullOrWhiteSpace(_displayName)) {
                newEl.SetAttribute("displayName", _displayName);
            }

            if (_tileset.Template.Contains('<', StringComparison.Ordinal)) {
                var template = _tileset.Template;
                if (RedundantTilesetTagRegex().Match(template) is { Success: true } m) {
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
            
            map.SaveTilesetXml(_isBg);

            RemoveSelf();
        }
        
        ImGui.EndDisabled();
    }
}

internal record ImportedTileset {
    public required string Template { get; init; }
    
    public required string Name { get; init; }
    
    public required bool IsBg { get; init; }
    
    public required VirtTexture Texture { get; init; }
    
    public required bool CreateTextureClone { get; init; }

    public char? CopyFrom { get; init; }

    public string? TexturePath { get; init; }
    
    public string? DefaultDisplayName { get; init; }
    
    public string TemplateName =>
        Template.Contains('<', StringComparison.Ordinal) ? "(custom)" : Template;
}

internal record AssetDriveTileset {
    [JsonPropertyName("template")]
    public string Template { get; set; }

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

    private Uri? _imageUri;

    [JsonIgnore]
    public Uri ImageUri => _imageUri ??= new($"https://maddie480.ovh/celeste/asset-drive/files/{Id}");


    private Task<string>? _readmeTask;
    
    [JsonIgnore]
    public Task<string> Readme
        => _readmeTask ??= ReadmeId is {} 
            ? MaddieAssetDriveTilesetApi.Descriptions.GetResource(ReadmeId).GetResourceAsync()
            : Task.FromResult("(No Readme provided)");
}

internal class MaddieAssetDriveTilesetApi(string uri) : StaticWebResource<List<AssetDriveTileset>>(uri) {
    public override Formats Format => Formats.Json;
    
    protected override List<AssetDriveTileset> GetFallback() => [];
    
    public static MaddieAssetDriveTilesetApi Fg { get; } = new("https://maddie480.ovh/celeste/asset-drive/list/fgtilesets");
    public static MaddieAssetDriveTilesetApi Bg { get; } = new("https://maddie480.ovh/celeste/asset-drive/list/bgtilesets");

    public static TextWebResourceRepo Descriptions { get; } = new(CompositeFormat.Parse("https://maddie480.ovh/celeste/asset-drive/files/{0}"));
}