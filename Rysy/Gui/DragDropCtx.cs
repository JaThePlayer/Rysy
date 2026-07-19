using Hexa.NET.ImGui;

namespace Rysy.Gui;

/// <summary>
/// Allows for setting up an imgui drag and drop source that accepts C# objects.
/// </summary>
/// <param name="payloadName">The name of the payload of this context.</param>
/// <typeparam name="T">The class type that will be drag-droppable.</typeparam>
public sealed class DragDropCtx<T>(string payloadName) where T : class {
    private static int NextPayloadId;

    private readonly string _payloadName = $"{payloadName}_{Interlocked.Increment(ref NextPayloadId)}";
    
    private readonly Dictionary<T, nint> _objectToIds = [];
    private readonly Dictionary<nint, T> _idsToObjects = [];
    private nint _nextId;
    
    /// <summary>
    /// Creates a drag-drop source for the given <paramref name="obj"/>.
    /// Holds a reference to the given object as long as this context is alive!
    /// </summary>
    /// <param name="obj">The object that can be drag-dropped.</param>
    /// <returns>If the user is currently dropping an object, returns that object. Otherwise, returns null.</returns>
    public unsafe T? DragDrop(T obj) {
        if (!_objectToIds.TryGetValue(obj, out nint id)) {
            id = _nextId++;
            _objectToIds[obj] = id;
            _idsToObjects[id] = obj;
        }
        
        if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.SourceNoDisableHover | ImGuiDragDropFlags.SourceNoPreviewTooltip)) {
            ImGui.SetDragDropPayload(_payloadName, &id, (nuint)sizeof(nint));
            ImGui.EndDragDropSource();
        }

        T? ret = null;
        if (ImGui.BeginDragDropTarget())
        {
            var payload = ImGui.AcceptDragDropPayload(_payloadName, ImGuiDragDropFlags.AcceptBeforeDelivery);
            if (payload.Handle != null) {
                var droppedId = *(nint*) payload.Data;
                ret = _idsToObjects[droppedId];
            }
            ImGui.EndDragDropTarget();
        }

        return ret;
    }
    
    /// <summary>
    /// Receives dropped objects without creating a drag source.
    /// </summary>
    /// <returns>If the user is currently dropping an object, returns that object. Otherwise, returns null.</returns>
    public unsafe T? DragDrop() {
        T? ret = null;
        if (ImGui.BeginDragDropTarget())
        {
            var payload = ImGui.AcceptDragDropPayload(_payloadName, ImGuiDragDropFlags.AcceptBeforeDelivery);
            if (payload.Handle != null) {
                var droppedId = *(nint*) payload.Data;
                ret = _idsToObjects[droppedId];
            }
            ImGui.EndDragDropTarget();
        }

        return ret;
    }
}