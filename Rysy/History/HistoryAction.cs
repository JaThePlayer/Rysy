namespace Rysy.History;

public interface IHistoryAction
{
    /// <returns>Whether the action had any effect. If false is returned, the action will not be added to history</returns>
    public bool Apply();
    public void Undo();
}

