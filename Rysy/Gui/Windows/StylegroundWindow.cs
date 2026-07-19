using Hexa.NET.ImGui;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Stylegrounds;

namespace Rysy.Gui.Windows;

public class StylegroundWindow : Window {
    private readonly DragDropCtx<Style> _dragDropCtx = new("styles");
    
    private IHistoryHandler _history;
    private Map _map;

    private bool _firstRender = true;

    private bool _fg = false;

    private ComboCache<Placement> _placementComboCache = new();

    private readonly ListenableList<Style> _selections;

    private FormWindow? _form;
    // the style used for the form window
    private Style? _formStyle;

    private HotkeyHandler _hotkeyHandler;
    
    private IEnumerable<ISprite>? _previewSprites;

    private readonly FakeBackgroundStyle _fakeBackgroundStyle;

    public StylegroundWindow(EditorState editorState, IHistoryHandler history) : base("rysy.stylegrounds.windowName".Translate(), new(1200, 800)) {
        _history = history;

        _map = editorState.Map!;
        editorState.OnMapChanged += () => {
            //Map = EditorState.Map!;
            //if (Map is null) {
                RemoveSelf();
            //}
        };

        var historyHook = () => {
            _form?.ReevaluateChanged(_formStyle!.Data.Inner);
            _map.Style.ClearFakePreviewData();
        };
        history.OnApply += historyHook;
        history.OnUndo += historyHook;
        SetRemoveAction((w) => {
            _history.OnApply -= historyHook;
            _history.OnUndo -= historyHook;
            _map.Style.ClearFakePreviewData();
        });

        NoSaveData = false;

        _hotkeyHandler = new(Input.Global, HotkeyHandler.ImGuiModes.Ignore);
        _hotkeyHandler.AddHotkeyFromSettings("delete", "delete", () => EditAll(s => Delete(s)));
        _hotkeyHandler.AddHotkeyFromSettings("stylegrounds.moveUp", "up", () => EditAll(s => Move(s, -1)), HotkeyModes.OnHoldSmoothInterval);
        _hotkeyHandler.AddHotkeyFromSettings("stylegrounds.moveDown", "down", () => EditAll(s => Move(s, 1), true), HotkeyModes.OnHoldSmoothInterval);
        _hotkeyHandler.AddHotkeyFromSettings("stylegrounds.moveUpInFolders", "up+shift", () => EditAll(s => MoveInOutFolder(s, -1)), HotkeyModes.OnHoldSmoothInterval);
        _hotkeyHandler.AddHotkeyFromSettings("stylegrounds.moveDownInFolders", "down+shift", () => EditAll(s => MoveInOutFolder(s, 1), true), HotkeyModes.OnHoldSmoothInterval);
        _hotkeyHandler.AddHotkeyFromSettings("copy", "ctrl+c", CopySelections);
        _hotkeyHandler.AddHotkeyFromSettings("paste", "ctrl+v", PasteSelections);
        _hotkeyHandler.AddHotkeyFromSettings("cut", "ctrl+x", CutSelections);

        _selections = new();
        _selections.OnChanged += _ => {
            if (_selections is [var main]) {
                CreateForm(main);
            } else {
                RemoveForm();
            }
        };

        _fakeBackgroundStyle = new FakeBackgroundStyle(_map);
    }

    IEnumerable<FakeStyle> GetFakeStyles(bool fg) => fg ? [] : [ _fakeBackgroundStyle ];

    private void RemoveForm() {
        if (_form is { }) {
            _form = null;
            _formStyle?.Data.SetOverlay(null);
            _formStyle = null;
        }
    }

    private void CopySelections() {
        if (_selections is [])
            return;

        Input.Clipboard.SetAsJson(_selections.Select(s => s.Pack()).ToArray());
    }

    private void PasteSelections() {
        if (!Input.Clipboard.TryGetFromJson<BinaryPacker.Element[]>(out var data)) {
            return;
        }

        var newStyles = data.Select(Style.FromElement).ToList();
        if (_selections is [var first, ..]) {
            var styles = GetStyleListContaining(first);
            var folder = GetFolderContaining(first);

            var startIdx = styles.IndexOf(first) - 1;
            var actions = newStyles.Select(s => CreateAddAction(s, styles, folder, startIdx.SnapBetween(0, styles.Count - 1)));

            _history.ApplyNewAction(actions);
        } else {
            var styles = GetStyleListContaining();

            var actions = newStyles.Select(s => CreateAddAction(s, styles, null));

            _history.ApplyNewAction(actions);
        }

        _selections.Clear();
        _selections.AddAll(newStyles);
    }

    private void CutSelections() {
        CopySelections();
        EditAll(Delete);
    }

