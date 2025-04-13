using ImGuiNET;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.History;

namespace Rysy.Gui.Windows;

internal sealed class TileLayersWindow : Window {
    private Map? _map;

    private FormWindow? Form;
    private TileLayer? FormLayer;
    private bool firstRender = true;
    private bool FG = true;
    
    public TileLayersWindow() : base("rysy.tilegrids.windowName".Translate(), new(1200, 400)) {
        EditorState.OnMapChanged += MapChanged;
        
        SetRemoveAction((w) => {
            EditorState.OnMapChanged -= MapChanged;
        });

        MapChanged();
    }

    private void MapChanged() {
        _map = EditorState.Map;
    }
    
    public static FieldList GetFields(TileLayer info) {
        var fields = info.GetFields();
        var order = new List<string>();

        var tooltipKeyPrefix = "rysy.tilegrids.fields.description";
        var nameKeyPrefix = "rysy.tilegrids.fields.attribute";

        fields.AddTranslations(tooltipKeyPrefix, nameKeyPrefix);

        return fields.Ordered(order);
    }
    
    private void RemoveForm() {
        if (Form is { } && _map is {}) {
            Form = null;
            FormLayer?.SetOverlay(null, _map);
            FormLayer = null;
        }
    }

    private void ClearSelection() {
        RemoveForm();
    }
    
    private void CreateForm(TileLayer? layer) {
        RemoveForm();
        
        if (layer is null || _map is null) {
            return;
        }
        
        var fields = GetFields(layer);

        var form = new FormWindow(fields, layer.Name);
        form.Exists = (key) => true;// style.Data.Has;
        
        form.OnChanged = (edited) => {
            var action = new ChangeTileLayerAction(layer, edited);

            EditorState.History?.ApplyNewAction(action);
        };
        form.OnLiveUpdate = (edited) => {
            layer.SetOverlay(edited, _map);
        };

        FormLayer = layer;
        Form = form;
    }

    private void RemoveEntry(TileLayer? layer) {
        
    }
    
    public override void RemoveSelf() {
        base.RemoveSelf();
        
        RemoveForm();
    }
    
    protected override void Render() {
        if (ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows) && !ImGui.GetIO().WantCaptureKeyboard) {
        //    HotkeyHandler.Update();
        }

        var size = ImGui.GetWindowSize();

        ImGui.Columns(2);

        if (firstRender)
            ImGui.SetColumnWidth(0, size.X / 3);

        if (ImGui.BeginTabBar("Layer")) {
            if (ImGui.BeginTabItem("BG")) {
                if (FG) {
                    ClearSelection();
                }
                FG = false;
                RenderList(TileLayer.BuiltinTypes.Bg);

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("FG")) {
                if (!FG) {
                    ClearSelection();
                }
                FG = true;
                RenderList(TileLayer.BuiltinTypes.Fg);

                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.NextColumn();

        ImGui.BeginChild("form");
        Form?.RenderBody();
        ImGui.EndChild();

        ImGui.Columns();

        firstRender = false;
    }

    private void RenderList(TileLayer.BuiltinTypes type) {
        if (_map is null)
            return;

        var layers = _map.GetUsedTileLayers();
        
        ImGui.BeginChild("list", new(ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y));
        if (ImGui.BeginTable("Tilegrid Layers", 1, ImGuiManager.TableFlags)) {
            try {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableHeadersRow();

                foreach (var entry in layers) {
                    if (entry.Type == type)
                        RenderEntry(entry);
                }

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
        
                ImGuiManager.PushNullStyle();
                ImGui.TreeNodeEx("rysy.new".Translate(), ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanFullWidth);
                ImGuiManager.PopNullStyle();
            } finally {
                ImGui.EndTable();
                ImGui.EndChild();
            }
        }
    }

    private void RenderEntry(TileLayer layer) {
        var id = $"{layer.DisplayName.ToImguiEscaped()}";

        ImGui.TableNextRow();

        ImGui.TableNextColumn();

        var flags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanFullWidth;
        if (layer.Equals(FormLayer)) {
            flags |= ImGuiTreeNodeFlags.Selected;
        }

        var open = ImGui.TreeNodeEx(Interpolator.Temp($"##{id}"), flags);
        var clicked = ImGui.IsItemClicked() && !layer.IsBuiltin;

        if (!layer.IsBuiltin) {
            var sid = $"d_ctx_{id}";
            ImGui.OpenPopupOnItemClick(sid, ImGuiPopupFlags.MouseButtonRight);
            if (ImGui.BeginPopupContextWindow(sid, ImGuiPopupFlags.NoOpenOverExistingPopup | ImGuiPopupFlags.MouseButtonRight)) {
                if (ImGuiManager.TranslatedButton("rysy.delete")) {
                    RemoveEntry(layer);
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
        }

        ImGui.SameLine();
        ImGui.BeginDisabled(layer.IsBuiltin);
        ImGui.Text(id);
        ImGui.EndDisabled();

        if (clicked) {
            CreateForm(layer);
        }
    }
}