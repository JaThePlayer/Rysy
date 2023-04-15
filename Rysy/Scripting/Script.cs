using Rysy.History;

namespace Rysy.Scripting;

public abstract class Script {
    public abstract string Name { get; }

    public virtual string? Tooltip => null;

    public virtual FieldList? Fields => null;

    public virtual void Prerun() { }

    public virtual void Run(Room room) {
    }
}