    private void EditAll(Func<Style, IHistoryAction?> styleToAction, bool descending = false) {
        RysyState.OnEndOfThisFrame += () => {
            var ordered = descending
            ? _selections.OrderByDescending(s => GetStyleListContaining(s).IndexOf(s))
            : _selections.OrderBy(s => GetStyleListContaining(s).IndexOf(s));

            _history.ApplyNewAction(ordered.Select(s => styleToAction(s)));
        };
    }

    private IHistoryAction CreateAddAction(Style newStyle, IList<Style> styles, StyleFolder? folder, int? index = null) {
        return new AddStyleAction(styles, newStyle, index, folder).WithHook(onUndo: () => _selections.Remove(newStyle));
    }

    private void Add(Style newStyle, IList<Style> styles, StyleFolder? folder) {
        _history.ApplyNewAction(CreateAddAction(newStyle, styles, folder));
        SetOrAddSelection(newStyle);
    }

    private IHistoryAction? Move(Style? toMove, int offset) {
        if (toMove is null or FakeStyle)
            return null;

        var styles = GetStyleListContaining(toMove);
        var idx = styles.IndexOf(toMove);

        while (_selections.Contains(styles.ElementAtOrDefault(idx + offset))) {
            offset += int.Sign(offset);
        }

        return new ReorderStyleAction(styles, toMove, offset);
    }

    public IHistoryAction? MoveInOutFolder(Style? toMove, int offset) {
        if (toMove is null or FakeStyle)
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
        if (toRemove is null or FakeStyle)
            return null;

        _selections.Remove(toRemove);

        return new RemoveStyleAction(GetStyleListContaining(toRemove), toRemove, GetFolderContaining(toRemove));
    }

