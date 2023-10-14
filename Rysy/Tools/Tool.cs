using ImGuiNET;
using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Helpers;
using Rysy.History;
using Rysy.Scenes;
using System;

namespace Rysy.Tools;

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
    public abstract List<string> ValidLayers { get; }


    private string? _Layer;
    /// <summary>
    /// Gets or sets the currently used layer.
    /// </summary>
    public string Layer {
        get => UsePersistence 
            ? Persistence.Instance.Get($"{PersistenceGroup}.Layer", ValidLayers.FirstOrDefault() ?? "")
            : _Layer ??= ValidLayers.First();
        set {
            if (UsePersistence)
                Persistence.Instance.Set($"{PersistenceGroup}.Layer", value);
            else
                _Layer = value;

            CancelInteraction();
            OnLayerChanged();
        }
    }

    protected virtual void OnLayerChanged() {

    }

    private string _Search = "";
    private string SearchPersistenceKey => $"{PersistenceGroup}.{Layer}.Search";
    /// <summary>
    /// Gets or sets the current search filter.
    /// </summary>
    public string Search {
        get => UsePersistence ? Persistence.Instance.Get(SearchPersistenceKey, "") : _Search;
        set {
            if (UsePersistence) {
                Persistence.Instance.Set(SearchPersistenceKey, value);
            } else {
                _Search = value;
            }
        }
    }

    private object? _Material;
    /// <summary>
    /// Gets or sets the currently selected material.
    /// </summary>
    public object? Material {
        get => UsePersistence ? Persistence.Instance?.Get($"{PersistenceGroup}.{Layer}.Material", (object) null!) : _Material;
        set {
            if (UsePersistence)
                Persistence.Instance?.Set($"{PersistenceGroup}.{Layer}.Material", value);
            else
                _Material = value;

            CancelInteraction();
        }
    }

    private HashSet<string>? _Favorites;
    public HashSet<string>? Favorites {
        get => UsePersistence ? Persistence.Instance.Get($"{PersistenceGroup}.{Layer}.Favorites", (HashSet<string>) null!) : _Favorites;
        set {
            if (UsePersistence) {
                Persistence.Instance.Set($"{PersistenceGroup}.{Layer}.Favorites", value);
            } else {
                _Favorites = value;
            }
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

        if (UsePersistence)
            Persistence.Save(Persistence.Instance);
        CachedSearch = null;
    }

    public abstract void Update(Camera camera, Room room);

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

    }

    /// <summary>
    /// Initializes hotkeys for this tool
    /// </summary>
    public virtual void InitHotkeys(HotkeyHandler handler) { }

    public abstract IEnumerable<object>? GetMaterials(string layer);

    public abstract string GetMaterialDisplayName(string layer, object material);

    public abstract string? GetMaterialTooltip(string layer, object material);

    public static void DrawSelectionRect(Rectangle rect) {
        var c = ColorHelper.HSVToColor(rect.Size().ToVector2().Length().Div(2f).AtMost(70f), 1f, 1f);
        ISprite.OutlinedRect(rect, c * 0.3f, c, outlineWidth: (int) (1f / EditorState.Camera.Scale).AtLeast(1)).Render();
    }

    protected bool IsEqual(string layer, object? currentMaterial, string name) {
        return currentMaterial is { } && GetMaterialDisplayName(layer, currentMaterial) == name;
    }

    public void RenderGui(Vector2 size, string id = "##ToolMaterialBox") {
        if (!ImGui.BeginChild($"##c_{id}", size.ToNumerics(), false, ImGuiWindowFlags.NoScrollWithMouse)) {
            return;
        }

        BeginMaterialListGUI(id, size);

        RenderMaterialList(size, out var searchBar);

        EndMaterialListGUI(searchBar);

        ImGui.EndChild();
    }

    public virtual float MaterialListElementHeight() => ImGui.GetTextLineHeightWithSpacing();

    public virtual int MaterialListColumnCount() => UsePersistence ? Persistence.Instance.Get($"{PersistenceGroup}.{Layer}.ColumnCount", 1) : 1;

    public virtual void RenderMaterialList(Vector2 size, out bool showSearchBar) {
        showSearchBar = true;

        var currentLayer = Layer;
        var currentMaterial = Material;
        var favorites = Favorites;

        if (currentLayer != CachedLayer) {
            CachedSearch = null;
            CachedLayer = currentLayer;
        }

        var cachedSearch = CachedSearch ??= (GetMaterials(currentLayer) ?? new List<object>()).Select(mat => (mat, GetMaterialDisplayName(currentLayer, mat))).SearchFilter(kv => kv.Item2, Search, Favorites).ToList();
        var rendered = 0;

        var columns = MaterialListColumnCount();

        var elementHeight = MaterialListElementHeight() / columns;
        var elementsVisible = size.Y / elementHeight;
        if (elementsVisible % columns > 0)
            elementsVisible++;
        var skip = (ImGui.GetScrollY() / elementHeight) - 1;

        var totalCount = cachedSearch.Count + (cachedSearch.Count % columns > 0 ? columns + 1 : 0) + 1;
        ImGui.BeginChild($"##{GetType().Name}_{Layer}", 
            new(0, Math.Max(GetMaterialListBoxSize(size).Y - ImGui.GetFrameHeightWithSpacing(), totalCount * elementHeight)), false, ImGuiWindowFlags.NoScrollWithMouse);
        skip = Math.Min(skip, cachedSearch.Count - elementsVisible);
        if (skip > 0) {
            ImGui.BeginChildFrame((uint) 12347, new(0, skip * elementHeight));
            ImGui.EndChildFrame();
        }

        if (columns > 1)
            ImGui.Columns(columns);

        foreach (var (mat, name) in cachedSearch) {
            if (rendered < elementsVisible && skip <= 0) {
                rendered++;
                RenderMaterialListElement(mat, name);
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

    private string? CachedLayer;
    private List<(object mat, string)>? CachedSearch;

    public void ClearMaterialListCache() {
        CachedSearch = null;
        CachedLayer = null;

        Material = null;
    }

    internal Vector2? BeginMaterialListWindow(bool firstGui) {
        if (firstGui) {
            var menubarHeight = ImGuiManager.MenubarHeight;
            var viewport = RysyEngine.Instance.GraphicsDevice.Viewport;
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

    protected NumVector2 GetMaterialListBoxSize(Vector2 windowSize) => new(windowSize.X - 10, windowSize.Y - ImGui.GetTextLineHeightWithSpacing() * 3);

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
        if (ImGui.InputText($"##{SearchPersistenceKey}", ref search, 512, ImGuiInputTextFlags.AlwaysOverwrite)) {
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
    /// </summary>
    protected virtual void RenderMaterialListElement(object material, string name) {
        var favorites = Favorites;
        var currentLayer = Layer;
        var currentMaterial = Material;
        var showPlacementIcons = Settings.Instance.ShowPlacementIcons;

        var size = new NumVector2(0, 0);
        var cursorStart = ImGui.GetCursorPos();
        
        if (ImGui.GetColumnIndex() == 0) {
            cursorStart.X = ImGui.GetColumnOffset();
            ImGui.SetCursorPos(cursorStart);
        }

        var previewOrNull = showPlacementIcons ? GetMaterialPreview(material) : null;

        if (previewOrNull is { } preview) {
            size.Y = preview.H;
        }

        var displayName = favorites is { } && favorites.Contains(name) ? $"* {name}" : name;
        if (ImGui.Selectable($"##{displayName}", IsEqual(currentLayer, currentMaterial, name), ImGuiSelectableFlags.AllowDoubleClick | ImGuiSelectableFlags.AllowItemOverlap, size)) {
            Material = material;

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
        ImGui.Text(displayName);
    }
}
