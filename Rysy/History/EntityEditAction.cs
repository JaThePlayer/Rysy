namespace Rysy.History;

internal sealed class EntityEditAction : IHistoryAction {
    List<Entity> _entities;
    Dictionary<string, object> _changed;
    
    List<List<(string, object?)>> _oldValues;

    public EntityEditAction(List<Entity> entities, Dictionary<string, object> changed) {
        _changed = new(changed);
        _entities = new(entities);
    }


    public bool Apply(Map map) {
        _oldValues = new(_entities.Count);

        foreach (var entity in _entities) {
            entity.EntityData.SetOverlay(null);
            var oldVals = new List<(string, object?)>(_changed.Count);
            foreach (var (key, val) in _changed) {
                entity.EntityData.TryGetValue(key, out var prevVal);
                oldVals.Add((key, prevVal));

                entity.EntityData[key] = val;
            }

            _oldValues.Add(oldVals);
            entity.ClearRoomRenderCache();
        }

        return true;
    }

    public void Undo(Map map) {
        for (int i = 0; i < _entities.Count; i++) {
            var entity = _entities[i];
            var changed = _oldValues[i];

            foreach (var (key, val) in changed) {
                if (val is null) {
                    entity.EntityData.Remove(key);
                } else {
                    entity.EntityData[key] = val;
                }
            }

            entity.ClearRoomRenderCache();
        }
    }
}
