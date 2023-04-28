using Rysy.History;

namespace Rysy.Scripting;

public abstract class Script {
    public abstract string Name { get; }

    public virtual string? Tooltip => null;

    public virtual FieldList? Parameters => null;

    /// <summary>
    /// A function that gets called once per script execution, regardless of how many rooms it should target.
    /// </summary>
    /// <param name="args">The arguments for this script, passed from the GUI.</param>
    /// <returns>A history action to add to the history handler to apply/undo this script</returns>
    public virtual IHistoryAction? Prerun(ScriptArgs args) => null;

    /// <summary>
    /// Whether the <see cref="Run(Room, ScriptArgs)"/> function should be called for this script.
    /// Defaults to true.
    /// </summary>
    public virtual bool CallRun => true;

    /// <summary>
    /// Runs the script in the given room. Any changes to <paramref name="room"/> will be automatically added to history, if this function returns true.
    /// </summary>
    /// <param name="room">The room to change in this call, which is a *clone* of a room in the map</param>
    /// <param name="args">The arguments for this script, passed from the GUI</param>
    /// <returns>Whether the room got changed at all</returns>
    public virtual bool Run(Room room, ScriptArgs args) {
        return false;
    }
}

public record class ScriptArgs {
    public IReadOnlyList<Room> Rooms { get; internal set; }

    public IReadOnlyDictionary<string, object> Args { get; internal set; }

    public T Get<T>(string name) => (T)Args[name];
}
