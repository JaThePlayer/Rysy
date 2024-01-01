using ImGuiNET;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.Mods;

namespace Rysy.Gui.Windows;

public sealed class DecalRegistryWindow : Window {
    readonly Map Map;
    bool firstRender = true;

    private DecalRegistryEntry? Selection;
    private DecalRegistryProperty? SelectionProp;

    private FormWindow? Form;
    private DecalRegistryProperty? FormProp;

    private List<DecalRegistryEntry>? Entries;

    public DecalRegistryWindow(Map map) : base("Decal Registry", new(1200, 800)) {
        Map = map;
    }

    private void SetSelection(DecalRegistryEntry? entry, DecalRegistryProperty? property) {
        Selection = entry;
        SelectionProp = property;

        CreateForm(property);
    }

    public static FieldList GetFields(DecalRegistryProperty main) {
        FieldList fieldInfo = null!;
        if (main is IPlaceable) {
            fieldInfo = (FieldList) main.GetType().GetMethod(nameof(IPlaceable.GetFields))?.Invoke(null, null)!;
        }
        fieldInfo ??= new FieldList();

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
            prop.Data.SetOverlay(null);
        };
        form.OnLiveUpdate = (edited) => {
            //prop.Data.BulkUpdate(edited);
            prop.Data.SetOverlay(edited);
            GFX.DecalRegistry.Serialize(Entries);
        };
        FormProp?.Data.SetOverlay(null);
        FormProp = prop;
        Form = form;
    }

    protected override void Render() {
        base.Render();

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
        var cam = new Camera(RysyEngine.GDM.GraphicsDevice.Viewport);
        cam.Scale = 6f;
        cam.Move(-new Vector2(previewW / 2f / cam.Scale, 300 / 2f / cam.Scale));

        ImGuiManager.XnaWidget("decal_registry_preview", previewW, 300, () => {
            if (Selection is { } entry) {
                var ctx = SpriteRenderCtx.Default(true);
                foreach (var item in entry.GetSprites()) {
                    item.Render(ctx);
                }
            }
        }, cam);

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

        ImGui.SameLine();
        ImGui.Text(prop.Name);

        //RenderOtherTabs(style);
    }

    private void RenderEntry(DecalRegistryEntry entry) {
        var id = entry.GetHashCode();

        ImGui.TableNextRow();

        ImGui.TableNextColumn();

        var flags = ImGuiTreeNodeFlags.SpanFullWidth;
        if (Selection == entry) {
            flags |= ImGuiTreeNodeFlags.Selected;
        }

        var open = ImGui.TreeNodeEx($"##{id}", flags);
        var clicked = ImGui.IsItemClicked();
        //AddStyleContextWindow(style, id);

        ImGui.SameLine();
        ImGui.Text(entry.Path);

        //RenderOtherTabs(style);

        if (open) {
            foreach (var prop in entry.Props) {
                //RenderStyleImgui(innerStyle);
                RenderProp(entry, prop);
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            //RenderAddNewEntry(apply);

            ImGui.TreePop();
        }

        if (clicked) {
            SetSelection(entry, null);
        }
    }

    public void RenderList(ModMeta mod) {
        if (!ImGui.BeginChild("list", new(ImGui.GetColumnWidth() - ImGui.GetStyle().FramePadding.X * 2, ImGui.GetWindowHeight() - 100f)))
            return;

        var entries = Entries ??= GFX.DecalRegistry.GetEntriesForMod(mod).ToList();

        var flags = ImGuiManager.TableFlags;
        var textBaseWidth = ImGui.CalcTextSize("m").X;

        if (!ImGui.BeginTable("Styles", 1, flags)) {
            ImGui.EndChild();
            return;
        }

        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthStretch);
        //ImGui.TableSetupColumn("Rooms", ImGuiTableColumnFlags.WidthFixed, textBaseWidth * 12f);
        ImGui.TableHeadersRow();

        foreach (var entry in entries) {
            RenderEntry(entry);
        }

        //ImGui.TableNextRow();
        //ImGui.TableNextColumn();
        //RenderAddNewEntry(null);

        //ImGui.TableNextRow();
        //ImGui.TableNextColumn();
        //RenderAddNewFolder();

        ImGui.EndTable();
        ImGui.EndChild();
    }
}
