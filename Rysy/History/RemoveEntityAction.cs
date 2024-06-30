namespace Rysy.History;

public sealed record RemoveEntityAction(EntityRef Entity) : IHistoryAction {
    private Entity? _removedEntity;
    private int _originalIndex;
    
    public bool Apply(Map map) {
        if (Entity.TryResolve(map) is not { } entity || entity.Room is null) {
            return false;
        }

        _removedEntity = entity;
        
        entity.ClearInnerCaches();
        var list = entity.GetRoomList();
        
        _originalIndex = list.IndexOf(entity);
        if (_originalIndex == -1)
            return false;
        list.RemoveAt(_originalIndex);
        
        return true;
    }

    public void Undo(Map map) {
        var entity = _removedEntity!;
        
        entity.GetRoomList().Insert(_originalIndex, entity);
    }
}
