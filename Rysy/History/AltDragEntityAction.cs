namespace Rysy.History;

public record AltDragEntityAction(EntityRef Entity, Vector2 Offset) : IHistoryAction {
    private AddEntityAction? _addEntityAction;
    private Entity? _clonedEntity;
    
    public bool Apply(Map map) {
        var entity = Entity.Resolve(map);
        var clone = entity.CloneWith(pl => pl.ValueOverrides.Remove(Rysy.Entity.EditorGroupEntityDataKey));
        clone.Pos += Offset;

        _addEntityAction = new AddEntityAction(clone, entity.Room);
        if (!_addEntityAction.Apply(map)) {
            return false;
        }

        _clonedEntity = clone;
        entity.TransferHandlersTo(clone);
        
        return true;
    }

    public void Undo(Map map) {
        _addEntityAction?.Undo(map);
        _clonedEntity?.TransferHandlersTo(Entity.Resolve(map));
    }
}