    public static FieldList GetFields(Style main) {
        var fieldInfo = EntityRegistry.GetFields(main.Name, RegisteredEntityType.Style);

        var fields = main is FakeStyle ? new FieldList() : Style.GetDefaultFields();
        fields.SetHiddenFields(fieldInfo.GetDynamicallyHiddenFields);
        var order = new List<string>(fields.Order?.Invoke(main) ?? []);
        
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
        : _fg ? _map.Style.Foregrounds : _map.Style.Backgrounds;

    private void CreateForm(Style? style) {
        RemoveForm();
        
        if (style is null) {
            return;
        }

        _formStyle?.Data.SetOverlay(null);
        
        var fields = GetFields(style);

        var form = new FormWindow(fields, style.Name);
        form.Exists = style.Data.Has;
        form.OnChanged = (edited) => {
            var action = _selections.Select(s => new ChangeStylegroundAction(s, edited)).MergeActions();

            _history.ApplyNewAction(action);
            _previewSprites = null;
        };
        form.OnLiveUpdate = (edited) => {
            style.Data.SetOverlay(edited);
            _previewSprites = null;
        };

        _formStyle = style;
        _form = form;
        _map.Style.ClearFakePreviewData();
        _previewSprites = null;
    }

    public override void RemoveSelf() {
        base.RemoveSelf();
        
        RemoveForm();
    }

    private Searchable GetPlacementSearchable(Placement placement) {
        var prefix = $"style.effects.{placement.Sid}.name";
        var name = placement.Name.TranslateOrNull(prefix)
            ?? LangRegistry.TranslateOrNull(prefix)
            ?? placement.Sid!.Split('/')[^1].Humanize();

        return new Searchable(name, placement.GetDefiningMod()) {
            IsFavourite = placement.Name == "parallax",
        };
    }

    private void SetOrAddSelection(Style style) {
        if (Input.Global.Keyboard.Shift()) {
            if (!_selections.Contains(style))
                _selections.Add(style);
        } else if (Input.Global.Keyboard.Ctrl()) {
            _selections.Remove(style);
        } else {
            _selections.Clear();
            _selections.Add(style);
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
            var placements = _fg ? EntityRegistry.FgStylegroundPlacements : EntityRegistry.BgStylegroundPlacements;
            ImGuiManager.List(placements, GetPlacementSearchable, _placementComboCache, pl => {
                var newStyle = Style.FromPlacement(pl);
                var styles = folder?.Styles ?? GetStyleListContaining();

                Add(newStyle, styles, folder);

                ImGui.CloseCurrentPopup();
            });

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
            _hotkeyHandler.Update();
        }

        var size = ImGui.GetContentRegionAvail();

        ImGui.Columns(2);

        if (_firstRender)
            ImGui.SetColumnWidth(0, size.X / 3);

        if (ImGui.BeginTabBar("Layer")) {
            if (ImGui.BeginTabItem("BG")) {
                if (_fg) {
                    _selections.Clear();
                }
                _fg = false;
                RenderList(_fg);

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("FG")) {
                if (!_fg) {
                    _selections.Clear();
                }
                _fg = true;
                RenderList(_fg);

                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.NextColumn();

        var previewW = Math.Min((int) ImGui.GetContentRegionAvail().X, 320); 
        
        ImGuiManager.XnaWidget("styleground_preview", previewW, 180, () => {
            if (_selections is [var selected]) {
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
        _form?.RenderBody();
        ImGui.EndChild();

        ImGui.Columns();

        _firstRender = false;
    }

    private bool AreSelectionsSequentialAndInSameFolder() {
        switch (_selections) {
            case [] or [_]:
                return true;
            case [var first, ..]:
                var currId = -1;
                foreach (var (other, id) in _selections.Select(s => (s, GetStyleListContaining(s).IndexOf(s))).OrderBy(s => s.Item2)) {
                    if (first.Parent != other.Parent)
                        return false;

                    if (currId == -1) {
                        currId = id;
                        continue;
                    }

                    if (id != currId + 1)
                        return false;
                    currId = id;
                }
                
                return true;
            default:
                throw new ArgumentOutOfRangeException(nameof(_selections));
        }
    }

    private void RenderList(bool fg) {
        ImGui.BeginChild("list");

        var flags = ImGuiManager.TableFlags;
        var textBaseWidth = ImGui.CalcTextSize("A").X;
        var styles = fg ? _map.Style.Foregrounds : _map.Style.Backgrounds;

        if (!ImGui.BeginTable("Styles", 2, flags)) {
            ImGui.EndChild();
            return;
        }

        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Rooms", ImGuiTableColumnFlags.WidthFixed, textBaseWidth * 12f);
        ImGui.TableHeadersRow();

        foreach (var fakeStyle in GetFakeStyles(fg)) {
            RenderStyleImgui(fakeStyle, false);
        }
        
        foreach (var style in styles) {
            RenderStyleImgui(style, canDragDrop: AreSelectionsSequentialAndInSameFolder());
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

    private void RenderStyleImgui(Style style, bool canDragDrop) {
        var id = style.GetHashCode();

        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        if (style is StyleFolder apply) {
            var flags = ImGuiTreeNodeFlags.SpanFullWidth;
            if (_selections.Contains(style)) {
                flags |= ImGuiTreeNodeFlags.Selected;
            }

            var open = ImGui.TreeNodeEx($"##{id}", flags);
            var clicked = ImGui.IsItemClicked();
            AddStyleContextWindow(style, id);
            if (canDragDrop && _dragDropCtx.DragDrop(apply) is { } droppedStyle && !_selections.Contains(apply)) {
                // Dropped a style onto an apply.
                var shouldUseDefaultBehavior = true;
                if (open) {
                    var parentList = GetStyleListContaining(apply);
                    var previousStyle = parentList.ElementAtOrDefault(parentList.IndexOf(apply) - 1);
                    if (_selections.Contains(previousStyle)) {
                        // We're moving a style directly above this apply onto the non-empty apply, move styles into it.
                        EditAll(s => new MoveStyleIntoFolderAction(s, apply, GetStyleListContaining(s), true));
                        shouldUseDefaultBehavior = false;
                    } else if (apply.Styles.Count > 0 && _selections.Contains(apply.Styles[0])) {
                        // We're trying to move the top style upwards, move out of folder
                        EditAll(s => new MoveStyleOutOfFolderAction(s, GetStyleListContaining(apply), apply.Parent, apply, true));
                        shouldUseDefaultBehavior = false;
                    }
                }
                
                if (shouldUseDefaultBehavior)
                    MoveStyleNextTo(droppedStyle, apply);
            }

            ImGui.SameLine();
            ImGui.TextUnformatted(style.DisplayName);

            RenderOtherTabs(style);

            if (open) {
                foreach (var innerStyle in apply.Styles) {
                    // Only allow dragging and dropping inside the folder if the folder isn't currently selected.
                    RenderStyleImgui(innerStyle, canDragDrop && !_selections.Contains(apply));
                }

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                RenderAddNewEntry(apply);
                if (canDragDrop && _dragDropCtx.DragDrop() is { } && !_selections.Contains(apply)) {
                    // Dropped a style onto the "new" button.
                    if (apply.Styles is [.., var lastStyleInApply]) {
                        if (!_selections.Contains(lastStyleInApply)) {
                            EditAll(s => new MoveStyleIntoFolderAction(s, apply, GetStyleListContaining(s), false));
                        } else {
                            // We're trying to move the last style downwards, move out of folder
                            EditAll(s => new MoveStyleOutOfFolderAction(s, GetStyleListContaining(apply), apply.Parent, apply, false));
                        }
                    }
                    else {
                        // Folder is empty, just add the styles
                        EditAll(s => new MoveStyleIntoFolderAction(s, apply, GetStyleListContaining(s), false));
                    }
                }

                ImGui.TreePop();
            }

            if (clicked) {
                SetOrAddSelection(style);
            }
        } else {
            var flags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanFullWidth;
            if (_selections.Contains(style)) {
                flags |= ImGuiTreeNodeFlags.Selected;
            }

            var open = ImGui.TreeNodeEx($"##{id}", flags);
            var clicked = ImGui.IsItemClicked();
            if (clicked) {
                SetOrAddSelection(style);
            }
            AddStyleContextWindow(style, id);
            
            if (canDragDrop && _dragDropCtx.DragDrop(style) is { } droppedStyle && !_selections.Contains(style)) {
                MoveStyleNextTo(droppedStyle, style);
            }

            ImGui.SameLine();
            ImGui.TextUnformatted(style.DisplayName);

            RenderOtherTabs(style);
        }
    }

    private void MoveStyleNextTo(Style toMove, Style to) {
        if (toMove == to)
            return;
        
        if (toMove.Parent == to.Parent) {
            var styles = GetStyleListContaining(toMove);
            var idxOfTarget = styles.IndexOf(to);
            var idxOfToMove = styles.IndexOf(toMove);
            var move = (idxOfTarget - idxOfToMove);
            var moveDir = int.Sign(move);
            var nextStyleToCheck = idxOfToMove + moveDir;
            
            while (styles.ElementAtOrDefault(nextStyleToCheck) is {} next && _selections.Contains(next)) {
                nextStyleToCheck += moveDir;
                move -= moveDir;
            }
            for (int i = 0; i < int.Abs(move); i++) {
                EditAll(s => Move(s, moveDir), descending: move > 0);
            }
            
            return;
        }

        if (to.Parent is not null) {
            // Moving into the parent folder
            var toParent = to.Parent;
            var targetStyles = GetStyleListContaining(to);
            var idxOfTargetInItsList = targetStyles.IndexOf(to);
        
            EditAll(s => {
                return new MoveStyleIntoFolderAction(s, toParent, GetStyleListContaining(s), FromTop: idxOfTargetInItsList == 0);
            }, descending: idxOfTargetInItsList == 0);
        } else if (toMove.Parent is not null) {
            // Moving out to no folder
            var fromStyles = toMove.Parent;
            var targetStyles = GetStyleListContaining(to);
            var idxOfTargetInItsList = GetStyleListContaining(toMove).IndexOf(toMove);
        
            EditAll(
                s => new MoveStyleOutOfFolderAction(s, targetStyles, null, fromStyles, FromTop: idxOfTargetInItsList == 0),
                descending: idxOfTargetInItsList != 0);
        }
    }

    private void AddStyleContextWindow(Style style, int id) {
        var popupId = Interpolator.TempU8($"style_ctx_{id}");
        ImGui.OpenPopupOnItemClick(popupId, ImGuiPopupFlags.MouseButtonRight);

        if (style is FakeStyle) {
            return;
        }

        if (ImGui.BeginPopupContextWindow(popupId, ImGuiPopupFlags.NoOpenOverExistingPopup | ImGuiPopupFlags.MouseButtonRight)) {
            ImGui.TextUnformatted(style.DisplayName);
            
            if (ImGui.Button("Remove")) {
                RysyState.OnEndOfThisFrame += () => _history.ApplyNewAction(Delete(style));
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
                // While it's not possible to make such a styleground in Rysy (as it will be turned into null),
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

/// <summary>
/// A style which doesn't exist in the map's actual styleground list, but is still displayed in the styleground list UI.
/// </summary>
internal abstract class FakeStyle : Style;

/// <summary>
/// A fake styleground mapped to the `Map.Style.color` property.
/// </summary>
[CustomEntity("Rysy/FakeBackgroundStyle")]
internal class FakeBackgroundStyle : FakeStyle, IPlaceable {
    private readonly Map _map;

    public FakeBackgroundStyle(Map map) {
        _map = map;
        Name = "Rysy/FakeBackgroundStyle";
        Data = new EntityData(Name, new Dictionary<string, object> {
            ["color"] = map.Style.BackgroundColor.ToString(ColorFormat.Rgb),
            ["only"] = "*",
        });
    }

    private Color BackgroundColor => Data.GetColor("color", Color.Black, ColorFormat.Rgb);

    public override void OnChanged(EntityDataChangeCtx changeCtx) {
       _map.Style.BackgroundColor = BackgroundColor;
    }

    public override bool CanBeInBackground => true;
    public override bool CanBeInForeground => false;

    public override IEnumerable<ISprite> GetPreviewSprites() {
        return ISprite.Rect(PreviewRectangle(), BackgroundColor);
    }

    public static FieldList GetFields() => new FieldList(new {
        color = Fields.Rgb(Color.Black),
    }).SetHiddenFields([ "only" ]);

    public static PlacementList GetPlacements() => [];
}
