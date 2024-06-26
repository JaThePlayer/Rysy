﻿using Rysy.Stylegrounds;

namespace Rysy.History;
public record class RemoveStyleAction(IList<Style> Styles, Style ToRemove, StyleFolder? Folder) : IHistoryAction {
    private int Index;
    private bool RemovedFromParent;

    public bool Apply(Map map) {
        Index = Styles.IndexOf(ToRemove);
        if (ToRemove.Parent == Folder) {
            ToRemove.Parent = null;
            RemovedFromParent = true;
        }

        return Styles.Remove(ToRemove);
    }

    public void Undo(Map map) {
        Styles.Insert(Index, ToRemove);
        if (RemovedFromParent) {
            ToRemove.Parent = Folder;
        }
    }
}
