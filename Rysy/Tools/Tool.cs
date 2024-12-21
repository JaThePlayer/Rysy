using ImGuiNET;
using JetBrains.Annotations;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Layers;
using System.Diagnostics.CodeAnalysis;

namespace Rysy.Tools;

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public abstract class Tool {
    public bool UsePersistence { get; set; }

    public HistoryHandler History { get; internal set; }

    public HotkeyHandler HotkeyHandler { get; internal set; }

    public Input Input { get; internal set; }

    public abstract string Name { get; }

    /// <summary>
    /// The group used for storing persistence for this tool. Several tools can share the same group, which will make them share the current layer, search, material, etc.
    /// </summary>
    public abstract string PersistenceGroup { get; }

    /// <summary>
    /// A list of possible layers this tool could use, used in the UI for generating the layer list.
    /// </summary>
    public abstract List<EditorLayer> ValidLayers { get; }

    /// <summary>
    /// A list of possible modes this tool could use, used in the UI for generating the mode list.
    /// </summary>
    public virtual List<ToolMode> ValidModes => ToolMode.DefaultList;
    
    private ToolMode? _mode;
    
    /// <summary>
    /// Gets or sets the currently used layer.
    /// </summary>
    public ToolMode Mode {
        get {
            if (UsePersistence) {
                var name = Persistence.Instance.Get($"{PersistenceGroup}.Mode", ValidModes.FirstOrDefault()?.Name ?? "");

                if (ValidModes.FirstOrDefault(m => m.Name == name) is {} mode)
                    return _mode = mode;
            }

            return _mode ??= ValidModes.FirstOrDefault() 
                             ?? throw new NotImplementedException($"No valid modes for tool {GetType().Name}");
        }
        set {
            _mode = value;
            if (UsePersistence) {
                Persistence.Instance.Set($"{PersistenceGroup}.Mode", value.Name);
            }
            
            CancelInteraction();
            OnLayerChanged();
        }
    }
    
    private EditorLayer? _layer;
    private string? _layerPersistenceKey;
    
    /// <summary>
    /// Gets or sets the currently used layer.
    /// </summary>
    public EditorLayer Layer {
        get {
            if (UsePersistence) {
                _layerPersistenceKey ??= $"{PersistenceGroup}.Layer";
                var name = Persistence.Instance.Get(_layerPersistenceKey, (string?)null);
                if (string.IsNullOrWhiteSpace(name)) {
                    name = ValidLayers.FirstOrDefault()?.Name ?? "";
                    Persistence.Instance.Set(_layerPersistenceKey, name);
                }

                return _layer = EditorLayers.EditorLayerFromName(name);
            }

            return _layer ??= ValidLayers.FirstOrDefault() 
                              ?? throw new NotImplementedException($"No valid layers for tool {GetType().Name}");
        }
        set {
            if (_layer == value)
                return;
            _layer = value;
            if (UsePersistence) {
                Persistence.Instance.Set($"{PersistenceGroup}.Layer", value.Name);
            }
            
            CancelInteraction();
            OnLayerChanged();
        }
    }

    protected virtual void OnLayerChanged() {

    }

    private string _search = "";
    private string SearchPersistenceKey => $"{PersistenceGroup}.{Layer.Name}.Search";
    /// <summary>
    /// Gets or sets the current search filter.
    /// </summary>
    public string Search {
        get => UsePersistence ? Persistence.Instance.Get(SearchPersistenceKey, "") : _search;
        set {
            if (UsePersistence) {
                Persistence.Instance.Set(SearchPersistenceKey, value);
            } else {
                _search = value;
            }
        }
    }

    private object? _material;
    private string PersistenceMaterialKey => GetPersistenceMaterialKeyForLayer(Layer.Name);
    
    public string GetPersistenceMaterialKeyForLayer(string layer) => $"{PersistenceGroup}.{layer}.Material";

    public virtual object? MaterialToPersistenceObj(object? material) => material;
    public virtual object? PersistenceObjToMaterial(object? material) => material;
    
    /// <summary>
    /// Gets or sets the currently selected material.
    /// </summary>
    public object? Material {
        get {
            if (_material is { } mat) {
                return mat is false ? null : mat;
            }

            if (UsePersistence && Persistence.Instance?.Get<object>(PersistenceMaterialKey, null!) is { } persisted) {
                _material = PersistenceObjToMaterial(persisted);
                return persisted;
            }

            _material = false;
            return null;
        }
        set {
            if (UsePersistence)
                Persistence.Instance?.Set(PersistenceMaterialKey, MaterialToPersistenceObj(value));
            _material = value ?? false;
            CancelInteraction();
        }
    }

    private HashSet<string>? _Favorites;
    public HashSet<string>? Favorites {
        get => UsePersistence ? _Favorites ??= Persistence.Instance.Get($"{PersistenceGroup}.{Layer.Name}.Favorites", (HashSet<string>) null!) : _Favorites;
        set {
            if (UsePersistence) {
                Persistence.Instance.Set($"{PersistenceGroup}.{Layer.Name}.Favorites", value);
            }
            _Favorites = value;
        }
    }

    /// <summary>
    /// Adds a new favorite for this group and layer. Use this instead of mutating <see cref="Favorites"/>.
    /// </summary>
    public void ToggleFavorite(string name) {
        var favorites = Favorites;
        if (favorites is null) {
            Favorites = favorites = new();
        }

        if (!favorites.Add(name)) {
            favorites.Remove(name);
        }

        if (UsePersistence) {
            // call the setter which sets up persistence
            Favorites = favorites;
            Persistence.Save(Persistence.Instance);
        }
        
        CachedSearch = null;
    }

    public abstract void Update(Camera camera, Room? room);

    /// <summary>
    /// Renders this tool. Before calling, the sprite batch should be set using currentRoom.StartBatch(camera)
    /// </summary>
    /// <param name="camera"></param>
    /// <param name="room"></param>
    public abstract void Render(Camera camera, Room room);

    /// <summary>
    /// Renders the overlay for this tool. The spritebatch has no transformation matrix, used to draw directly to the screen.
    /// </summary>
    public abstract void RenderOverlay();

    /// <summary>
    /// Called once, when the tool is first created.
    /// </summary>
    public virtual void Init() {

    }

    /// <summary>
    /// Called once, when the tool is unloaded for any reason.
    /// </summary>
    public virtual void Unload() {

    }

    /// <summary>
    /// Called whenever the tool should cancel an interaction (like rectangle drawing, selections, etc),
    /// for example when switching rooms or undoing
    /// </summary>
    public virtual void CancelInteraction() {
        if (UsePersistence)
            _material = null;
    }

    /// <summary>
    /// Initializes hotkeys for this tool
    /// </summary>
    public virtual void InitHotkeys(HotkeyHandler handler) { }

    public abstract IEnumerable<object>? GetMaterials(EditorLayer layer);

    public abstract string GetMaterialDisplayName(EditorLayer layer, object material);

    public abstract string? GetMaterialTooltip(EditorLayer layer, object material);

    public (Color outline, Color fill) GetSelectionColor(Rectangle rect) 
        => GetSelectionColorCore(rect.Size().ToVector2().Length());
    
    private (Color outline, Color fill) GetSelectionColorCore(float len) {
        var outline = ColorHelper.HSVToColor(len.Div(2f).AtMost(70f), 1f, 1f);
        return (outline, outline * 0.3f);
    }
    
    public void DrawSelectionRect(Rectangle rect) {
        var c = GetSelectionColor(rect);
        ISprite.OutlinedRect(rect, c.fill, c.outline, outlineWidth: (int) (1f / EditorState.Camera.Scale).AtLeast(1)).Render();
    }

    protected bool IsEqual(EditorLayer layer, object? currentMaterial, string name) {
        return currentMaterial is { } && GetMaterialDisplayName(layer, currentMaterial) == name;
    }
    
    public Point GetMouseRoomPos(Camera camera, Room? room, Point? pos = default) {
        if (Layer == EditorLayers.Room)
            return camera.ScreenToReal(pos ?? Input.Mouse.Pos);
        
        return room.WorldToRoomPos(camera, pos ?? Input.Mouse.Pos);
    }

    public void RenderGui(Vector2 size, string id = "##ToolMaterialBox") {
        if (!ImGui.BeginChild($"##c_{id}", size.ToNumerics(), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollWithMouse)) {
            return;
        }

        BeginMaterialListGUI(id, size);

        RenderMaterialList(size, out var searchBar);

        EndMaterialListGUI(searchBar);

        ImGui.EndChild();
    }

    public virtual float MaterialListElementHeight() => ImGui.GetTextLineHeightWithSpacing();

    public virtual int MaterialListColumnCount() => UsePersistence ? Persistence.Instance.Get($"{PersistenceGroup}.{Layer.Name}.ColumnCount", 1) : 1;
    

    public virtual object GetGroupKeyForMaterial(object material) => material;


    private Dictionary<object, string> GroupKeyToMainPlacementName = new();
    
    private (object material, string displayName) GetMainPlacementForGroupKey(object key, List<(object material, string displayName)> group) {
        if (!GroupKeyToMainPlacementName.TryGetValue(key, out var targetName)) 
            return group[0];

        foreach (var pair in group) {
            if (pair.displayName == targetName && pair.material is { }) {
                return pair;
            }
        }

        return group[0];
    }
    
    public virtual void RenderMaterialList(Vector2 size, out bool showSearchBar) {
        showSearchBar = true;

        var currentLayer = Layer ??= ValidLayers.FirstOrDefault()!;

        if (currentLayer != CachedLayer) {
            CachedSearch = null;
            CachedLayer = currentLayer;
        }

        var cachedSearch = CachedSearch ??= currentLayer is null ? [] :
            (GetMaterials(currentLayer) ?? [])
            .Select(mat => (mat, GetMaterialDisplayName(currentLayer, mat)))
            .SearchFilter(kv => kv.Item2, Search, Favorites)
            .GroupBy(pair => GetGroupKeyForMaterial(pair.mat))
            .Select(gr => gr.ToList())
            .ToList();
        var rendered = 0;

        var columns = MaterialListColumnCount();

        var elementHeight = MaterialListElementHeight() / columns;
        var elementsVisible = size.Y / elementHeight;
        if (elementsVisible % columns > 0)
            elementsVisible++;
        var skip = (ImGui.GetScrollY() / elementHeight) - 1;

        var totalCount = cachedSearch.Count + (cachedSearch.Count % columns > 0 ? columns + 1 : 0) + 1;
        ImGui.BeginChild(Interpolator.Temp($"##{GetType().Name}_{Layer.Name}"), 
            new(0, Math.Max(GetMaterialListBoxSize(size).Y - ImGui.GetFrameHeightWithSpacing(), totalCount * elementHeight)), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollWithMouse);
        // make sure columns stay consistent
        skip -= skip % columns;
        skip = Math.Min(skip, cachedSearch.Count - elementsVisible);
        if (skip > 0) {
            ImGui.BeginChild((uint) 12347, new(0, skip * elementHeight));
            ImGui.EndChild();
        }

        if (columns > 1)
            ImGui.Columns(columns);

        foreach (var group in cachedSearch) {
            if (rendered < elementsVisible && skip <= 0) {
                rendered++;

                var groupKey = GetGroupKeyForMaterial(group[0].material);
                var first = GetMainPlacementForGroupKey(groupKey, group);
                
                RenderMaterialListElement(first.material, first.displayName);
                
                // draw dropdown for alternate placements
                if (group.Count > 1) {
                    ImGui.SameLine();
                    var style = ImGui.GetStyle();
                    var columnWidth = columns > 1 ? ImGui.GetColumnWidth() : ImGui.GetWindowWidth();
                    
                    var lasty = style.FramePadding.Y;
                    style.FramePadding.Y = (MaterialListElementHeight() - ImGui.GetTextLineHeightWithSpacing()) / 2;
                    ImGui.SetCursorPosX(ImGui.GetColumnOffset() + columnWidth - style.ItemSpacing.X * 2 - style.FramePadding.Y * 2);
                    
                    var comboOpened = ImGui.BeginCombo(Interpolator.Temp($"##{rendered}"), "", ImGuiComboFlags.NoPreview);
                    style.FramePadding.Y = lasty;
                    
                    if (comboOpened) {
                        foreach (var (mat, displayName) in group) {
                            if (RenderMaterialListElement(mat, displayName)) {
                                GroupKeyToMainPlacementName[groupKey] = displayName;
                            }
                        }
                        
                        ImGui.EndCombo();
                    }
                }
                
                if (columns > 1)
                    ImGui.NextColumn();
            }

            skip--;
        }
        if (columns > 1)
            ImGui.Columns();
        ImGui.EndChild();
    }

    public virtual bool AllowSwappingRooms => true;

    private EditorLayer? CachedLayer;
    private List<List<(object material, string displayName)>>? CachedSearch;

    public virtual void ClearMaterialListCache() {
        CachedSearch = null;
        CachedLayer = null;

        Material = null;
    }

    internal Vector2? BeginMaterialListWindow(bool firstGui) {
        if (firstGui) {
            var menubarHeight = ImGuiManager.MenubarHeight;
            var viewport = RysyState.GraphicsDevice.Viewport;
            var size = new NumVector2(ToolHandler.DefaultMaterialListWidth, viewport.Height - menubarHeight);
            ImGui.SetNextWindowPos(new NumVector2(viewport.Width - size.X, menubarHeight), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
        }

        ImGuiManager.PushWindowStyle();
        if (!ImGui.Begin("Material", ImGuiManager.WindowFlagsResizable | ImGuiWindowFlags.NoScrollWithMouse)) {
            return null;
        }
        ImGuiManager.PopWindowStyle();

        return ImGui.GetWindowSize().ToXna();
    }

    protected NumVector2 GetMaterialListBoxSize(Vector2 windowSize) => new(windowSize.X - 10, windowSize.Y - ImGui.GetTextLineHeightWithSpacing() - ImGui.GetFrameHeightWithSpacing() * 1.5f);

    protected void BeginMaterialListGUI(string id, Vector2 windowSize) {
        ImGui.BeginListBox(id, GetMaterialListBoxSize(windowSize));
    }

    protected void EndMaterialListGUI(bool searchBar) {
        ImGui.EndListBox();

        if (searchBar)
            RenderSearchBar();

        //ImGui.End();
    }

    protected void RenderSearchBar() {
        var search = Search;
        // pass the persistence key as the ID to imgui, because otherwise if you had the search bar selected while switching layers/tools,
        // your search would persist to the different layer/tool
        if (ImGui.InputText(Interpolator.Temp($"Search##{SearchPersistenceKey}"), ref search, 512)) {
            Search = search;
            CachedSearch = null;
        }
    }

    /// <summary>
    /// Creates a widget that renders a preview for the given material
    /// </summary>
    protected virtual XnaWidgetDef? GetMaterialPreview(object material) {
        return null;
    }

    /// <summary>
    /// Converts the previously created widget into one that can be used for rendering inside of the tooltip.
    /// This edited widget should be larger.
    /// </summary>
    protected virtual XnaWidgetDef CreateTooltipPreview(XnaWidgetDef materialPreview, object material) {
        var cam = new Camera() {
            Scale = 2f,
        };
        cam.Move(new(-16, -16));

        var upsizedDef = materialPreview with {
            W = 256,
            H = 128,
            Camera = cam,
        };
        return upsizedDef;
    }

    /// <summary>
    /// Renders additional ImGui elements below the tooltip for the given material
    /// </summary>
    protected virtual void RenderMaterialTooltipExtraInfo(object material) {

    }

    /// <summary>
    /// Renders the gui for a given material inside the material list.
    /// Returns whether the element got clicked this frame.
    /// </summary>
    protected virtual bool RenderMaterialListElement(object material, string name) {
        bool ret = false;
        var favorites = Favorites;
        var currentLayer = Layer;
        var currentMaterial = Material;
        var showPlacementIcons = Settings.Instance.ShowPlacementIcons;

        var size = new NumVector2(0, 0);
        var cursorStart = ImGui.GetCursorPos();
        
        if (ImGui.GetColumnIndex() == 0) {
            //cursorStart.X = ImGui.GetColumnOffset();
            //ImGui.SetCursorPos(cursorStart);
        }

        var previewOrNull = showPlacementIcons ? GetMaterialPreview(material) : null;

        if (previewOrNull is { } preview) {
            size.Y = preview.H;
        }
        
        var displayName = name;
        if (ImGui.Selectable(Interpolator.Temp($"##{displayName}"), currentMaterial == material, ImGuiSelectableFlags.AllowDoubleClick | ImGuiSelectableFlags.AllowOverlap, size)) {
            Material = material;
            ret = true;
            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) {
                ToggleFavorite(name);
            }
        }
        if (ImGui.IsItemHovered()) {
            var prevStyles = ImGuiManager.PopAllStyles();
            if (!showPlacementIcons)
                previewOrNull = GetMaterialPreview(material);

            XnaWidgetDef? tooltipPreview = previewOrNull is { } p2 ? CreateTooltipPreview(p2, material) : null;
            tooltipPreview = tooltipPreview is { } p3 ? p3 with { ID = "upsized_preview" } : null;

            var w = (tooltipPreview?.W ?? 256) + ImGui.GetStyle().FramePadding.X * 4;
            ImGui.SetNextWindowSize(new(w.AtLeast(256), 0));
            ImGui.BeginTooltip();

            ImGui.TextWrapped(displayName);
            ImGui.Separator();

            if (GetMaterialTooltip(currentLayer, material) is { } tooltip)
                ImGui.TextWrapped(tooltip);

            if (tooltipPreview is { }) {
                ImGuiManager.XnaWidget(tooltipPreview.Value);
            }

            RenderMaterialTooltipExtraInfo(material);

            ImGui.EndTooltip();
            ImGuiManager.PushAllStyles(prevStyles);
        }

        ImGui.SetCursorPos(cursorStart);
        if (previewOrNull is { } p && showPlacementIcons) {
            ImGuiManager.XnaWidget(p);
            ImGui.SameLine();
        }

        
        // center the text
        cursorStart.Y = ImGui.GetCursorPos().Y;
        if (showPlacementIcons)
            cursorStart.Y += previewOrNull?.H / 4 ?? 0;
        ImGui.SetCursorPosY(cursorStart.Y);
        ImGui.Text(favorites is { } && favorites.Contains(name) ? Interpolator.Temp($"* {displayName}") : displayName);

        return ret;
    }
}
