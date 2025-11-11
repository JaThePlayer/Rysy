using Hexa.NET.ImGui;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Graphics.TextureTypes;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Mods;
using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;

namespace Rysy.Gui.Windows;

public sealed class TilesetWindow : Window {
    internal enum Tabs {
        Bg,
        Fg,
        AnimatedTiles
    }
    
    private Tabs _tab = Tabs.Fg;
    
    private Map? Map => EditorState.Map;

    internal ModMeta? Mod => Map?.Mod;
    
    bool _firstRender = true;

    private FormWindow? _form;

    private char _formPropId = '\0';
    private string? _formPropAnimName;

    private IXmlBackedEntityData? FormProp {
        get {
            if (_tab is Tabs.Fg or Tabs.Bg) {
                return (Bg ? Map!.BgAutotiler : Map!.FgAutotiler).Tilesets.GetValueOrDefault(_formPropId);
            } else {
                return _formPropAnimName is not null ? Map!.AnimatedTiles.Tiles.GetValueOrDefault(_formPropAnimName) : null;
            }
        }
        set {
            if (value is TilesetData tilesetData) {
                _formPropId = tilesetData.Id;
                _formPropAnimName = null;
            } else if (value is AnimatedTileData animatedTileData) {
                _formPropId = '\0';
                _formPropAnimName = animatedTileData.Name;
            } else {
                _formPropId = '\0';
                _formPropAnimName = null;
            }
        }
    }

    private readonly HotkeyHandler _hotkeyHandler;
    private HistoryHandler? _history;

    private bool Bg
        => _tab == Tabs.Bg;

    public TilesetWindow() : base("rysy.tilesetWindow.name".Translate(), new(1200, 800)) {
        _hotkeyHandler = new(Input.Global, HotkeyHandler.ImGuiModes.Ignore);
        _hotkeyHandler.AddHotkeyFromSettings("delete", "delete", DeleteSelections);
        
       // HotkeyHandler.AddHotkeyFromSettings("stylegrounds.moveUp", "up", () => MoveSelections(-1), HotkeyModes.OnHoldSmoothInterval);
       // HotkeyHandler.AddHotkeyFromSettings("stylegrounds.moveDown", "down", () => MoveSelections(1), HotkeyModes.OnHoldSmoothInterval);
       // HotkeyHandler.AddHotkeyFromSettings("copy", "ctrl+c", CopySelections);
       // HotkeyHandler.AddHotkeyFromSettings("paste", "ctrl+v", PasteSelections);
       // HotkeyHandler.AddHotkeyFromSettings("cut", "ctrl+x", CutSelections);
       
       _hotkeyHandler.AddHistoryHotkeys(() => _history?.Undo(), () => _history?.Redo(), Save);
    }

    private void HistoryHook() {
        var prop = FormProp;
        if (prop is { }) {
            _form?.ReevaluateChanged(prop.FakeData.Inner);
        }
        Save();
    }

    private void SetSelection(object? entry) {
        CreateForm(entry);
    }

    private void DeleteSelections()
        => ForAllSelections((e) => {
            switch (e) {
                case TilesetData t:
                    RemoveEntry(t.Id, Bg);
                    break;
                case AnimatedTileData a:
                    RemoveEntry(a);
                    break;
            }
        });

    private FieldList GetFields(TilesetData main) {
        FieldList fieldInfo = main.GetFields(Bg);

        var fields = new FieldList();
        var order = new List<string>();

        foreach (var (k, f) in fieldInfo.OrderedEnumerable(main)) {
            fields[k] = f.CreateClone();
            order.Add(k);
        }

        // Take into account properties defined on this style, even if not present in FieldInfo
        foreach (var (k, v) in main.FakeData) {
            if (k is "id")
                continue;
                
            if (fields.TryGetValue(k, out var knownFieldType)) {
                fields[k].SetDefault(v);
            } else {
                fields[k] = Fields.GuessFromValue(v, fromMapData: true)!;
                order.Add(k);
            }
        }
        
        fields.AddTranslations("rysy.tilesetWindow.description", "rysy.tilesetWindow.attribute");

        return fields.Ordered(order);
    }

