namespace Rysy.History;

public record class AddNodeAction(Entity Entity, Node Node, int NodeIdx) : IHistoryAction {
    public bool Apply() {
        var node = Node;
        var nodes = Entity.Nodes;

        nodes.Insert(NodeIdx, node);

        Entity.ClearRoomRenderCache();
        Entity.OnChanged(new()
        {
            NodesChanged = true
        });
        RecalculateIds();
        
        return true;
    }

    private void RecalculateIds() {
        if (Entity._NodeSelectionHandlers is { } handlers) {
            foreach (var h in handlers) {
                h?.RecalculateId();
            }

            Entity._NodeSelectionHandlers = new NodeSelectionHandler?[handlers.Length];

            for (int i = 0; i < Entity._NodeSelectionHandlers.Length; i++) {
                Entity._NodeSelectionHandlers[i] = handlers.FirstOrDefault(h => h is {} && h.NodeIdx == i);
            }
        }
    }

    public void Undo() {
        var nodes = Entity.Nodes;

        nodes.Remove(Node);
        
        RecalculateIds();
        Entity.ClearRoomRenderCache();
        Entity.OnChanged(new()
        {
            NodesChanged = true
        });
    }
}
