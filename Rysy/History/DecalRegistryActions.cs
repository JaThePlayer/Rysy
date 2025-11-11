using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.History;

internal sealed record DecalRegistryRemoveEntryAction(DecalRegistryEntry Entry) : IHistoryAction {
    private int _index;
    
    public bool Apply(Map map) {
        _index = Gfx.DecalRegistry.GetEntriesForMod(map.Mod!).IndexOf(Entry);
        
        return Gfx.DecalRegistry.RemoveEntryFromMod(map.Mod!, Entry);
    }

    public void Undo(Map map) {
        Gfx.DecalRegistry.AddEntryToMod(map.Mod!, Entry, _index);
    }
}

internal sealed record DecalRegistryRemovePropAction(DecalRegistryEntry Entry, DecalRegistryProperty Prop) : IHistoryAction {
    private int _index;
    
    public bool Apply(Map map) {
        _index = Entry.Props.IndexOf(Prop);
        return Entry.Props.Remove(Prop);
    }

    public void Undo(Map map) {
        Entry.Props.Insert(_index, Prop);
    }
}

internal sealed record DecalRegistryAddEntryAction(DecalRegistryEntry Entry) : IHistoryAction {
    public bool Apply(Map map) {
        Gfx.DecalRegistry.AddEntryToMod(map.Mod!, Entry);

        return true;
    }

    public void Undo(Map map) {
        Gfx.DecalRegistry.RemoveEntryFromMod(map.Mod!, Entry);
    }
}

internal sealed record DecalRegistryAddPropAction(DecalRegistryEntry Entry, DecalRegistryProperty Prop) : IHistoryAction {
    public bool Apply(Map map) {
        Entry.Props.Add(Prop);
        
        return true;
    }

    public void Undo(Map map) {
        Entry.Props.Remove(Prop);
    }
}

internal sealed record DecalRegistryChangeEntryPathAction(DecalRegistryEntry Entry, string NewPath) : IHistoryAction {
    private string _oldPath;
    
    public bool Apply(Map map) {
        _oldPath = Entry.Path;
        Entry.Path = NewPath;

        return true;
    }

    public void Undo(Map map) {
        Entry.Path = _oldPath;
    }
}

internal sealed record DecalRegistryMoveEntryAction(DecalRegistryEntry Entry, int Offset) : IHistoryAction {
    private int _startIdx;
    
    public bool Apply(Map map) {
        var entries = Gfx.DecalRegistry.GetEntriesForMod(map.Mod!);
        
        var idx = entries.IndexOf(Entry);
        if (idx == -1)
            return false;
        
        _startIdx = idx;
        
        var i = idx + Offset;
        if (i < 0 || i >= entries.Count) {
            return false;
        }
        
        (entries[i], entries[idx]) = (entries[idx], entries[i]);

        return true;
    }

    public void Undo(Map map) {
        var entries = Gfx.DecalRegistry.GetEntriesForMod(map.Mod!);
        var i = _startIdx + Offset;

        (entries[i], entries[_startIdx]) = (entries[_startIdx], entries[i]);
    }
}

internal sealed record DecalRegistryMovePropAction(DecalRegistryEntry Entry, DecalRegistryProperty Prop, int Offset) : IHistoryAction {
    private int _startIdx;
    
    public bool Apply(Map map) {
        var entries = Entry.Props;
        
        var idx = entries.IndexOf(Prop);
        if (idx == -1)
            return false;
        
        _startIdx = idx;
        var i = idx + Offset;
        
        if (i < 0 || i >= entries.Count) {
            return false;
        }
        
        (entries[i], entries[idx]) = (entries[idx], entries[i]);

        return true;
    }

    public void Undo(Map map) {
        var entries = Entry.Props;
        var i = _startIdx + Offset;

        (entries[i], entries[_startIdx]) = (entries[_startIdx], entries[i]);
    }
}

internal sealed record DecalRegistryChangePropertyAction(DecalRegistryProperty Prop, Dictionary<string, object> Changed) : IHistoryAction {
    private Dictionary<string, object> _old;
    private Dictionary<string, object> _editedClone;
    
    public bool Apply(Map map) {
        _old ??= new(Prop.Data.Inner, Prop.Data.Inner.Comparer);
        _editedClone ??= new(Changed, Changed.Comparer);
        
        Prop.Data.BulkUpdate(_editedClone);

        return true;
    }

    public void Undo(Map map) {
        Prop.Data.BulkUpdate(_old);
    }
}