    private void CreateForm(object? propObj) {
        if (propObj is null) {
            CleanupPreviousForm();
            _form = null;
            FormProp = null;
            return;
        }

        if (propObj is TilesetData prop) {
            var fields = GetFields(prop);

            var form = new FormWindow(fields, prop.Filename);
            form.Exists = prop.FakeData.Has;
            form.OnChanged = (edited) => {
                FormProp?.FakeData.SetOverlay(null);
                _history?.ApplyNewAction(new TilesetChangedAction(_formPropId, Bg, new(edited)));

                Save();
            };
            form.OnLiveUpdate = (edited) => {
                Dictionary<string, object> editCopy = new(edited);
                FormProp?.FakeData.SetOverlay(editCopy);
            
                FormProp?.UpdateData(FormProp.FakeData!);
            };

            CleanupPreviousForm();
        
            FormProp = prop;
            _form = form;
        }
        
        else if (propObj is AnimatedTileData propAnim) {
            var fields = propAnim.GetFields();

            var form = new FormWindow(fields, propAnim.Name);
            form.Exists = propAnim.FakeData.Has;
            form.OnChanged = (edited) => {
                FormProp?.FakeData.SetOverlay(null);
                _history?.ApplyNewAction(new AnimatedTileChangedAction(propAnim.Name, new(edited)));

                Save();
            };
            form.OnLiveUpdate = (edited) => {
                Dictionary<string, object> editCopy = new(edited);
                FormProp?.FakeData.SetOverlay(editCopy);
            
                FormProp?.UpdateData(FormProp.FakeData!);
            };

            CleanupPreviousForm();
        
            FormProp = propAnim;
            _form = form;
        }

        return;
        
        void CleanupPreviousForm()
        {
            if (FormProp is { } prev) {
                var overlay = prev.FakeData.FakeOverlay;
                prev.FakeData.SetOverlay(null);
                prev.UpdateData(prev.FakeData!);
            
                if (overlay is { }) {
                    var removalData = new Dictionary<string, object?>();
                    foreach (var (ok, ov) in overlay) {
                        if (!prev.FakeData.Has(ok)) {
                            removalData.Add(ok, null);
                        }
                    }

                    if (removalData.Count > 0) {
                        prev.UpdateData(removalData);
                    }
                }
            }
        }
    }

    private void Save() {
        switch (_tab) {
            case Tabs.Fg or Tabs.Bg:
                Map?.SaveTilesetXml(Bg);
                break;
            case Tabs.AnimatedTiles:
                Map?.SaveAnimatedTilesXml();
                break;
        }
    }

    private void ChangeTab(Tabs newTab) {
        if (_tab != newTab) {
            _tab = newTab;
            FormProp = null;
            SetSelection(null);
        }
    }
    
    protected override void Render() {
        if (Map is not { Mod: not null }) {
            ImGuiManager.TranslatedText("rysy.tilesetWindow.needToBeInMap");
            return;
        }

        if (_history?.Map != Map) {
            _history = null;
        }
        
        _history ??= new HistoryHandler(Map) {
            OnApply = HistoryHook,
            OnUndo = HistoryHook
        };
        
        base.Render();
        
        if (ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows) && !ImGui.GetIO().WantCaptureKeyboard) {
            _hotkeyHandler.Update();
        }

        var size = ImGui.GetContentRegionAvail();

        ImGui.Columns(2);

        if (_firstRender)
            ImGui.SetColumnWidth(0, size.X / 3);

        if (ImGui.BeginTabBar("layer")) {
            if (ImGui.BeginTabItem("rysy.tilesetWindow.tabs.fg".Translate())) {
                ChangeTab(Tabs.Fg);
                RenderList();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("rysy.tilesetWindow.tabs.bg".Translate())) {
                ChangeTab(Tabs.Bg);
                RenderList();
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("rysy.tilesetWindow.tabs.anim".Translate())) {
                ChangeTab(Tabs.AnimatedTiles);
                RenderList();
                ImGui.EndTabItem();
            }
            
            ImGui.EndTabBar();
        }
        

        ImGui.NextColumn();

        var previewW = (int) ImGui.GetContentRegionAvail().X;
        var cam = new Camera(RysyState.GraphicsDevice.Viewport);
        cam.Scale = _tab is Tabs.AnimatedTiles ? 4f : 2f;
        cam.Move(-new Vector2(previewW / 2f / cam.Scale, 300 / 2f / cam.Scale));

