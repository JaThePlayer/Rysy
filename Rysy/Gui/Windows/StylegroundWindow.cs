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

    private readonly ListenableList<Style> Selections;

    private FormWindow? Form;
    // the style used for the form window
    private Style? FormStyle;

    private HotkeyHandler HotkeyHandler;
    
    private IEnumerable<ISprite>? _previewSprites;

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

        HotkeyHandler = new(Input.Global, HotkeyHandler.ImGuiModes.Ignore);
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
            if (Selections is [var main]) {
                CreateForm(main);
            } else {
                RemoveForm();
            }
        };
    }

    private void RemoveForm() {
        if (Form is { }) {
            Form = null;
            FormStyle?.Data.SetOverlay(null);
            FormStyle = null;
        }
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
        RysyState.OnEndOfThisFrame += () => {
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
        var fieldInfo = EntityRegistry.GetFields(main.Name, RegisteredEntityType.Style);

        var fields = Style.GetDefaultFields();
        fields.SetHiddenFields(fieldInfo.GetDynamicallyHiddenFields);
        var order = new List<string>(fields.Order!(main));
        
        if (main is StyleFolder) {
            fields.Add(StyleFolder.EditorNameDataKey, Fields.String(null!).AllowNull().ConvertEmptyToNull());
            order.Insert(order.Count - 2, StyleFolder.EditorNameDataKey);
        }

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

        // Fake fields added by lonn, completely useless
        fields.Remove("fg");
        fields.Remove("name");

        var tooltipKeyPrefix = $"style.effects.{main.Name}.description";
        var nameKeyPrefix = $"style.effects.{main.Name}.attribute";
        var defaultTooltipKeyPrefix = $"style.effects.default.description";
        var defaultNameKeyPrefix = $"style.effects.default.attribute";

        fields.AddTranslations(tooltipKeyPrefix, nameKeyPrefix, defaultTooltipKeyPrefix, defaultNameKeyPrefix);

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
        RemoveForm();
        
        if (style is null) {
            return;
        }

        FormStyle?.Data.SetOverlay(null);
        
        var fields = GetFields(style);

        var form = new FormWindow(fields, style.Name);
        form.Exists = style.Data.Has;
        form.OnChanged = (edited) => {
            var action = Selections.Select(s => new ChangeStylegroundAction(s, edited)).MergeActions();

            History.ApplyNewAction(action);
            _previewSprites = null;
        };
        form.OnLiveUpdate = (edited) => {
            style.Data.SetOverlay(edited);
            _previewSprites = null;
        };

        FormStyle = style;
        Form = form;
        Map.Style.ClearFakePreviewData();
        _previewSprites = null;
    }

    public override void RemoveSelf() {
        base.RemoveSelf();
        
        RemoveForm();
    }

    private string GetPlacementName(Placement placement) {
        var prefix = $"style.effects.{placement.SID}.name";
        var name = placement.Name.TranslateOrNull(prefix)
            ?? LangRegistry.TranslateOrNull(prefix)
            ?? placement.SID!.Split('/')[^1].Humanize();

        if (placement.GetDefiningMod() is { } mod) {
            return $"{name} [{mod.DisplayName}]";
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
        ImGui.TreeNodeEx("rysy.new".Translate(), ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanFullWidth);
        ImGuiManager.PopNullStyle();

        var hashString = folder?.GetHashCode().ToString(CultureInfo.InvariantCulture) ?? "";
        var id = $"new_{hashString}";
        ImGui.OpenPopupOnItemClick(id, ImGuiPopupFlags.MouseButtonLeft);

        if (ImGui.BeginPopupContextWindow(id, ImGuiPopupFlags.NoOpenOverExistingPopup | ImGuiPopupFlags.MouseButtonRight)) {
            var placements = FG ? EntityRegistry.FgStylegroundPlacements : EntityRegistry.BgStylegroundPlacements;
            ImGuiManager.List(placements, GetPlacementName, PlacementComboCache, (pl) => {
                var newStyle = Style.FromPlacement(pl);
                var styles = folder?.Styles ?? GetStyleListContaining();

                Add(newStyle, styles, folder);

                ImGui.CloseCurrentPopup();
            }, favorites: ["Parallax"]);

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

        var previewW = Math.Min((int) ImGui.GetColumnWidth(), 320); 
        
        ImGuiManager.XnaWidget("styleground_preview", previewW, 180, () => {
            if (Selections is [var selected]) {
                if (_previewSprites is null) {
                    try {
                        _previewSprites = selected.GetPreviewSprites().ToList();
                    } catch (Exception) {
                        _previewSprites = [];
                    }
                }


                var ctx = SpriteRenderCtx.Default(true);
                foreach (var sprite in _previewSprites) {
                    sprite.Render(ctx);
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
        var popupId = Interpolator.Temp($"style_ctx_{id}");
        ImGui.OpenPopupOnItemClick(popupId, ImGuiPopupFlags.MouseButtonRight);

        if (ImGui.BeginPopupContextWindow(popupId, ImGuiPopupFlags.NoOpenOverExistingPopup | ImGuiPopupFlags.MouseButtonRight)) {
            if (ImGui.Button("Remove")) {
                RysyState.OnEndOfThisFrame += () => History.ApplyNewAction(Delete(style));
                ImGui.CloseCurrentPopup();
            }

            if (ImGui.Button("Clone")) {
                RysyState.OnEndOfThisFrame += () => {
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

        var only = style.Attr("only", null!); // See if the styleground provides its own 'only'
        switch (only)
        {
            case "":
                // Empty string makes the styleground never visible, most likely map maker error.
                // While its not possible to make such a styleground in Rysy (as it will be turned into null),
                // other map editors may allow for it.
                ImGuiManager.PushInvalidStyle();
                ImGui.Text("<NONE>");
                ImGuiManager.PopInvalidStyle();
                break;
            case null:
                // The styleground doesn't define its own 'only' field, but it may be inherited from a folder
                ImGui.BeginDisabled();
                ImGui.Text(style.Only ?? "<NULL, how?>");
                ImGui.EndDisabled();
                break;
            default:
                ImGui.Text(only);
                break;
        }
    }
}
