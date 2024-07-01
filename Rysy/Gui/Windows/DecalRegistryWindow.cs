using ImGuiNET;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Graphics.TextureTypes;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Mods;
using System.Xml.Linq;

namespace Rysy.Gui.Windows;

public sealed class DecalRegistryWindow : Window {
    const string CreateNewEntryPopupId = "create_new_entry";
    
    private readonly Map Map;

    internal ModMeta Mod => Map.Mod!;
    
    bool firstRender = true;

    private DecalRegistryEntry? Selection;
    private DecalRegistryProperty? SelectionProp;

    private FormWindow? Form;
    private DecalRegistryProperty? FormProp;

    private readonly HotkeyHandler HotkeyHandler;
    private readonly HistoryHandler History;

    internal List<DecalRegistryEntry> Entries => GFX.DecalRegistry.GetEntriesForMod(Map.Mod!);

    public DecalRegistryWindow(Map map) : base("Decal Registry", new(1200, 800)) {
        Map = map;

        History = new(map) {
            OnApply = HistoryHook,
            OnUndo = HistoryHook
        };
        
        HotkeyHandler = new(Input.Global, updateInImgui: true);
        HotkeyHandler.AddHotkeyFromSettings("delete", "delete", DeleteSelections);
        
        HotkeyHandler.AddHotkeyFromSettings("stylegrounds.moveUp", "up", () => MoveSelections(-1), HotkeyModes.OnHoldSmoothInterval);
        HotkeyHandler.AddHotkeyFromSettings("stylegrounds.moveDown", "down", () => MoveSelections(1), HotkeyModes.OnHoldSmoothInterval);

        HotkeyHandler.AddHotkeyFromSettings("copy", "ctrl+c", CopySelections);
        HotkeyHandler.AddHotkeyFromSettings("paste", "ctrl+v", PasteSelections);
        HotkeyHandler.AddHotkeyFromSettings("cut", "ctrl+x", CutSelections);
        
        HotkeyHandler.AddHistoryHotkeys(History.Undo, History.Redo, Save);
    }

    private void HistoryHook() {
        Form?.ReevaluateChanged(FormProp!.Data.Inner);
        Save();
    }

    private void MoveSelections(int by) {
        if (Selection is not { } entry)
            return;

        if (SelectionProp is { } prop) {
            History.ApplyNewAction(new DecalRegistryMovePropAction(entry, prop, by));
        } else {
            History.ApplyNewAction(new DecalRegistryMoveEntryAction(entry, by));
        }
    }

    private void SetSelection(DecalRegistryEntry? entry, DecalRegistryProperty? property) {
        Selection = entry;
        SelectionProp = property;

        CreateForm(property);
    }

    private void DeleteSelections()
        => ForAllSelections(RemoveEntry, RemoveProp);

    private void CopySelections() {
        if (Selection is not { } entry)
            return;
        
        if (SelectionProp is { } prop) {
            Input.Clipboard.Set(prop.Serialize().ToString());
        } else {
            Input.Clipboard.Set(entry.Serialize().ToString());
        }
    }
    
    private void CutSelections() {
        CopySelections();
        DeleteSelections();
    }

    private void PasteSelections() {
        var str = Input.Clipboard.Get();

        try {
            var xdoc = XDocument.Parse(str);
            
            if (xdoc.Element("decal") is { } decalEl && DecalRegistryEntry.TryLoadFromNode(decalEl, out var newEntry)) {
                AddEntry(newEntry);
            } else if (Selection is {} entry) {
                foreach (var el in xdoc.Elements()) {
                    var prop = DecalRegistryProperty.FromNode(el);
                    
                    AddProp(entry, prop);
                }
            }

        } catch { }
    }

    public static FieldList GetFields(DecalRegistryProperty main) {
        FieldList fieldInfo = EntityRegistry.GetFields(main.Name, RegisteredEntityType.DecalRegistryProperty);

        var fields = new FieldList();
        var order = new List<string>();

        foreach (var (k, f) in fieldInfo.OrderedEnumerable(main)) {
            fields[k] = f.CreateClone();
            order.Add(k);
        }

        // Take into account properties defined on this style, even if not present in FieldInfo
        foreach (var (k, v) in main.Data.Inner) {
            if (fields.TryGetValue(k, out var knownFieldType)) {
                fields[k].SetDefault(v);
            } else {
                fields[k] = Fields.GuessFromValue(v, fromMapData: true)!;
                order.Add(k);
            }
        }

        var tooltipKeyPrefix = $"rysy.decal_registry.{main.Name}.description";
        var nameKeyPrefix = $"rysy.decal_registry.{main.Name}.attribute";
        var defaultTooltipKeyPrefix = $"rysy.decal_registry.default.description";
        var defaultNameKeyPrefix = $"rysy.decal_registry.default.attribute";

        foreach (var (name, f) in fields) {
            f.Tooltip ??= name.TranslateOrNull(tooltipKeyPrefix) ?? name.TranslateOrNull(defaultTooltipKeyPrefix);
            f.NameOverride ??= name.TranslateOrNull(nameKeyPrefix) ?? name.TranslateOrNull(defaultNameKeyPrefix);
        }

        return fields.Ordered(order);
    }