        ImGuiManager.XnaWidget("tileset_preview", previewW, 300, () => {
            var ctx = SpriteRenderCtx.Default(true);
                
            IEnumerable<ISprite>? sprites;
            if (_form is {} && FormProp is TilesetData prop) {
                sprites = ISprite.FromTexture(prop.Texture).Centered();
            } else if (FormProp is AnimatedTileData animProp) {
                animProp.RenderAt(ctx, Gfx.Batch, default, Color.White);
                sprites = Map.FgAutotiler.GetSprites(default, new[,] { { 'z' } }, Color.White, tilesOob: false);
            } else {
                sprites = [];
            }
                
            foreach (var item in sprites) {
                item.Render(ctx);
            }
        }, cam, rerender: true);

        if (_form is { }) {
            ImGui.BeginChild("form");
            _form?.RenderBody(() => {
                if (FormProp is TilesetData { Xml: {} } formProp) {
                    if (ImGuiManager.TranslatedButton("rysy.tilesetWindow.editXml")) {
                        var bg = Bg;
                        
                        var sw = new StringWriter(CultureInfo.InvariantCulture);
                        var xmlWriter = XmlWriter.Create(sw, new XmlWriterSettings {
                            Indent = true, 
                            ConformanceLevel = ConformanceLevel.Fragment,
                        });
                        formProp.Xml.WriteContentTo(xmlWriter);
                        xmlWriter.Close();
                        var xml = sw.ToString();

                        var tab = _tab;
                        var editedTileId = _formPropId;
                        
                        RysyState.Scene.AddWindow(new ScriptedWindow("rysy.tilesetWindow.editXml".Translate(), (w) => {
                            var size = ImGui.GetContentRegionAvail();
                            size.Y -= ImGui.GetTextLineHeightWithSpacing();
                            ImGui.InputTextMultiline("##", ref xml, 8192, size);
                        }, new(700, 700), (w) => {
                            if (ImGuiManager.TranslatedButton("rysy.save")) {
                                if (GetAutotiler(bg).GetTilesetData(editedTileId) is { Xml: {} } tileset) {
                                    tileset.Xml.InnerXml = xml;
                                    w.RemoveSelf();
                                }
                            }
                        }));
                    }
                }
            });
            ImGui.EndChild();
        }
        ImGui.Columns();

