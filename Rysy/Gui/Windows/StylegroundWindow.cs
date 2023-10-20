using ImGuiNET;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Stylegrounds;

namespace Rysy.Gui.Windows;

public class StylegroundWindow : Window {
    private HistoryHandler History;
    private Map Map;

    private bool firstRender = true;

    private bool FG = false;

    private ComboCache<Placement> PlacementComboCache = new();

    private Dictionary<string, object>? SelectedAlteredValues;

    private readonly ListenableList<Style> Selections;

    private FormWindow? Form;
    // the style used for the form window
    private Style? FormStyle;

    private HotkeyHandler HotkeyHandler;

    public StylegroundWindow(HistoryHandler history) : base("rysy.stylegrounds.windowName".Translate(), new(1200, 800)) {
        History = history;

        Map = EditorState.Map!;
        EditorState.OnMapChanged += () => {
            //Map = EditorState.Map!;
            //if (Map is null) {
                RemoveSelf();
            //}
        };

        var historyHook = () => {
            Form?.ReevaluateChanged(FormStyle!.Data.Inner);
            Map.Style.ClearFakePreviewData();
        };
        history.OnApply += historyHook;
        history.OnUndo += historyHook;
        SetRemoveAction((w) => {
            History.OnApply -= historyHook;
            History.OnUndo -= historyHook;
            Map.Style.ClearFakePreviewData();
        });

        NoSaveData = false;

        HotkeyHandler = new(Input.Global, updateInImgui: true);
        HotkeyHandler.AddHotkeyFromSettings("delete", "delete", () => EditAll(s => Delete(s)));
        HotkeyHandler.AddHotkeyFromSettings("stylegrounds.moveUp", "up", () => EditAll(s => Move(s, -1)), HotkeyModes.OnHoldSmoothInterval);
        HotkeyHandler.AddHotkeyFromSettings("stylegrounds.moveDown", "down", () => EditAll(s => Move(s, 1), true), HotkeyModes.OnHoldSmoothInterval);
        HotkeyHandler.AddHotkeyFromSettings("stylegrounds.moveUpInFolders", "up+shift", () => EditAll(s => MoveInOutFolder(s, -1)), HotkeyModes.OnHoldSmoothInterval);
        HotkeyHandler.AddHotkeyFromSettings("stylegrounds.moveDownInFolders", "down+shift", () => EditAll(s => MoveInOutFolder(s, 1), true), HotkeyModes.OnHoldSmoothInterval);
        HotkeyHandler.AddHotkeyFromSettings("copy", "ctrl+c", CopySelections);
        HotkeyHandler.AddHotkeyFromSettings("paste", "ctrl+v", PasteSelections);
        HotkeyHandler.AddHotkeyFromSettings("cut", "ctrl+x", CutSelections);

        Selections = new();
        Selections.OnChanged += () => {
            if (Selections is [var main, ..]) {
                CreateForm(main);
            } else {
                Form = null;
            }
        };
    }

    private void CopySelections() {
        if (Selections is [])
            return;

        Input.Clipboard.SetAsJson(Selections.Select(s => s.Pack()).ToArray());
    }

    private void PasteSelections() {
        if (Input.Clipboard.TryGetFromJson<BinaryPacker.Element[]>() is not { } data) {
            return;
        }

        var newStyles = data.Select(Style.FromElement).ToList();
        if (Selections is [var first, ..]) {
            var styles = GetStyleListContaining(first);
            var folder = GetFolderContaining(first);

            var startIdx = styles.IndexOf(first) - 1;
            var actions = newStyles.Select(s => CreateAddAction(s, styles, folder, startIdx.SnapBetween(0, styles.Count - 1)));

            History.ApplyNewAction(actions);
        } else {
            var styles = GetStyleListContaining();

            var actions = newStyles.Select(s => CreateAddAction(s, styles, null));

            History.ApplyNewAction(actions);
        }

        Selections.Clear();
        Selections.AddAll(newStyles);
    }

