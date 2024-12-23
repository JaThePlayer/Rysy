using ImGuiNET;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Graphics.TextureTypes;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Mods;
using System.Xml;
using System.Xml.Linq;

namespace Rysy.Gui.Windows;

public sealed class TilesetWindow : Window {
    private Map? Map => EditorState.Map;

    internal ModMeta? Mod => Map?.Mod;
    
    bool firstRender = true;

    private FormWindow? Form;

    private char formPropId = '\0';

    private TilesetData? FormProp {
        get => (_bg ? Map.BGAutotiler : Map.FGAutotiler).Tilesets.GetValueOrDefault(formPropId);
        set => formPropId = value?.Id ?? '\0';
    }

    private readonly HotkeyHandler _hotkeyHandler;
    private HistoryHandler? _history;

    private bool _bg;

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
            Form?.ReevaluateChanged(prop.FakeData.Inner);
        }
        Save();
    }

    private void SetSelection(TilesetData? entry) {
        CreateForm(entry);
    }

    private void DeleteSelections()
        => ForAllSelections((e) => RemoveEntry(e.Id, _bg));

    private FieldList GetFields(TilesetData main) {
        FieldList fieldInfo = main.GetFields(_bg);

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

        var defaultTooltipKeyPrefix = $"rysy.tilesetWindow.description";
        var defaultNameKeyPrefix = $"rysy.tilesetWindow.attribute";

        foreach (var (name, f) in fields) {
            f.Tooltip ??= name.TranslateOrNull(defaultTooltipKeyPrefix);
            f.NameOverride ??= name.TranslateOrNull(defaultNameKeyPrefix);
        }

        return fields.Ordered(order);
    }

    private void CreateForm(TilesetData? prop) {
        if (prop is null) {
            Form = null;
            return;
        }

        var fields = GetFields(prop);

        var form = new FormWindow(fields, prop.Filename);
        form.Exists = prop.FakeData.Has;
        form.OnChanged = (edited) => {
            FormProp?.FakeData.SetOverlay(null);
            _history?.ApplyNewAction(new TilesetChangedAction(formPropId, _bg, new(edited)));

            Save();
        };
        form.OnLiveUpdate = (edited) => {
            Dictionary<string, object> editCopy = new(edited);
            FormProp?.FakeData.SetOverlay(editCopy);
            
            FormProp?.UpdateData(FormProp.FakeData!);
        };

        if (FormProp is { } prev) {
            var overlay = prev.FakeData.FakeOverlay;
            prev.FakeData.SetOverlay(null);
            prev.UpdateData(FormProp.FakeData!);
            
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

        
        FormProp = prop;
        Form = form;
    }

    private void Save() {
        Map?.SaveTilesetXml(_bg);
    }

    private void ChangeTab(bool newBg) {
        if (_bg != newBg) {
            _bg = newBg;
            FormProp = null;
            SetSelection(null);
        }
    }
    
    protected override void Render() {
        if (Map is not { Mod: not null }) {
            ImGuiManager.TranslatedText("rysy.tilesetWindow.needToBeInMap");
            return;
        }

        if (Map.GetModContainingTilesetXml(_bg)?.Filesystem is not IWriteableModFilesystem) {
            ImGuiManager.TranslatedText("rysy.tilesetWindow.xmlCantBeEdited");
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

        var size = ImGui.GetWindowSize();

        ImGui.Columns(2);

        if (firstRender)
            ImGui.SetColumnWidth(0, size.X / 3);

        if (ImGui.BeginTabBar("layer")) {
            if (ImGui.BeginTabItem("FG")) {
                ChangeTab(false);
                RenderList(false);
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("BG")) {
                ChangeTab(true);
                RenderList(true);
                ImGui.EndTabItem();
            }
            
            ImGui.EndTabBar();
        }
        

        ImGui.NextColumn();

        var previewW = (int) ImGui.GetColumnWidth();
        var cam = new Camera(RysyState.GraphicsDevice.Viewport);
        cam.Scale = 2f;
        cam.Move(-new Vector2(previewW / 2f / cam.Scale, 300 / 2f / cam.Scale));

        ImGuiManager.XnaWidget("tileset_preview", previewW, 300, () => {
            var ctx = SpriteRenderCtx.Default(true);
                
            IEnumerable<ISprite>? sprites;
            if (Form is {} && FormProp is { } prop) {
                sprites = ISprite.FromTexture(prop.Texture).Centered();
            } else {
                sprites = [];
            }
                
            foreach (var item in sprites) {
                item.Render(ctx);
            }
        }, cam, rerender: true);

        ImGui.BeginChild("form");
        Form?.RenderBody();
        ImGui.EndChild();

        ImGui.Columns();

        firstRender = false;
    }

    private void ForAllSelections(Action<TilesetData>? onEntry) {
        if (FormProp is not { } entry)
            return;
        
        onEntry?.Invoke(entry);
    }

    private bool CanRemove(char id) {
        var autotiler = GetAutotiler(_bg);

        return autotiler.Tilesets.All(x => x.Value.CopyFrom != id);
    }
    
    private void RemoveEntry(char entryId, bool bg) {
        var autotiler = GetAutotiler(_bg);

        if (!CanRemove(entryId))
            return;
        
        if (autotiler?.GetTilesetData(entryId) is { Texture: ModTexture { Mod: { } textureMod } } tilesetData
            && Mod == textureMod && textureMod.Filesystem is IWriteableModFilesystem
            && autotiler.Tilesets.Count(x => x.Value.Filename == tilesetData.Filename) == 1) {
            RysyState.Scene.AddWindow(new ScriptedWindow("rysy.tilesetWindow.removeSourceTexture.name".Translate(),
                (w) => {
                    ImGuiManager.TranslatedTextWrapped("rysy.tilesetWindow.removeSourceTexture");
                }, new(400, 120), bottomBarFunc: (w) => {
                    if (ImGuiManager.TranslatedButton("rysy.ok")) {
                        _history?.ApplyNewAction(new RemoveTilesetAction(entryId, _bg, removeSourceTexture: true));
                        if (FormProp?.Id == entryId)
                            FormProp = null;
                        w.RemoveSelf();
                    }
                    
                    ImGui.SameLine();
                    if (ImGuiManager.TranslatedButton("rysy.no")) {
                        _history?.ApplyNewAction(new RemoveTilesetAction(entryId, _bg, removeSourceTexture: false));
                        if (FormProp?.Id == entryId)
                            FormProp = null;
                        w.RemoveSelf();
                    }
                    
                    ImGui.SameLine();
                    if (ImGuiManager.TranslatedButton("rysy.cancel")) {
                        w.RemoveSelf();
                    }
                }));
        } else {
            _history?.ApplyNewAction(new RemoveTilesetAction(entryId, _bg, removeSourceTexture: false));
            if (FormProp?.Id == entryId)
                FormProp = null;
        }
    }
    
    private void RenderEntry(TilesetData entry, bool bg) {
        var id = $"[{entry.Id.ToImguiEscapedString()}] {entry.GetDisplayName()}";

        ImGui.TableNextRow();

        ImGui.TableNextColumn();

        var flags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanFullWidth;
        if (FormProp == entry) {
            flags |= ImGuiTreeNodeFlags.Selected;
        }

        var open = ImGui.TreeNodeEx(Interpolator.Temp($"##{id}"), flags);
        var clicked = ImGui.IsItemClicked();
        var tileid = entry.Id;
        AddContextWindow(id, render: () => {
            var autotiler = GetAutotiler(_bg);
            var tileset = autotiler.GetTilesetData(tileid);
            if (tileset is { Xml: not null } && ImGuiManager.TranslatedButton("rysy.tilesetWindow.clone")) {
                RysyState.Scene.AddWindow(new CreateTilesetWindow(new ImportedTileset {
                    Texture = tileset.Texture,
                    CreateTextureClone = false,
                    IsBg = _bg,
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
        });

        ImGui.SameLine();
        ImGui.BeginDisabled(entry.IsTemplate);
        ImGui.Text(id);
        ImGui.EndDisabled();

        if (clicked) {
            SetSelection(entry);
        }
    }

    public void RenderList(bool bg) {
        if (Map is null)
            return;
        
        if (!ImGui.BeginChild("list", new(ImGui.GetColumnWidth() - ImGui.GetStyle().FramePadding.X * 2, ImGui.GetWindowHeight() - 100f)))
            return;

        var autotiler = GetAutotiler(bg);

        var entries = autotiler.Tilesets.Values;

        var flags = ImGuiManager.TableFlags;

        if (!ImGui.BeginTable("Tilesets", 1, flags)) {
            ImGui.EndChild();
            return;
        }

        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        foreach (var entry in entries) {
            RenderEntry(entry, bg);
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        
        ImGuiManager.PushNullStyle();
        ImGui.TreeNodeEx("rysy.new".Translate(), ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanFullWidth);
        ImGuiManager.PopNullStyle();
        
        var id = $"new_tileset";
        ImGui.OpenPopupOnItemClick(id, ImGuiPopupFlags.MouseButtonLeft);

        if (ImGui.BeginPopupContextWindow(id, ImGuiPopupFlags.NoOpenOverExistingPopup | ImGuiPopupFlags.MouseButtonMask)) {
            if (ImGuiManager.TranslatedButton("rysy.tilesetImport.fromAssetDrive")) {
                RysyState.Scene.AddWindowIfNeeded(() => new AssetDriveTilesetImportWindow(_bg));
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        ImGui.EndTable();
        ImGui.EndChild();
    }

    private Autotiler GetAutotiler(bool bg)
    {
        return bg ? Map.BGAutotiler : Map.FGAutotiler;
    }

    private void AddContextWindow(string id, Action? render = null) {
        var sid = $"d_ctx_{id}";
        ImGui.OpenPopupOnItemClick(sid, ImGuiPopupFlags.MouseButtonRight);

        if (ImGui.BeginPopupContextWindow(sid, ImGuiPopupFlags.NoOpenOverExistingPopup | ImGuiPopupFlags.MouseButtonMask)) {
            render?.Invoke();

            ImGui.EndPopup();
        }
    }
}
