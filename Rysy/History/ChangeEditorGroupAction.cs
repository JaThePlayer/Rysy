using Rysy.Helpers;
using Rysy.Layers;

namespace Rysy.History; 

public sealed record ChangeEditorGroupAction(Map Map, string GroupName, string? AutoAssignString, string? AutoAssignDecalsString) : IHistoryAction {
    private bool _groupCreated;
    private HashSet<string>? _lastAutoAssign;
    private List<DecalRegistryPath>? _lastAutoAssignDecals;
    
    private HashSet<string> _newAutoAssign;
    private List<Entity>? _preAutoAssignEntities;
    
    public bool Apply(Map map) {
        var group = Map.EditorGroups.GetOrCreate(GroupName, out _groupCreated);

        bool shouldSetPreAutoAssignEntities = false;
        
        if (AutoAssignString is {} autoAssignString) {
            _lastAutoAssign = group.AutoAssignTo;
            _newAutoAssign = EditorGroup.CreateAutoAssignFromString(autoAssignString);

            if (!_lastAutoAssign.SetEquals(_newAutoAssign)) {
                // update auto assigns on all entities
                group.AutoAssignTo = _newAutoAssign;
                
                if (_lastAutoAssign.Count == 0)
                    shouldSetPreAutoAssignEntities = true;
            }
        }

        if (AutoAssignDecalsString is { }) {
            _lastAutoAssignDecals = group.AutoAssignToDecals;
            group.AutoAssignToDecals = EditorGroup.CreateAutoAssignDecalsFromString(AutoAssignDecalsString);
            if (_lastAutoAssignDecals.Count == 0)
                shouldSetPreAutoAssignEntities = true;
        }
        
        if (shouldSetPreAutoAssignEntities) {
            // if the group previously wasn't auto-assigned, it might have some manual entities that will get lost.
            // keep track of them, for CTRL+Z
            _preAutoAssignEntities = Map.GetEntitiesInGroup(group).ToList();
        }
        
        Map.GroupsChanged();
        
        return true;
    }

    public void Undo(Map map) {
        var group = Map.EditorGroups.GetOrCreate(GroupName);
        
        if (_groupCreated) {
            Map.EditorGroups.Remove(group);
        }

        if (_lastAutoAssign is {}) {
            group.AutoAssignTo = _lastAutoAssign;
        }
        
        if (_lastAutoAssignDecals is { }) {
            group.AutoAssignToDecals = _lastAutoAssignDecals;
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