        _firstRender = false;
    }

    private void ForAllSelections(Action<object>? onEntry) {
        if (FormProp is not { } entry)
            return;
        
        onEntry?.Invoke(entry);
    }

    private bool CanRemove(char id) {
        var autotiler = GetAutotiler(Bg);

        return autotiler.Tilesets.All(x => x.Value.CopyFrom != id);
    }

    private void RemoveEntry(AnimatedTileData tile) {
        if (FormProp == tile)
            SetSelection(null);
        _history?.ApplyNewAction(new RemoveAnimatedTileAction(tile.Name));
    }
    
    private void RemoveEntry(char entryId, bool bg) {
        var autotiler = GetAutotiler(Bg);

        if (!CanRemove(entryId))
            return;
        
        if (autotiler?.GetTilesetData(entryId) is { Texture: ModTexture { Mod: { } textureMod } } tilesetData
            && Mod == textureMod && textureMod.Filesystem is IWriteableModFilesystem
            && autotiler.Tilesets.Count(x => x.Value.Filename == tilesetData.Filename) == 1) {
            RysyState.Scene.AddWindow(new ScriptedWindow("rysy.tilesetWindow.removeSourceTexture.name".Translate(),
                (w) => {
                    ImGuiManager.TranslatedTextWrapped("rysy.tilesetWindow.removeSourceTexture");
                }, new(400, ImGui.GetTextLineHeightWithSpacing() * 4 + ImGui.GetFrameHeightWithSpacing() * 2), bottomBarFunc: (w) => {
                    if (ImGuiManager.TranslatedButton("rysy.ok")) {
                        _history?.ApplyNewAction(new RemoveTilesetAction(entryId, Bg, removeSourceTexture: true));
                        if (_formPropId == entryId)
                            SetSelection(null);
                        w.RemoveSelf();
                    }
                    
                    ImGui.SameLine();
                    if (ImGuiManager.TranslatedButton("rysy.no")) {
                        _history?.ApplyNewAction(new RemoveTilesetAction(entryId, Bg, removeSourceTexture: false));
                        if (_formPropId == entryId)
                            SetSelection(null);
                        w.RemoveSelf();
                    }
                    
                    ImGui.SameLine();
                    if (ImGuiManager.TranslatedButton("rysy.cancel")) {
                        w.RemoveSelf();
                    }
                }));
        } else {
            _history?.ApplyNewAction(new RemoveTilesetAction(entryId, Bg, removeSourceTexture: false));
            if (_formPropId == entryId)
                SetSelection(null);
        }
    }

    private void RenderEntry(object entry) {
        switch (entry) {
            case TilesetData tileset:
                RenderEntry(tileset);
                break;
            case AnimatedTileData animatedTileData:
                RenderEntry(animatedTileData);
                break;
        }
    }
    
    private void RenderEntry(TilesetData entry) {
        var id = ImGuiManager.PerFrameInterpolator.Utf8($"[{entry.Id.ToImguiEscapedString()}] {entry.GetDisplayName()}");

        ImGui.TableNextRow();

        ImGui.TableNextColumn();

        var flags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanFullWidth;
        if (FormProp == entry) {
            flags |= ImGuiTreeNodeFlags.Selected;
        }

        var open = ImGui.TreeNodeEx(Interpolator.TempU8($"##{id}"), flags);
        var clicked = ImGui.IsItemClicked();
        var tileid = entry.Id;
        var bg = Bg;
        var sid = ImGuiManager.PerFrameInterpolator.Utf8($"d_ctx_{id}");
        ImGui.OpenPopupOnItemClick(sid, ImGuiPopupFlags.MouseButtonRight);
        if (ImGui.BeginPopupContextWindow(sid, ImGuiPopupFlags.NoOpenOverExistingPopup | ImGuiPopupFlags.MouseButtonRight)) {
            var autotiler = GetAutotiler(Bg);
            var tileset = autotiler.GetTilesetData(tileid);
            if (tileset is { Xml: not null } && ImGuiManager.TranslatedButton("rysy.tilesetWindow.clone")) {
                RysyState.Scene.AddWindow(new CreateTilesetWindow(new ImportedTileset {
                    Texture = tileset.Texture,
                    CreateTextureClone = false,
                    IsBg = Bg,
                    Name = tileset.Filename,
                    Template = tileset.Xml.InnerXml,
                    CopyFrom = tileset.CopyFrom,
                    TexturePath = tileset.Filename,
                    DefaultDisplayName = $"{tileset.GetDisplayName()} (copy)"
                }));
            }
            
            if (CanRemove(tileid) && ImGuiManager.TranslatedButton("rysy.delete")) {
                RemoveEntry(tileid, bg);
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }

        ImGui.SameLine();
        ImGui.BeginDisabled(entry.IsTemplate);
        ImGui.Text(id);
        ImGui.EndDisabled();

        if (clicked) {
            SetSelection(entry);
        }
    }

    private void RenderEntry(AnimatedTileData entry) {
        var id = $"{entry.Name}";

        ImGui.TableNextRow();

        ImGui.TableNextColumn();

        var flags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanFullWidth;
        if (FormProp == entry) {
            flags |= ImGuiTreeNodeFlags.Selected;
        }

        var open = ImGui.TreeNodeEx(Interpolator.TempU8($"##{id}"), flags);
        var clicked = ImGui.IsItemClicked();
        var tileid = entry.Name;
        var sid = $"d_ctx_{id}";
        ImGui.OpenPopupOnItemClick(sid, ImGuiPopupFlags.MouseButtonRight);
        if (ImGui.BeginPopupContextWindow(sid, ImGuiPopupFlags.NoOpenOverExistingPopup | ImGuiPopupFlags.MouseButtonRight)) {
            if (ImGuiManager.TranslatedButton("rysy.delete")) {
                RemoveEntry(entry);
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }

        ImGui.SameLine();
        ImGui.Text(id);

        if (clicked) {
            SetSelection(entry);
        }
    }
    
    public void RenderList() {
        if (Map is null)
            return;

        var xmlPath = _tab switch {
            Tabs.Bg => Map.Meta.BackgroundTiles,
            Tabs.Fg => Map.Meta.ForegroundTiles,
            Tabs.AnimatedTiles => Map.Meta.AnimatedTiles,
        };
        
        if (string.IsNullOrWhiteSpace(xmlPath)) {
            ImGuiManager.TranslatedTextWrapped("rysy.tilesetWindow.xmlCantBeEdited.notSet");
            if (ImGuiManager.TranslatedButton("rysy.tilesetWindow.xmlCantBeEdited.createNew")) {
                RysyState.Scene.AddWindow(new CreateDefaultXmlWindow(_tab));
            }
            
            return; 
        }
        
        if (ModRegistry.Filesystem.FindFirstModContaining(xmlPath)?.Filesystem is not IWriteableModFilesystem) {
            ImGuiManager.TranslatedTextWrapped("rysy.tilesetWindow.xmlCantBeEdited.notWriteable");
            return;
        }

        ImGui.BeginChild("list", new NumVector2(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y));
        
        var entries = _tab switch {
            Tabs.Bg or Tabs.Fg => GetAutotiler(Bg).Tilesets.Values.AsEnumerable<object>(),
            Tabs.AnimatedTiles => Map.AnimatedTiles.Tiles.Values.Where(v => v.Xml is {}).AsEnumerable<object>(),
            _ => throw new UnreachableException()
        };

        var flags = ImGuiManager.TableFlags;

        if (!ImGui.BeginTable("Tilesets", 1, flags)) {
            ImGui.EndChild();
            return;
        }

        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        foreach (var entry in entries) {
            RenderEntry(entry);
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        
        ImGuiManager.PushNullStyle();
        ImGui.TreeNodeEx("rysy.new".Translate(), ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanFullWidth);
        ImGuiManager.PopNullStyle();
        
        var id = $"new_tileset";
        ImGui.OpenPopupOnItemClick(id, ImGuiPopupFlags.MouseButtonLeft);

        if (ImGui.BeginPopupContextWindow(id, ImGuiPopupFlags.NoOpenOverExistingPopup | ImGuiPopupFlags.MouseButtonRight)) {
            if (_tab is Tabs.Bg or Tabs.Fg) {
                if (ImGuiManager.TranslatedButton("rysy.tilesetImport.fromAssetDrive")) {
                    RysyState.Scene.AddWindowIfNeeded(() => new AssetDriveTilesetImportWindow(Bg));
                    ImGui.CloseCurrentPopup();
                }
            
                if (ImGuiManager.TranslatedButton("rysy.tilesetImport.fromExistingSprite")) {
                    RysyState.Scene.AddWindowIfNeeded(() => new ExistingSpriteTilesetImportWindow(Bg));
                    ImGui.CloseCurrentPopup();
                }
            } else if (_tab is Tabs.AnimatedTiles) {
                if (ImGuiManager.TranslatedButton("rysy.animTileImport.new")) {
                    RysyState.Scene.AddWindowIfNeeded(() => new ExistingSpriteAnimatedTileImportWindow(_history!));
                    ImGui.CloseCurrentPopup();
                }
                if (ImGuiManager.TranslatedButton("rysy.animTileImport.importFromXml")) {
                    RysyState.Scene.AddWindowIfNeeded(() => new XmlSnippetAnimatedTileImportWindow(_history!));
                    ImGui.CloseCurrentPopup();
                }
            }

            ImGui.EndPopup();
        }

        ImGui.EndTable();
        ImGui.EndChild();
    }

    private Autotiler GetAutotiler(bool bg)
    {
        return bg ? Map.BgAutotiler : Map.FgAutotiler;
    }

    private void AddContextWindow(string id, Action? render = null) {
        var sid = $"d_ctx_{id}";
        ImGui.OpenPopupOnItemClick(sid, ImGuiPopupFlags.MouseButtonRight);

        if (ImGui.BeginPopupContextWindow(sid, ImGuiPopupFlags.NoOpenOverExistingPopup | ImGuiPopupFlags.MouseButtonRight)) {
            render?.Invoke();

            ImGui.EndPopup();
        }
    }
}

internal sealed class ExistingSpriteAnimatedTileImportWindow : Window {
    private readonly Field _nameField;
    private readonly HistoryHandler _history;
    
    private string _name = "";
    private bool _wasInvalid = false;
    
    public ExistingSpriteAnimatedTileImportWindow(HistoryHandler history) : base("rysy.animTileImport.new.windowName".Translate(),
        new(400, ImGui.GetTextLineHeightWithSpacing() * 6)) {
        _history = history;

        _nameField = Fields.String("")
            .WithValidator(x => {
                var tiles = EditorState.Map?.AnimatedTiles;
                if (tiles is null)
                    return ValidationResult.Ok;
                
                if (string.IsNullOrWhiteSpace(x))
                    return ValidationResult.CantBeNull;
                
                if (x.AsSpan().ContainsAny("\"&<>"))
                    return ValidationResult.GenericError;

                if (tiles.Has(x))
                    return ValidationResult.AnimTileNameNotUnique;
                
                return ValidationResult.Ok;
            })
            .Translated("rysy.animTileImport.name");
    }

    protected override void Render() {
        _wasInvalid = false;
        
        if (_nameField.RenderGuiWithValidation(_name, out var isValid) is {} newVal) {
            _name = newVal.ToString() ?? "";
        }
        _wasInvalid |= !isValid.IsOk;
    }

    public override bool HasBottomBar => true;

    public override void RenderBottomBar() {
        ImGui.BeginDisabled(_wasInvalid);

        if (ImGuiManager.TranslatedButton("rysy.create")) {
            _history.ApplyNewAction(new AddAnimatedTileAction($"""
            <sprite name="{_name}" path="animatedTiles/grass/top_a" delay="0.2" posX="0" posY="-8" origX="4" origY="4" />
            """));
            
            RemoveSelf();
        }
        
        ImGui.EndDisabled();
    }
}

internal sealed class XmlSnippetAnimatedTileImportWindow : Window {
    private readonly HistoryHandler _history;
    
    private string _xml = "";
    private bool _wasInvalid = false;
    
    public XmlSnippetAnimatedTileImportWindow(HistoryHandler history) : base("rysy.animTileImport.importFromXml.windowName".Translate(),
        new(400, ImGui.GetTextLineHeightWithSpacing() * 18)) {
        _history = history;
    }

    protected override void Render() {
        _wasInvalid = false;
        ImGuiManager.TranslatedTextWrapped("rysy.animTileImport.importFromXml.tip");
        ImGui.InputTextMultiline("", ref _xml, 8192, ImGui.GetContentRegionAvail());
    }

    public override bool HasBottomBar => true;

    public override void RenderBottomBar() {
        ImGui.BeginDisabled(_wasInvalid);

        if (ImGuiManager.TranslatedButton("rysy.ok")) {
            _history.ApplyNewAction(new AddAnimatedTileAction($"<Data>\n{_xml}\n</Data>"));
            
            RemoveSelf();
        }
        
        ImGui.EndDisabled();
    }
}

internal sealed class ExistingSpriteTilesetImportWindow : Window {
    private Field _pathField;
    private Field _templateField;
    
    private bool _wasValid;
    private string _path = "";
    private bool _bg;
    
    private TilesetTemplates.Templates _template = TilesetTemplates.Templates.Vanilla;
    private string _xml = "rysy.tilesetImport.templatePlaceholder".Translate();
    
    public ExistingSpriteTilesetImportWindow(bool bg) : base("rysy.tilesetImport.fromExistingSprite".Translate(), new(640, 450)) {
        _bg = bg;
        _pathField = Fields.AtlasPath("", @"^tilesets/(.*)$").AllowEdits(false).WithValidator(x => {
            var path = x?.ToString();
            if (string.IsNullOrWhiteSpace(path))
                return ValidationResult.CantBeNull;
            // Path is guaranteed to be correct once provided, because the dropdown is un-editable
            return ValidationResult.Ok;
        });

        _templateField = Fields.EnumNamesDropdown(TilesetTemplates.Templates.Vanilla);
    }

    protected override void Render() {
        var valid = _pathField.IsValid(_path);
        _wasValid = valid.IsOk;

        if (_pathField.RenderGuiWithValidation("rysy.tilesetImport.importPath".Translate(), _path, valid)?.ToString() is {} newPath) {
            _path = newPath;
        }
        
        _template = _templateField.RenderGui("rysy.tilesetImport.template".Translate(), _template.ToString())
                        is string t ? Enum.Parse<TilesetTemplates.Templates>(t) : _template;
        
        var windowSize = ImGui.GetContentRegionAvail();
        
        ImGui.BeginDisabled(_template != TilesetTemplates.Templates.Custom);
        var prevXml = _xml;
        ImGui.InputTextMultiline("##Template", ref _xml, (nuint)_xml.Length + 3, windowSize);
        var placeholderText = "rysy.tilesetImport.templatePlaceholder".Translate();
        if (_xml != prevXml && _xml.StartsWith(placeholderText, StringComparison.Ordinal) && _xml.Length > placeholderText.Length) {
            _xml = _xml[(placeholderText.Length)..];
        }
        ImGui.EndDisabled();
    }

    public override bool HasBottomBar => true;

    public override void RenderBottomBar() {
        VirtTexture? texture = null!;
        ImGui.BeginDisabled(!_wasValid || !Gfx.Atlas.TryGet($"tilesets/{_path}", out texture));
        if (ImGuiManager.TranslatedButton("rysy.tilesetImport.import") && texture is {}) {
            RysyState.Scene.AddWindow(new CreateTilesetWindow(new ImportedTileset {
                Texture = texture,
                CopyFrom = null,
                CreateTextureClone = false,
                DefaultDisplayName = null,
                IsBg = _bg,
                Name = _path,
                Template = _template switch {
                    TilesetTemplates.Templates.Vanilla => "vanilla",
                    TilesetTemplates.Templates.PixelatorAlternate => "alternate",
                    TilesetTemplates.Templates.JadeBetter => "better",
                    TilesetTemplates.Templates.Custom => _xml,
                    _ => throw new ArgumentOutOfRangeException()
                },
                TexturePath = _path,
            }));
            
            RemoveSelf();
        }
        ImGui.EndDisabled();
    }
}

internal sealed class CreateDefaultXmlWindow(TilesetWindow.Tabs tab) 
    : CreateNewAssetWindow("rysy.tilesetWindow.xmlCantBeEdited.createNew", GetDefaultPath(tab)) {
    
    protected override string RealPath(string userPath) {
        return $"Graphics/{userPath}.xml";
    }

    protected override string PathFieldTranslationKey => "rysy.tilesetWindow.xmlCantBeEdited.path";

    private static string GetDefaultPath(TilesetWindow.Tabs tab) {
        var map = EditorState.Map;
        if (map is null) {
            throw new Exception($"Map cant be null when {nameof(CreateDefaultXmlWindow)} is created");
        }

        var defaultFileName = tab switch {
            TilesetWindow.Tabs.Bg => "BackgroundTiles",
            TilesetWindow.Tabs.Fg => "ForegroundTiles",
            TilesetWindow.Tabs.AnimatedTiles => "AnimatedTiles",
            _ => throw new ArgumentOutOfRangeException(nameof(tab), tab, null)
        };
        
        return $"{map.GetDefaultAssetSubdirectory()?.TrimEnd('/') ?? ""}/{defaultFileName}".TrimStart('/');
    }

    protected override void Create(Map map, IWriteableModFilesystem fs, string realPath) {
        var xmlContents = tab switch {
            TilesetWindow.Tabs.Bg => NewModWindow.BackgroundTilesXmlContents,
            TilesetWindow.Tabs.Fg => NewModWindow.ForegroundTilesXmlContents,
            TilesetWindow.Tabs.AnimatedTiles => NewModWindow.AnimatedTilesXmlContents,
        };
        
        if (fs.TryWriteToFile(realPath, xmlContents.Value)) {
            var metaCopy = new MapMetadata();
            metaCopy.Unpack(map.Meta.Pack());
            
            switch (tab) {
                case TilesetWindow.Tabs.Bg:
                    metaCopy.BackgroundTiles = realPath;
                    break;
                case TilesetWindow.Tabs.Fg:
                    metaCopy.ForegroundTiles = realPath;
                    break;
                case TilesetWindow.Tabs.AnimatedTiles:
                    metaCopy.AnimatedTiles = realPath;
                    break;
            }

            map.Meta = metaCopy;
        }
    }
}
