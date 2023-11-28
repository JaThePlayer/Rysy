using Rysy.Layers;

namespace Rysy.History; 

public sealed record ChangeEditorGroupAction(Map Map, string GroupName, string? AutoAssignString) : IHistoryAction {
    private bool _groupCreated;
    private HashSet<string> _lastAutoAssign;
    private HashSet<string> _newAutoAssign;
    private bool _autoAssignChanged;

    private void UpdateAutoAssigns(EditorGroup group, HashSet<string> newAutoAssign) {

    }
    
    public bool Apply() {
        var group = Map.EditorGroups.GetOrCreate(GroupName, out _groupCreated);

        if (AutoAssignString is {} autoAssignString) {
            _lastAutoAssign = group.AutoAssignTo;
            _newAutoAssign = EditorGroup.CreateAutoAssignFromString(autoAssignString);

            if (!_lastAutoAssign.SequenceEqual(_newAutoAssign)) {
                // update auto assigns on all entities
                group.AutoAssignTo = _newAutoAssign;
                UpdateAutoAssigns(group, _newAutoAssign);
                
                _autoAssignChanged = true;
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
        
        Map.GroupsChanged();
    }
}