namespace Rysy.History;

public sealed record RemoveEntityAction(EntityRef Entity) : IHistoryAction {
    private Entity? removedEntity;
    
    public bool Apply(Map map) {
        if (Entity.TryResolve(map) is not { } entity || entity.Room is null) {
            return false;
        }

        removedEntity = entity;
        
        entity.ClearInnerCaches();
        var removed = entity.GetRoomList().Remove(entity);
        
        return removed;
    }

    public void Undo(Map map) {
        var entity = removedEntity!;
        
        entity.GetRoomList().Add(entity);
    }
}