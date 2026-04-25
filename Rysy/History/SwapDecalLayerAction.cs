namespace Rysy.History;

public sealed record SwapDecalLayerAction(EntityRef ToSwap) : IHistoryAction {
    private RemoveEntityAction _removeEntityAction = null!;
    private AddEntityAction _addEntityAction = null!;
    
    public bool Apply(Map map) {
        if (ToSwap.TryResolve(map) is not Decal decal)
            return false;

        var swapped = decal.CloneWith(pl => pl.Sid = decal.Fg ? EntityRegistry.BgDecalSid : EntityRegistry.FgDecalSid);
        
        decal.TransferHandlersTo(swapped);

        _removeEntityAction = new RemoveEntityAction(decal);
        _addEntityAction = new AddEntityAction(swapped, new RoomRef(ToSwap.RoomName));

        _removeEntityAction.Apply(map);
        _addEntityAction.Apply(map);
        
        return true;
    }

    public void Undo(Map map) {
        _addEntityAction.Undo(map);
        _removeEntityAction.Undo(map);
    }
}