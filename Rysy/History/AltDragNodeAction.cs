namespace Rysy.History;

public record AltDragNodeAction(EntityRef Entity, List<int> NodeIndexes, Vector2 Offset) : IMergeableHistoryAction {
    private List<IHistoryAction> _addNodeActions = [];
    
    public bool Apply(Map map) {
        var anyAdded = false;
        var entity = Entity.Resolve(map);
        var nodes = entity.Nodes;

        foreach (var nodeIndex in NodeIndexes) {
            if (!entity.CanAddAnotherNode())
                break;
            
            var node = new Node(nodes[nodeIndex].Pos + Offset);
            NodeSelectionHandler? sourceNodeSelectionHandler = null;

            _addNodeActions.Add(new AddNodeAction(Entity, node, nodeIndex + 1).WithHook(
                onApply: () => {
                    sourceNodeSelectionHandler = entity.NodeSelectionHandlers?.ElementAtOrDefault(nodeIndex);
                    if (sourceNodeSelectionHandler is { }) {
                        entity.NodeSelectionHandlers![nodeIndex] = null;
                        entity.CreateNodeSelection(nodeIndex + 1, sourceNodeSelectionHandler);
                        sourceNodeSelectionHandler.Node = nodes[nodeIndex + 1];
                        sourceNodeSelectionHandler.RecalculateId();
                    }
                },
                onUndo: () => {
                    if (sourceNodeSelectionHandler is { }) {
                        entity.NodeSelectionHandlers![nodeIndex] = sourceNodeSelectionHandler;
                        entity.NodeSelectionHandlers![nodeIndex + 1] = null;
                        sourceNodeSelectionHandler.Node = nodes[nodeIndex];
                        sourceNodeSelectionHandler.RecalculateId();
                    }
                }
            ));
            _addNodeActions[^1].Apply(map);
            anyAdded = true;
        }

        return anyAdded;
    }

    public void Undo(Map map) {
        foreach (var action in _addNodeActions.AsEnumerable().Reverse()) {
            action.Undo(map);
        }
        _addNodeActions.Clear();
    }

    public IHistoryAction? TryMergeWith(IMergeableHistoryAction other) {
        if (other is not AltDragNodeAction otherAltDrag)
            return null;
        
        return this with {
            NodeIndexes = NodeIndexes.Concat(otherAltDrag.NodeIndexes).OrderDescending().ToList()
        };
    }
}
