using Rysy.Layers;

namespace Rysy.History; 

public sealed record ChangeEditorGroupAction(Map Map, string GroupName, string? AutoAssignString) : IHistoryAction {
    private bool _groupCreated;
    private HashSet<string> _lastAutoAssign;
    private HashSet<string> _newAutoAssign;
    private bool _autoAssignChanged;
    private List<Entity>? _preAutoAssignEntities;

    private void UpdateAutoAssigns(EditorGroup group, HashSet<string> newAutoAssign) {

    }
    
    public bool Apply() {
        var group = Map.EditorGroups.GetOrCreate(GroupName, out _groupCreated);

        if (AutoAssignString is {} autoAssignString) {
            _lastAutoAssign = group.AutoAssignTo;
            _newAutoAssign = EditorGroup.CreateAutoAssignFromString(autoAssignString);

            if (!_lastAutoAssign.SetEquals(_newAutoAssign)) {
                // update auto assigns on all entities
                group.AutoAssignTo = _newAutoAssign;
                
                _autoAssignChanged = true;

                if (_lastAutoAssign.Count == 0) {
                    // if the group previously wasn't auto-assigned, it might have some manual entities that will get lost.
                    // keep track of them, for CTRL+Z
                    _preAutoAssignEntities = Map.GetEntitiesInGroup(group).ToList();
                }
                
                UpdateAutoAssigns(group, _newAutoAssign);
            }
        }
        
        Map.GroupsChanged();
        
        return true;
    }

    public void Undo() {
        var group = Map.EditorGroups.GetOrCreate(GroupName);
        
        if (_groupCreated) {
            Map.EditorGroups.Remove(group);
        }

        if (_autoAssignChanged) {
            group.AutoAssignTo = _lastAutoAssign;
            UpdateAutoAssigns(group, _lastAutoAssign);
        }

        if (_preAutoAssignEntities is { } prev) {
            foreach (var entityIncorrect in Map.GetEntitiesInGroup(group)) {
                // remove all entities that were auto-assigned to this group, which should no longer be assigned.
                entityIncorrect.EditorGroups.Remove(group);
            }
            
            foreach (var e in prev) {
                e.EditorGroups.Add(group);
            }
        }
        
        Map.GroupsChanged();
    }
}