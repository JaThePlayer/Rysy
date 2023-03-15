namespace Rysy.History;

internal class EntityEditAction : IHistoryAction {
    List<Entity> Entities;
    Dictionary<string, object> Changed;
    
    List<List<(string, object?)>> OldValues;

    public EntityEditAction(List<Entity> entities, Dictionary<string, object> changed) {
        Changed = new(changed);
        Entities = new(entities);
    }


    public bool Apply() {
        OldValues = new(Entities.Count);

        foreach (var entity in Entities) {
            var oldVals = new List<(string, object?)>(Changed.Count);
            foreach (var (key, val) in Changed) {
                entity.EntityData.TryGetValue(key, out var prevVal);
                oldVals.Add((key, prevVal));

                entity.EntityData[key] = val;
            }

            OldValues.Add(oldVals);
            entity.ClearRoomRenderCache();
        }

        return true;
    }

    public void Undo() {
        for (int i = 0; i < Entities.Count; i++) {
            var entity = Entities[i];
            var changed = OldValues[i];

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
