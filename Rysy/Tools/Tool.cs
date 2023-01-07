using ImGuiNET;
using Rysy.Graphics;
using Rysy.Gui;
using Rysy.History;
using Rysy.Scenes;
using System.Collections.ObjectModel;

namespace Rysy.Tools;

public abstract class Tool {
    public HistoryHandler History;

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
        set => Persistence.Instance.Set($"{PersistenceGroup}.Layer", value);
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
        get => Persistence.Instance.Get($"{PersistenceGroup}.{Layer}.Material", (object) null!);
        set => Persistence.Instance.Set($"{PersistenceGroup}.{Layer}.Material", value);
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

        favorites.LogAsJson();
    }

    public abstract void Update(Camera camera, Room currentRoom);

    /// <summary>
    /// Renders this tool. Before calling, the sprite batch should be set using currentRoom.StartBatch(camera)
    /// </summary>
    /// <param name="camera"></param>
    /// <param name="currentRoom"></param>
    public abstract void Render(Camera camera, Room currentRoom);

    /// <summary>
    /// Renders the overlay for this tool. The spritebatch has no transformation matrix, used to draw directly to the screen.
    /// </summary>
    public abstract void RenderOverlay();

    /// <summary>
    /// Called once, when the tool is first created.
    /// </summary>
    public virtual void Init() {

    }

    public abstract IEnumerable<object>? GetMaterials(string layer);

    public abstract string MaterialToDisplayName(string layer, object material);

    private bool IsEqual(string layer, object? currentMaterial, string name) {
        return currentMaterial is { } && MaterialToDisplayName(layer, currentMaterial) == name;
    }

    public virtual void RenderGui(EditorScene editor, bool firstGui) {
        RenderMaterialList(editor, firstGui);
    }

    private void RenderMaterialList(EditorScene editor, bool firstGui) {
        var currentLayer = Layer;
        var materials = GetMaterials(currentLayer);
        if (materials is null)
            materials = new List<object>();
        var currentMaterial = Material;

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
        var favorites = Favorites;

        ImGui.BeginListBox("##ToolMaterialBox", new(windowSize.X - 10, windowSize.Y - ImGui.GetTextLineHeightWithSpacing() * 3));

        foreach (var (mat, name) in materials.Select(mat => (mat, MaterialToDisplayName(currentLayer, mat))).SearchFilter(kv => kv.Item2, Search, Favorites)) {
            var displayName = favorites is { } && favorites.Contains(name) ? $"* {name}" : name;

            if (ImGui.Selectable(displayName, IsEqual(currentLayer, currentMaterial, name), ImGuiSelectableFlags.AllowDoubleClick).WithTooltip(mat?.ToString() ?? "")) {
                Material = mat;

                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) {
                    ToggleFavorite(name);
                }
            }
        }

        ImGui.EndListBox();


        var search = Search;
        // pass the persistence key as the ID to imgui, because otherwise if you had the search bar selected while switching layers/tools,
        // your search would persist to the different layer/tool
        if (ImGui.InputText($"##{SearchPersistenceKey}", ref search, 512, ImGuiInputTextFlags.AlwaysOverwrite)) {
            Search = search;
        }

        ImGui.End();
    }
}
