using Hexa.NET.ImGui;
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
    
    private readonly Map _map;

    internal ModMeta Mod => _map.Mod!;
    
    bool _firstRender = true;

    private DecalRegistryEntry? _selection;
    private DecalRegistryProperty? _selectionProp;

    private FormWindow? _form;
    private DecalRegistryProperty? _formProp;

    private readonly HotkeyHandler _hotkeyHandler;
    private readonly HistoryHandler _history;

    internal List<DecalRegistryEntry> Entries => Gfx.DecalRegistry.GetEntriesForMod(_map.Mod!);

    public DecalRegistryWindow(Map map) : base("Decal Registry", new(1200, 800)) {
        _map = map;

        _history = new(map) {
            OnApply = HistoryHook,
            OnUndo = HistoryHook
        };
        
        _hotkeyHandler = new(Input.Global, HotkeyHandler.ImGuiModes.Ignore);
        _hotkeyHandler.AddHotkeyFromSettings("delete", "delete", DeleteSelections);
        
        _hotkeyHandler.AddHotkeyFromSettings("stylegrounds.moveUp", "up", () => MoveSelections(-1), HotkeyModes.OnHoldSmoothInterval);
        _hotkeyHandler.AddHotkeyFromSettings("stylegrounds.moveDown", "down", () => MoveSelections(1), HotkeyModes.OnHoldSmoothInterval);

        _hotkeyHandler.AddHotkeyFromSettings("copy", "ctrl+c", CopySelections);
        _hotkeyHandler.AddHotkeyFromSettings("paste", "ctrl+v", PasteSelections);
        _hotkeyHandler.AddHotkeyFromSettings("cut", "ctrl+x", CutSelections);
        
        _hotkeyHandler.AddHistoryHotkeys(_history.Undo, _history.Redo, Save);
    }

    private void HistoryHook() {
        _form?.ReevaluateChanged(_formProp!.Data.Inner);
        Save();
    }

    private void MoveSelections(int by) {
        if (_selection is not { } entry)
            return;

        if (_selectionProp is { } prop) {
            _history.ApplyNewAction(new DecalRegistryMovePropAction(entry, prop, by));
        } else {
            _history.ApplyNewAction(new DecalRegistryMoveEntryAction(entry, by));
        }
    }

    private void SetSelection(DecalRegistryEntry? entry, DecalRegistryProperty? property) {
        _selection = entry;
        _selectionProp = property;

        CreateForm(property);
    }

    private void DeleteSelections()
        => ForAllSelections(RemoveEntry, RemoveProp);

    private void CopySelections() {
        if (_selection is not { } entry)
            return;
        
        if (_selectionProp is { } prop) {
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
            } else if (_selection is {} entry) {
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

        fields.AddTranslations(tooltipKeyPrefix, nameKeyPrefix, defaultTooltipKeyPrefix, defaultNameKeyPrefix);

        return fields.Ordered(order);
    }

    private void CreateForm(DecalRegistryProperty? prop) {
        if (prop is null) {
            _form = null;
            return;
        }

        var fields = GetFields(prop);

        var form = new FormWindow(fields, prop.Name);
        form.Exists = prop.Data.Has;
        form.OnChanged = (edited) => {
            _history.ApplyNewAction(new DecalRegistryChangePropertyAction(prop, edited));

            Save();
        };
        form.OnLiveUpdate = (edited) => {
            prop.Data.SetOverlay(edited);
        };
        _formProp?.Data.SetOverlay(null);
        _formProp = prop;
        _form = form;
    }

    private void Save() {
        Gfx.DecalRegistry.SaveMod(Mod);
    }
    
    protected override void Render() {
        base.Render();
        
        if (ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows) && !ImGui.GetIO().WantCaptureKeyboard) {
            _hotkeyHandler.Update();
        }

        if (_map.Mod is not { } mod) {
            ImGui.Text("Decal Registry can only be edited for packaged mods.");
            return;
        }

        var size = ImGui.GetContentRegionAvail();

        ImGui.Columns(2);

        if (_firstRender)
            ImGui.SetColumnWidth(0, size.X / 3);

        RenderList(mod);

        ImGui.NextColumn();

        var previewW = (int) ImGui.GetContentRegionAvail().X;
        var cam = new Camera(RysyState.GraphicsDevice.Viewport);
        cam.Scale = 6f;
        cam.Move(-new Vector2(previewW / 2f / cam.Scale, 300 / 2f / cam.Scale));

        ImGuiManager.XnaWidget("decal_registry_preview", previewW, 300, () => {
            if (_selection is { } entry) {
                var ctx = SpriteRenderCtx.Default(true);

                IEnumerable<ISprite>? sprites;
                if (_form is {} && _formProp is { } prop) {
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
        _form?.RenderBody();
        ImGui.EndChild();

        ImGui.Columns();

        _firstRender = false;
    }

    private void RenderProp(DecalRegistryEntry entry, DecalRegistryProperty prop) {
        var id = prop.GetHashCode();

        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        var flags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanFullWidth;
        if (_selectionProp == prop) {
            flags |= ImGuiTreeNodeFlags.Selected;
        }

        var open = ImGui.TreeNodeEx($"##{id}", flags);
        var clicked = ImGui.IsItemClicked();
        if (clicked) {
            SetSelection(entry, prop);
        }
        AddContextWindow(id.ToString(CultureInfo.InvariantCulture), remove: () => {
            RemoveProp(entry, prop);
        });

        ImGui.SameLine();
        ImGui.Text(prop.Name);

        //RenderOtherTabs(style);
    }

    private void ForAllSelections(Action<DecalRegistryEntry>? onEntry, Action<DecalRegistryEntry, DecalRegistryProperty>? onProp) {
        if (_selection is not { } entry)
            return;
        
        if (_selectionProp is { } prop) {
            onProp?.Invoke(entry, prop);
        } else {
            onEntry?.Invoke(entry);
        }
    }
    
    private void RemoveEntry(DecalRegistryEntry entry) {
        _history.ApplyNewAction(new DecalRegistryRemoveEntryAction(entry));
        
        if (_selection == entry)
            _selection = null;
    }
    
    private void RemoveProp(DecalRegistryEntry entry, DecalRegistryProperty prop) {
        _history.ApplyNewAction(new DecalRegistryRemovePropAction(entry, prop));
        
        if (_selectionProp == prop)
            _selectionProp = null;
    }
    
    private void AddProp(DecalRegistryEntry entry, DecalRegistryProperty prop) {
        _history.ApplyNewAction(new DecalRegistryAddPropAction(entry, prop));
    }
    
    private void RenderEntry(DecalRegistryEntry entry) {
        var id = entry.Path;

        ImGui.TableNextRow();

        ImGui.TableNextColumn();

        var flags = ImGuiTreeNodeFlags.SpanFullWidth;
        if (_selection == entry) {
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
            ImGui.TreeNodeEx("rysy.new".Translate(), ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanFullWidth);
            ImGuiManager.PopNullStyle();

            var popupid = $"new_{entry.Path}";
            ImGui.OpenPopupOnItemClick(popupid, ImGuiPopupFlags.MouseButtonLeft);

            if (ImGui.BeginPopupContextWindow(popupid, ImGuiPopupFlags.NoOpenOverExistingPopup | ImGuiPopupFlags.MouseButtonRight)) {
                var blocked = entry.Props.Where(p => !p.AllowMultiple).Select(p => p.Name).ToHashSet();
                var placements = EntityRegistry.DecalRegistryPropertyPlacements.Where(p => !blocked.Contains(p.Sid ?? ""));
                
                ImGuiManager.List(placements, p => new Searchable(p.Sid ?? p.Name), _newPropertyComboCache, (pl) => {
                    var prop = DecalRegistryProperty.CreateFromPlacement(pl);
                    AddProp(entry, prop);
                    
                    ImGui.CloseCurrentPopup();
                });
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
        ImGui.BeginChild("list", ImGui.GetContentRegionAvail());

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
        ImGui.TreeNodeEx("rysy.new".Translate(), ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanFullWidth);
        ImGuiManager.PopNullStyle();
        
        if (!ImGui.IsPopupOpen(CreateNewEntryPopupId))
            ImGui.OpenPopupOnItemClick(CreateNewEntryPopupId, ImGuiPopupFlags.MouseButtonLeft);
        RenderEntryEditPopup(mod, CreateNewEntryPopupId);

        ImGui.EndTable();
        ImGui.EndChild();
    }

    private void RenderEntryEditPopup(ModMeta mod, string popupid, DecalRegistryEntry? toChange = null) {
        if (!ImGui.BeginPopupContextWindow(popupid, ImGuiPopupFlags.NoOpenOverExistingPopup | ImGuiPopupFlags.MouseButtonRight)) {
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
                _history.ApplyNewAction(new DecalRegistryChangeEntryPathAction(toChange, _newEntryName));
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
        _history.ApplyNewAction(new DecalRegistryAddEntryAction(entry));
        SetSelection(entry, null);
    }

    internal bool EntryExistsFor(string path) {
        return Entries.Any(e => e.Path == path);
    }
    
    internal bool IsValidPath(FoundPath path)
        => Gfx.Atlas.TryGet(path.Path, out var texture) && texture is ModTexture modTexture && modTexture.Mod == Mod && !EntryExistsFor(path.Captured);
    
    private void AddContextWindow(string id, Action? remove = null, Action? render = null) {
        var sid = $"d_ctx_{id}";
        ImGui.OpenPopupOnItemClick(sid, ImGuiPopupFlags.MouseButtonRight);

        if (ImGui.BeginPopupContextWindow(sid, ImGuiPopupFlags.NoOpenOverExistingPopup | ImGuiPopupFlags.MouseButtonRight)) {
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