    private void CutSelections() {
        CopySelections();
        EditAll(Delete);
    }

    private void EditAll(Func<Style, IHistoryAction?> styleToAction, bool descending = false) {
        RysyEngine.OnEndOfThisFrame += () => {
            var ordered = descending
            ? Selections.OrderByDescending(s => GetStyleListContaining(s).IndexOf(s))
            : Selections.OrderBy(s => GetStyleListContaining(s).IndexOf(s));

            History.ApplyNewAction(ordered.Select(s => styleToAction(s)));
        };
    }

    private IHistoryAction CreateAddAction(Style newStyle, IList<Style> styles, StyleFolder? folder, int? index = null) {
        return new AddStyleAction(styles, newStyle, index, folder).WithHook(onUndo: () => Selections.Remove(newStyle));
    }

    private void Add(Style newStyle, IList<Style> styles, StyleFolder? folder) {
        History.ApplyNewAction(CreateAddAction(newStyle, styles, folder));
        SetOrAddSelection(newStyle);
    }

    private IHistoryAction? Move(Style? toMove, int offset) {
        if (toMove is null)
            return null;

        var styles = GetStyleListContaining(toMove);
        var idx = styles.IndexOf(toMove);

        while (Selections.Contains(styles.ElementAtOrDefault(idx + offset))) {
            offset += int.Sign(offset);
        }

        return new ReorderStyleAction(styles, toMove, offset);
    }

    public IHistoryAction? MoveInOutFolder(Style? toMove, int offset) {
        if (toMove is null)
            return null;

        var styles = GetStyleListContaining(toMove);
        var indexInThisList = styles.IndexOf(toMove);
        var parent = toMove.Parent;

        if (toMove is not StyleFolder { CanBeNested: false } && styles.ElementAtOrDefault(indexInThisList + offset) is StyleFolder folderInThisList) {
            return new MoveStyleIntoFolderAction(toMove, folderInThisList, styles, offset > 0);
        }

        if (parent is { } && (indexInThisList + offset < 0 || indexInThisList + offset >= styles.Count)) {
            var newFolder = GetFolderContaining(parent);
            var newStyles = GetStyleListContaining(parent);

            return new MoveStyleOutOfFolderAction(toMove, newStyles, newFolder, parent, offset < 0);
        }

        return Move(toMove, offset);
    }

    private IHistoryAction? Delete(Style? toRemove) {
        if (toRemove is null)
            return null;

        Selections.Remove(toRemove);

        return new RemoveStyleAction(GetStyleListContaining(toRemove), toRemove, GetFolderContaining(toRemove));
    }

    public static FieldList GetFields(Style main) {
        var fieldInfo = EntityRegistry.GetFields(main.Name);

        var fields = Style.GetDefaultFields();
        var order = new List<string>(fields.Order!(main));

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

        var tooltipKeyPrefix = $"style.effects.{main.Name}.description";
        var nameKeyPrefix = $"style.effects.{main.Name}.attribute";
        var defaultTooltipKeyPrefix = $"style.effects.default.description";
        var defaultNameKeyPrefix = $"style.effects.default.attribute";

        foreach (var (name, f) in fields) {
            f.Tooltip ??= name.TranslateOrNull(tooltipKeyPrefix) ?? name.TranslateOrNull(defaultTooltipKeyPrefix);
            f.NameOverride ??= name.TranslateOrNull(nameKeyPrefix) ?? name.TranslateOrNull(defaultNameKeyPrefix);
        }

        return fields.Ordered(order);
    }

    private StyleFolder? GetFolderContaining(Style? style) {
        return style switch {
            { Parent: { } parent } => parent,
            _ => null,
        };
    }

    private IList<Style> GetStyleListContaining(Style? style = null)
        => style is { Parent: { } parent }
        ? parent.Styles
        : FG ? Map.Style.Foregrounds : Map.Style.Backgrounds;

