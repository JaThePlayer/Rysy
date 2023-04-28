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
    public HistoryHandler History { get; internal set; }

    public HotkeyHandler HotkeyHandler { get; internal set; }

    public abstract string Name { get; }

    /// <summary>
    /// The group used for storing persistence for this tool. Several tools can share the same group, which will make them share the current layer, search, material, etc.
    /// </summary>
    public abstract string PersistenceGroup { get; }

    /// <summary>
    /// A list of possible layers this tool could use, used in the UI for generating the layer list.
    /// </summary>
    public abstract List<string> ValidLayers { get; }

    /// <summary>
    /// Gets or sets the currently used layer.
    /// </summary>
    public string Layer {
        get => Persistence.Instance.Get($"{PersistenceGroup}.Layer", ValidLayers.FirstOrDefault() ?? "");
        set {
            Persistence.Instance.Set($"{PersistenceGroup}.Layer", value);
            CancelInteraction();
        }
    }

    private string SearchPersistenceKey => $"{PersistenceGroup}.{Layer}.Search";
    /// <summary>
    /// Gets or sets the current search filter.
    /// </summary>
    public string Search {
        get => Persistence.Instance.Get(SearchPersistenceKey, "");
        set => Persistence.Instance.Set(SearchPersistenceKey, value);
    }

    /// <summary>
    /// Gets or sets the currently selected material.
    /// </summary>
    public object? Material {
        get => Persistence.Instance?.Get($"{PersistenceGroup}.{Layer}.Material", (object) null!);
        set {
            Persistence.Instance?.Set($"{PersistenceGroup}.{Layer}.Material", value);
            CancelInteraction();
        }
    }

    public HashSet<string>? Favorites {
        get => Persistence.Instance.Get($"{PersistenceGroup}.{Layer}.Favorites", (HashSet<string>) null!);
        set => Persistence.Instance.Set($"{PersistenceGroup}.{Layer}.Favorites", value);
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
        var c = ColorHelper.HSVToColor(rect.Size.ToVector2().Length().Div(2f).AtMost(70f), 1f, 1f);
        ISprite.OutlinedRect(rect, c * 0.3f, c, outlineWidth: (int) (1f / EditorState.Camera.Scale).AtLeast(1)).Render();
    }

    private bool IsEqual(string layer, object? currentMaterial, string name) {
        return currentMaterial is { } && GetMaterialDisplayName(layer, currentMaterial) == name;
    }

    public virtual void RenderGui(EditorScene editor, bool firstGui) {
        RenderMaterialList(editor, firstGui);
    }

    public virtual bool AllowSwappingRooms => true;

    private string? CachedLayer;
    private List<(object mat, string)>? CachedSearch;

    public void ClearMaterialListCache() {
        CachedSearch = null;
        CachedLayer = null;

        Material = null;
    }

    protected void BeginMaterialListGUI(bool firstGui) {
        if (firstGui) {
            var menubarHeight = ImGuiManager.MenubarHeight;
            var viewport = RysyEngine.Instance.GraphicsDevice.Viewport;
            var size = new NumVector2(ToolHandler.DefaultMaterialListWidth, viewport.Height - menubarHeight);
            ImGui.SetNextWindowPos(new NumVector2(viewport.Width - size.X, menubarHeight));
            ImGui.SetNextWindowSize(size);
        }

        ImGuiManager.PushWindowStyle();
        if (!ImGui.Begin("Material", ImGuiManager.WindowFlagsResizable)) {
            return;
        }
        ImGuiManager.PopWindowStyle();

        var windowSize = ImGui.GetWindowSize();
        ImGui.BeginListBox("##ToolMaterialBox", new(windowSize.X - 10, windowSize.Y - ImGui.GetTextLineHeightWithSpacing() * 3));
    }

    protected void EndMaterialListGUI(bool searchBar) {
        ImGui.EndListBox();

        if (searchBar)
            RenderSearchBar();

        ImGui.End();
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

    protected virtual void RenderMaterialListElement(object material, string name) {
        var favorites = Favorites;
        var currentLayer = Layer;
        var currentMaterial = Material;

        var displayName = favorites is { } && favorites.Contains(name) ? $"* {name}" : name;
        if (ImGui.Selectable(displayName, IsEqual(currentLayer, currentMaterial, name), ImGuiSelectableFlags.AllowDoubleClick).WithTooltip(GetMaterialTooltip(currentLayer, material))) {
            Material = material;

            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) {
                ToggleFavorite(name);
            }
        }
    }

    private void RenderMaterialList(EditorScene editor, bool firstGui) {
        var currentLayer = Layer;
        var currentMaterial = Material;
        var favorites = Favorites;

        BeginMaterialListGUI(firstGui);

        if (currentLayer != CachedLayer) {
            CachedSearch = null;
            CachedLayer = currentLayer;
        }

        var cachedSearch = CachedSearch ??= (GetMaterials(currentLayer) ?? new List<object>()).Select(mat => (mat, GetMaterialDisplayName(currentLayer, mat))).SearchFilter(kv => kv.Item2, Search, Favorites).ToList();
        var rendered = 0;


        var skip = (ImGui.GetScrollY()) / ImGui.GetTextLineHeightWithSpacing() - 1;
        //ImGui.BeginChildFrame(2, new(0, (skip) * ImGui.GetTextLineHeightWithSpacing()), ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        //ImGui.EndChildFrame();
        //skip = 0;

        foreach (var (mat, name) in cachedSearch) {
            // todo: calculate that 60!!!
            if (rendered < 60 && skip <= 0) {
                rendered++;
                RenderMaterialListElement(mat, name);
            } else {
                ImGui.NewLine(); // better performance than selectable
            }


            skip--;
        }

        //var left = cachedSearch.Count - rendered;
        //ImGui.BeginChildFrame(1, new(0, (cachedSearch.Count - 60) * ImGui.GetTextLineHeightWithSpacing()), ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        //ImGui.EndChildFrame();

        EndMaterialListGUI(searchBar: true);
    }
}
