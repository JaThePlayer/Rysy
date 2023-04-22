using Rysy.History;

namespace Rysy.Scripting;

public abstract class Script {
    public abstract string Name { get; }

    public virtual string? Tooltip => null;

    public virtual FieldList? Parameters => null;

    public virtual IHistoryAction? Prerun(ScriptArgs args) => null;

    public virtual bool CallRun => true;

    public virtual void Run(Room room, ScriptArgs args) {
    }
}

public record class ScriptArgs {
    public IReadOnlyList<Room> Rooms { get; internal set; }

    public IReadOnlyDictionary<string, object> Args { get; internal set; }

    public T Get<T>(string name) => (T)Args[name];
}