    private void CreateForm(Style? style) {
        if (style is null) {
            Form = null;
            return;
        }

        var fields = GetFields(style);

        var form = new FormWindow(fields, style.Name);
        form.Exists = style.Data.Has;
        form.OnChanged = (edited) => {
            var action = Selections.Select(s => new ChangeStylegroundAction(s, edited)).MergeActions();

            History.ApplyNewAction(action);
            SelectedAlteredValues = null;
        };
        form.OnLiveUpdate = (edited) => {
            SelectedAlteredValues = edited;
        };

        FormStyle = style;
        Form = form;
        Map.Style.ClearFakePreviewData();
        SelectedAlteredValues = null;
    }

    private string GetPlacementName(Placement placement) {
        var prefix = $"style.effects.{placement.SID}.name";
        var name = placement.Name.TranslateOrNull(prefix)
            ?? LangRegistry.TranslateOrNull(prefix)
            ?? placement.SID!.Split('/')[^1].Humanize();

        if (placement.GetDefiningMod() is { } mod) {
            return $"{name} [{mod.Name}]";
        }

        return name;
    }

    private void SetOrAddSelection(Style style) {
        if (Input.Global.Keyboard.Shift()) {
            if (!Selections.Contains(style))
                Selections.Add(style);
        } else {
            Selections.Clear();
            Selections.Add(style);
        }
    }

    private void RenderAddNewEntry(StyleFolder? folder) {
        ImGuiManager.PushNullStyle();
        ImGui.TreeNodeEx("New...", ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanFullWidth);
        ImGuiManager.PopNullStyle();

        var hashString = folder?.GetHashCode().ToString(CultureInfo.InvariantCulture) ?? "";
        var id = $"new_{hashString}";
        ImGui.OpenPopupOnItemClick(id, ImGuiPopupFlags.MouseButtonLeft);

        if (ImGui.BeginPopupContextWindow(id, ImGuiPopupFlags.NoOpenOverExistingPopup | ImGuiPopupFlags.MouseButtonMask)) {
            var placements = EntityRegistry.StylegroundPlacements;
            ImGuiManager.List(placements, GetPlacementName, PlacementComboCache, (pl) => {
                var newStyle = Style.FromPlacement(pl);
                var styles = folder?.Styles ?? GetStyleListContaining();

                Add(newStyle, styles, folder);

                ImGui.CloseCurrentPopup();
            }, new() { "Parallax" });

            ImGui.EndPopup();
        }
    }

    private void RenderAddNewFolder() {
        ImGuiManager.PushNullStyle();
        ImGui.TreeNodeEx("New Folder...", ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanFullWidth);
        ImGuiManager.PopNullStyle();

        if (!ImGui.IsItemClicked(ImGuiMouseButton.Left)) {
            return;
        }

        var newStyle = Style.FromName("apply");

        Add(newStyle, GetStyleListContaining(), null);
    }

    protected override void Render() {
        if (ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows) && !ImGui.GetIO().WantCaptureKeyboard) {
            HotkeyHandler.Update();
        }

        var size = ImGui.GetWindowSize();

        ImGui.Columns(2);

        if (firstRender)
            ImGui.SetColumnWidth(0, size.X / 3);