    private void CreateForm(DecalRegistryProperty? prop) {
        if (prop is null) {
            Form = null;
            return;
        }

        var fields = GetFields(prop);

        var form = new FormWindow(fields, prop.Name);
        form.Exists = prop.Data.Has;
        form.OnChanged = (edited) => {
            History.ApplyNewAction(new DecalRegistryChangePropertyAction(prop, edited));

            Save();
        };
        form.OnLiveUpdate = (edited) => {
            prop.Data.SetOverlay(edited);
        };
        FormProp?.Data.SetOverlay(null);
        FormProp = prop;
        Form = form;
    }

    private void Save() {
        GFX.DecalRegistry.SaveMod(Mod);
    }
    
    protected override void Render() {
        base.Render();
        
        if (ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows) && !ImGui.GetIO().WantCaptureKeyboard) {
            HotkeyHandler.Update();
        }

        if (Map.Mod is not { } mod) {
            ImGui.Text("Decal Registry can only be edited for packaged mods.");
            return;
        }

        var size = ImGui.GetWindowSize();

        ImGui.Columns(2);

        if (firstRender)
            ImGui.SetColumnWidth(0, size.X / 3);

        RenderList(mod);

        ImGui.NextColumn();

        var previewW = (int) ImGui.GetColumnWidth();
        var cam = new Camera(RysyState.GraphicsDevice.Viewport);
        cam.Scale = 6f;
        cam.Move(-new Vector2(previewW / 2f / cam.Scale, 300 / 2f / cam.Scale));

        ImGuiManager.XnaWidget("decal_registry_preview", previewW, 300, () => {
            if (Selection is { } entry) {
                var ctx = SpriteRenderCtx.Default(true);

                IEnumerable<ISprite>? sprites;
                if (Form is {} && FormProp is { } prop) {
                    sprites = entry.GetAffectedTextures().SelectMany(t => prop.GetSprites(t, ctx));
                } else {
                    sprites = entry.GetSprites();
                }
                
                foreach (var item in sprites) {
                    item.Render(ctx);
                }
            }
        }, cam, rerender: true);

        ImGui.BeginChild("form");
        Form?.RenderBody();
        ImGui.EndChild();

        ImGui.Columns();

