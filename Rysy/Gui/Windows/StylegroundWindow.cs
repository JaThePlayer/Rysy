using ImGuiNET;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.History;

namespace Rysy.Gui.Windows;

public class StylegroundWindow : Window {
    private HistoryHandler History;
    private Map Map;

    private bool firstRender = true;

    private bool FG = false;

    private Style? _Selected = null;
    private Dictionary<string, object>? SelectedAlteredValues;

    private Style? Selected {
        get => _Selected;
        set {
            SelectedAlteredValues = null;
            _Selected = value;

            CreateForm(value);
        }
    }

    private FormWindow? Form;

    public StylegroundWindow(HistoryHandler history, Map map) : base("rysy.stylegrounds.windowName".Translate(), new(1000, 800)) {
        History = history;
        Map = map;

        NoSaveData = false;
    }

    public static FieldList GetFields(Style main) {
        var fieldInfo = EntityRegistry.GetFields(main.Name);

        var fields = new FieldList();

        foreach (var (k, f) in fieldInfo) {
            fields[k] = f.CreateClone();
        }

        // Take into account properties defined on this style, even if not present in FieldInfo
        foreach (var (k, v) in main.Data.Inner) {
            if (fields.TryGetValue(k, out var knownFieldType)) {
                fields[k].SetDefault(v);
            } else {
                fields[k] = Fields.GuessFromValue(v, fromMapData: true)!;
            }
        }


        var tooltipKeyPrefix = $"style.effects.{main.Name}.description";
        var nameKeyPrefix = $"style.effects.{main.Name}.attribute";
        var defaultTooltipKeyPrefix = $"style.effects.default.description";
        var defaultNameKeyPrefix = $"style.effects.default.attribute";

        foreach (var (name, f) in fields) {
            f.Tooltip ??= name.TranslateOrNull(tooltipKeyPrefix) ?? name.TranslateOrNull(defaultTooltipKeyPrefix);
            f.NameOverride ??= name.TranslateOrNull(nameKeyPrefix) ?? name.TranslateOrNull(defaultNameKeyPrefix);
        }

        return fields;
    }

    private void CreateForm(Style? style) {
        if (style is null) {
            Form = null;
            return;
        }

        var fields = GetFields(style);

        var form = new FormWindow(fields, style.Name);
        form.Exists = style.Data.Has;
        form.OnChanged = (edited) => {
            History.ApplyNewAction(new ChangeStylegroundAction(style, edited));
        };
        form.OnLiveUpdate = (edited) => {
            SelectedAlteredValues = form.GetAllValues();
        };

        Form = form;
    }

    protected override void Render() {
        ImGui.ShowDemoWindow();

        var size = ImGui.GetWindowSize();

        ImGui.Columns(2);

        if (firstRender)
            ImGui.SetColumnWidth(0, size.X / 3);

        if (ImGui.BeginTabBar("Layer")) {
            if (ImGui.BeginTabItem("BG")) {
                FG = false;
                RenderList(FG);

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("FG")) {
                FG = true;
                RenderList(FG);

                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
        

        ImGui.NextColumn();

        var previewW = (int)ImGui.GetColumnWidth();
        ImGuiManager.XnaWidget("styleground_preview", previewW, 300, () => {
            if (Selected is { } selected) {
                IEnumerable<ISprite> sprites = Array.Empty<ISprite>();
                var oldData = selected.Data.Inner;

                try {
                    if (SelectedAlteredValues is { } altered) {
                        selected.Data.Inner = SelectedAlteredValues;
                        sprites = Selected.GetPreviewSprites().ToList();
                    } else {
                        sprites = Selected.GetPreviewSprites();
                    }
                } finally {
                    selected.Data.Inner = oldData;
                }

                foreach (var sprite in sprites) {
                    sprite.Render();
                }
            }

        });

        ImGui.BeginChild("form");
        Form?.RenderBody();
        ImGui.EndChild();

        ImGui.Columns();

        firstRender = false;
    }

    private void RenderList(bool fg) {
        if (!ImGui.BeginChild("list"))
            return;

        var flags = ImGuiTableFlags.BordersV | ImGuiTableFlags.BordersOuterH | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.Hideable;
        var textBaseWidth = ImGui.CalcTextSize("A").X;
        var styles = fg ? Map.Style.Foregrounds : Map.Style.Backgrounds;

        if (!ImGui.BeginTable("Styles", 2, flags)) {
            ImGui.EndChild();
            return;
        }

        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Rooms", ImGuiTableColumnFlags.WidthFixed, textBaseWidth * 12f);
        ImGui.TableHeadersRow();

        var id = 0;
        foreach (var style in styles) {
            RenderStyleImgui(style, ref id);
        }

        ImGui.EndTable();
        ImGui.EndChild();
    }

    private void RenderStyleImgui(Style style, ref int id) {
        var name = $"{(style is Parallax parallax ? parallax.Texture : style.Name)}##{id++}";

        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        if (style is Apply apply) {
            var flags = ImGuiTreeNodeFlags.SpanFullWidth;
            if (Selected == style) {
                flags |= ImGuiTreeNodeFlags.Selected;
            }

            var open = ImGui.TreeNodeEx(name, flags);
            var clicked = ImGui.IsItemClicked();

            RenderOtherTabs(style);

            if (open) {
                foreach (var innerStyle in apply.Styles) {
                    RenderStyleImgui(innerStyle, ref id);
                }

                ImGui.TreePop();
            } else {
                id += apply.Styles.Count; // increase the id to make sure that they stay consistent regardless of how many tree nodes are open
            }

            if (clicked) {
                Selected = style;
            }
        } else {
            var flags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanFullWidth;
            if (Selected == style) {
                flags |= ImGuiTreeNodeFlags.Selected;
            }

            var open = ImGui.TreeNodeEx(name, flags);
            var clicked = ImGui.IsItemClicked();

            if (clicked) {
                Selected = style;
            }

            RenderOtherTabs(style);
        }


    }

    private void RenderOtherTabs(Style style) {
        ImGui.TableNextColumn();

        var parent = style.Parent;
        bool gray = false;

        var only = style.Only;
        if (string.IsNullOrWhiteSpace(only) && parent?.Only is [_, ..] parentOnly) {
            only = parentOnly;
            gray = true;
        }

        if (gray)
            ImGui.BeginDisabled();
        ImGui.Text(only);
        if (gray) {
            ImGui.EndDisabled();
            gray = false;
        }
    }
}