        if (ImGui.BeginTabBar("Layer")) {
            if (ImGui.BeginTabItem("BG")) {
                if (FG) {
                    Selections.Clear();
                }
                FG = false;
                RenderList(FG);

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("FG")) {
                if (!FG) {
                    Selections.Clear();
                }
                FG = true;
                RenderList(FG);

                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.NextColumn();

        var previewW = (int) ImGui.GetColumnWidth();
        ImGuiManager.XnaWidget("styleground_preview", previewW, 300, () => {
            if (Selections is [var selected, ..]) {
                IEnumerable<ISprite> sprites = Array.Empty<ISprite>();
                try {
                    if (SelectedAlteredValues is { } altered) {
                        selected.FakePreviewData = new(selected.Data.SID, selected.Data.Inner.CreateMerged(SelectedAlteredValues));
                        sprites = selected.GetPreviewSprites().ToList();
                    } else {
                        sprites = selected.GetPreviewSprites();
                    }
                } finally {
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
        if (!ImGui.BeginChild("list", new(ImGui.GetColumnWidth() - ImGui.GetStyle().FramePadding.X * 2, ImGui.GetWindowHeight() - 100f)))
            return;

        var flags = ImGuiManager.TableFlags;
        var textBaseWidth = ImGui.CalcTextSize("A").X;
        var styles = fg ? Map.Style.Foregrounds : Map.Style.Backgrounds;

        if (!ImGui.BeginTable("Styles", 2, flags)) {
            ImGui.EndChild();
            return;
        }

        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Rooms", ImGuiTableColumnFlags.WidthFixed, textBaseWidth * 12f);
        ImGui.TableHeadersRow();

        foreach (var style in styles) {
            RenderStyleImgui(style);
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        RenderAddNewEntry(null);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        RenderAddNewFolder();

        ImGui.EndTable();
        ImGui.EndChild();
    }

    private void RenderStyleImgui(Style style) {
        var id = style.GetHashCode();

        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        if (style is StyleFolder apply) {
            var flags = ImGuiTreeNodeFlags.SpanFullWidth;
            if (Selections.Contains(style)) {
                flags |= ImGuiTreeNodeFlags.Selected;
            }

            var open = ImGui.TreeNodeEx($"##{id}", flags);
            var clicked = ImGui.IsItemClicked();
            AddStyleContextWindow(style, id);

            ImGui.SameLine();
            ImGui.Text(style.DisplayName);

            RenderOtherTabs(style);

            if (open) {
                foreach (var innerStyle in apply.Styles) {
                    RenderStyleImgui(innerStyle);
                }

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                RenderAddNewEntry(apply);

                ImGui.TreePop();
            }

            if (clicked) {
                SetOrAddSelection(style);
            }
        } else {
            var flags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanFullWidth;
            if (Selections.Contains(style)) {
                flags |= ImGuiTreeNodeFlags.Selected;
            }

            var open = ImGui.TreeNodeEx($"##{id}", flags);
            var clicked = ImGui.IsItemClicked();
            if (clicked) {
                SetOrAddSelection(style);
            }
            AddStyleContextWindow(style, id);

            ImGui.SameLine();
            ImGui.Text(style.DisplayName);

            RenderOtherTabs(style);
        }
    }

    private void AddStyleContextWindow(Style style, int id) {
        ImGui.OpenPopupOnItemClick($"style_ctx_{id}", ImGuiPopupFlags.MouseButtonRight);

        if (ImGui.BeginPopupContextWindow($"style_ctx_{id}", ImGuiPopupFlags.NoOpenOverExistingPopup | ImGuiPopupFlags.MouseButtonMask)) {
            if (ImGui.Button("Remove")) {
                RysyEngine.OnEndOfThisFrame += () => History.ApplyNewAction(Delete(style));
                ImGui.CloseCurrentPopup();
            }

            if (ImGui.Button("Clone")) {
                RysyEngine.OnEndOfThisFrame += () => {
                    var newStyle = Style.FromElement(style.Pack());
                    var styles = GetStyleListContaining(style);
                    var folder = GetFolderContaining(style);

                    Add(newStyle, styles, folder);
                };

                ImGui.CloseCurrentPopup();
            }


            ImGui.EndPopup();
        }
    }

    private void RenderOtherTabs(Style style) {
        ImGui.TableNextColumn();

        var parent = style.Parent;
        bool gray = false;

        var only = style.Only;
        if (only.IsNullOrWhitespace()) {
            gray = true;
            if (parent?.Only is { Length: > 0 } parentOnly) {
                only = parentOnly;
            }
        }

        if (gray)
            ImGui.BeginDisabled();
        ImGui.Text(only ?? "*");
        if (gray) {
            ImGui.EndDisabled();
            gray = false;
        }
    }
}
