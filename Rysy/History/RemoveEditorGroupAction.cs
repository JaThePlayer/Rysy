using Rysy.Layers;

namespace Rysy.History; 

public record RemoveEditorGroupAction(Map Map, EditorGroup Group) : IHistoryAction {
    private List<Entity> _affectedEntities;
    private int _groupIndex;
    
    public bool Apply(Map map) {
        _groupIndex = Map.EditorGroups.Remove(Group);
        _affectedEntities = Map.GetEntitiesInGroup(Group).ToList();
        foreach (var e in _affectedEntities) {
            e.EditorGroups.Remove(Group);
        }
        
        Map.GroupsChanged();

        return true;
    }

    public void Undo(Map map) {
        Map.EditorGroups.Insert(_groupIndex, Group);

        foreach (var e in _affectedEntities) {
            e.EditorGroups.Add(Group);
        }
        
        Map.GroupsChanged();
    }
}