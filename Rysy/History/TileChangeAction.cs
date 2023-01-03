namespace Rysy.History;

/// <summary>
/// An action where one tile gets changed in a tile grid. Calls SafeSetTile.
/// </summary>
public record class TileChangeAction(char ID, int X, int Y, Tilegrid Grid) : IHistoryAction
{
    private char lastID;

    public bool Apply() {
        lastID = Grid.SafeTileAt(X, Y);
        return Grid.SafeSetTile(ID, X, Y);
    }

    public void Undo()
    {
        Grid.SafeSetTile(lastID, X, Y);
    }
}
