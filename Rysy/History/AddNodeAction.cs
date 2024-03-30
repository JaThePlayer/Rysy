namespace Rysy.History;

public record class AddNodeAction(EntityRef Entity, Node Node, int NodeIdx) : IHistoryAction {
    public bool Apply(Map map) {
        var entity = Entity.Resolve(map);
        var node = Node;
        var nodes = entity.Nodes;

        nodes.Insert(NodeIdx, node);

        entity.ClearRoomRenderCache();
        entity.OnChanged(new()
        {
            NodesChanged = true
        });
        RecalculateIds(entity);
        
        return true;
    }

    private void RecalculateIds(Entity entity) {
        if (entity._NodeSelectionHandlers is { } handlers) {
            foreach (var h in handlers) {
                h?.RecalculateId();
            }

            entity._NodeSelectionHandlers = new NodeSelectionHandler?[handlers.Length];

            for (int i = 0; i < entity._NodeSelectionHandlers.Length; i++) {
                entity._NodeSelectionHandlers[i] = handlers.FirstOrDefault(h => h is {} && h.NodeIdx == i);
            }
        }
    }

    public void Undo(Map map) {
        var entity = Entity.Resolve(map);
        var nodes = entity.Nodes;

        nodes.Remove(Node);
        
        RecalculateIds(entity);
        entity.ClearRoomRenderCache();
        entity.OnChanged(new()
        {
            NodesChanged = true
        });
    }
}
