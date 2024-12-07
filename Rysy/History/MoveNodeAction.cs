namespace Rysy.History;

public record MoveNodeAction(Node Node, EntityRef Entity, Vector2 By) : IHistoryAction {
    public bool Apply(Map map) {
        var entity = Entity.Resolve(map);
        
        Node.Pos += By;

        entity.ClearRoomRenderCache();
        entity.OnChanged(new() {
            NodesChanged = true,
            NodeCountChanged = false,
        });

        return true;
    }

    public void Undo(Map map) {
        var entity = Entity.Resolve(map);
        Node.Pos -= By;

        entity.ClearRoomRenderCache();
        entity.OnChanged(new() {
            NodesChanged = true,
        });
    }
}