        firstRender = false;
    }

    private void RenderProp(DecalRegistryEntry entry, DecalRegistryProperty prop) {
        var id = prop.GetHashCode();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        var flags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanFullWidth;
        if (SelectionProp == prop) {
            flags |= ImGuiTreeNodeFlags.Selected;
        }

        var open = ImGui.TreeNodeEx($"##{id}", flags);
        var clicked = ImGui.IsItemClicked();
        if (clicked) {
            SetSelection(entry, prop);
        }
        AddContextWindow(id.ToString(), remove: () => {
            RemoveProp(entry, prop);
        });

        ImGui.SameLine();
        ImGui.Text(prop.Name);

        //RenderOtherTabs(style);
    }

    private void ForAllSelections(Action<DecalRegistryEntry>? onEntry, Action<DecalRegistryEntry, DecalRegistryProperty>? onProp) {
        if (Selection is not { } entry)
            return;
        
        if (SelectionProp is { } prop) {
            onProp?.Invoke(entry, prop);
        } else {
            onEntry?.Invoke(entry);
        }
    }
    
    private void RemoveEntry(DecalRegistryEntry entry) {
        History.ApplyNewAction(new DecalRegistryRemoveEntryAction(entry));
        
        if (Selection == entry)
            Selection = null;
    }
    
    private void RemoveProp(DecalRegistryEntry entry, DecalRegistryProperty prop) {
        History.ApplyNewAction(new DecalRegistryRemovePropAction(entry, prop));
        
        if (SelectionProp == prop)
            SelectionProp = null;
    }
    
    private void AddProp(DecalRegistryEntry entry, DecalRegistryProperty prop) {
        History.ApplyNewAction(new DecalRegistryAddPropAction(entry, prop));
    }
    
    private void RenderEntry(DecalRegistryEntry entry) {
        var id = entry.Path;

        ImGui.TableNextRow();

        ImGui.TableNextColumn();

        var flags = ImGuiTreeNodeFlags.SpanFullWidth;
        if (Selection == entry) {
            flags |= ImGuiTreeNodeFlags.Selected;
        }

        var open = ImGui.TreeNodeEx($"##{id}", flags);
        var clicked = ImGui.IsItemClicked();
        AddContextWindow(id.ToString(), remove: () => {
            RemoveEntry(entry);
        }, render: () => {
            if (ImGui.Button("Edit")) {
                _newEntryName = entry.Path.TrimEnd('*');
                _newEntryType = entry.Type;
                _pathField = null;
                ImGui.OpenPopup(CreateNewEntryPopupId);
            }
            RenderEntryEditPopup(Mod, CreateNewEntryPopupId, entry);
        });

        ImGui.SameLine();
        ImGui.Text(entry.Path);

        //RenderOtherTabs(style);

        if (open) {
            foreach (var prop in entry.Props) {
                RenderProp(entry, prop);
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            
            ImGuiManager.PushNullStyle();
            ImGui.TreeNodeEx("New...", ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanFullWidth);
            ImGuiManager.PopNullStyle();

            var popupid = $"new_{entry.Path}";
            ImGui.OpenPopupOnItemClick(popupid, ImGuiPopupFlags.MouseButtonLeft);

            if (ImGui.BeginPopupContextWindow(popupid, ImGuiPopupFlags.NoOpenOverExistingPopup | ImGuiPopupFlags.MouseButtonMask)) {
                var blocked = entry.Props.Where(p => !p.AllowMultiple).Select(p => p.Name).ToHashSet();
                var placements = EntityRegistry.DecalRegistryPropertyPlacements.Where(p => !blocked.Contains(p.SID ?? ""));
                
                ImGuiManager.List(placements, p => p.SID ?? p.Name, _newPropertyComboCache, (pl) => {
                    var prop = DecalRegistryProperty.CreateFromPlacement(pl);
                    AddProp(entry, prop);
                    
                    ImGui.CloseCurrentPopup();
                }, favorites: []);
                _newPropertyComboCache.Clear();

                ImGui.EndPopup();
            }

            ImGui.TreePop();
        }

        if (clicked) {
            SetSelection(entry, null);
        }
    }

    public void RenderList(ModMeta mod) {
        if (!ImGui.BeginChild("list", new(ImGui.GetColumnWidth() - ImGui.GetStyle().FramePadding.X * 2, ImGui.GetWindowHeight() - 100f)))
            return;

        var entries = Entries;

        var flags = ImGuiManager.TableFlags;

        if (!ImGui.BeginTable("Styles", 1, flags)) {
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
        ImGui.TreeNodeEx("New...", ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanFullWidth);
        ImGuiManager.PopNullStyle();
        
        if (!ImGui.IsPopupOpen(CreateNewEntryPopupId))
            ImGui.OpenPopupOnItemClick(CreateNewEntryPopupId, ImGuiPopupFlags.MouseButtonLeft);
        RenderEntryEditPopup(mod, CreateNewEntryPopupId);

        ImGui.EndTable();
        ImGui.EndChild();
    }

    private void RenderEntryEditPopup(ModMeta mod, string popupid, DecalRegistryEntry? toChange = null) {
        if (!ImGui.BeginPopupContextWindow(popupid, ImGuiPopupFlags.NoOpenOverExistingPopup | ImGuiPopupFlags.MouseButtonMask)) {
            _pathField = null;
            return;
        }
        
        var isInvalid = EntryExistsFor(_newEntryName);

        _pathField ??= new(true) {
            DecalRegistryWindow = this,
        };
        var path = new DecalRegistryPath(_newEntryName);

        if (_pathField.RenderDetailedWindow(ref path)) {
            _newEntryName = path.SavedName;
            _newEntryType = path.Type;
        }

        ImGui.BeginDisabled(isInvalid);
        if (ImGuiManager.TranslatedButton("rysy.decalRegistryWindow.create")) {
            if (_newEntryType is DecalRegistryEntry.Types.StartsWith && _newEntryName is not [.., '*']) {
                _newEntryName += "*";
            }

            if (toChange is { }) {
                History.ApplyNewAction(new DecalRegistryChangeEntryPathAction(toChange, _newEntryName));
                SetSelection(toChange, null);
            } else {
                var entry = new DecalRegistryEntry { Path = _newEntryName };
                AddEntry(entry);
            }

            _newEntryName = "";
            _pathField = null;
            ImGui.CloseCurrentPopup();
        }
        ImGui.EndDisabled();

        ImGui.EndPopup();
    }

    private void AddEntry(DecalRegistryEntry entry)
    {
        History.ApplyNewAction(new DecalRegistryAddEntryAction(entry));
        SetSelection(entry, null);
    }

    internal bool EntryExistsFor(string path) {
        return Entries.Any(e => e.Path == path);
    }
    
    internal bool IsValidPath(FoundPath path)
        => GFX.Atlas.TryGet(path.Path, out var texture) && texture is ModTexture modTexture && modTexture.Mod == Mod && !EntryExistsFor(path.Captured);
    
    private void AddContextWindow(string id, Action? remove = null, Action? render = null) {
        var sid = $"d_ctx_{id}";
        ImGui.OpenPopupOnItemClick(sid, ImGuiPopupFlags.MouseButtonRight);

        if (ImGui.BeginPopupContextWindow(sid, ImGuiPopupFlags.NoOpenOverExistingPopup | ImGuiPopupFlags.MouseButtonMask)) {
            render?.Invoke();
            
            if (remove is {} && ImGui.Button("Remove")) {
                RysyState.OnEndOfThisFrame += remove;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    private string _newEntryName = "";
    private DecalRegistryEntry.Types _newEntryType = DecalRegistryEntry.Types.SingleTexture;
    private DecalRegistryPathField? _pathField;
    
    private readonly ComboCache<Placement> _newPropertyComboCache = new();

}
