namespace Rysy.Tools; 

public abstract class ToolMode {
    public abstract string Name { get; }

    public string LocalizedName => Name.TranslateOrHumanize("rysy.tools.modes");

    public static readonly ToolMode Default = new NamedMode("default");
    public static readonly List<ToolMode> DefaultList = new(1) { Default };
}

public sealed class NamedMode : ToolMode {
    public override string Name { get; }

    public NamedMode(string name) {
        Name = name;
    }
}