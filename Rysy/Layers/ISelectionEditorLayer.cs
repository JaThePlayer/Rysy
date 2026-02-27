using Rysy.Selections;

namespace Rysy.Layers;

/// <summary>
/// Allows the layer to be used by the Selection Tool.
/// </summary>
public interface ISelectionEditorLayer : IEditorLayer {
    /// <summary>
    /// Returns a list of all selections within the provided rectangle.
    /// Respects editor layers.
    /// </summary>
    public List<Selection> GetSelectionsInRect(Map map, Room? room, Rectangle? rect);

    /// <summary>
    /// Renders the beginning of the material list on top of default selection tool rendering.
    /// </summary>
    public void RenderCustomMaterialListStart() {}
}