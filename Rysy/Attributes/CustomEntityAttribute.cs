using JetBrains.Annotations;

namespace Rysy;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
[MeansImplicitUse]
public sealed class CustomEntityAttribute : Attribute {
    public string Name { get; }
    public string[] AssociatedMods { get; }

    public CustomEntityAttribute(string name, string[]? associatedMods = null) {
        Name = name;
        AssociatedMods = associatedMods ?? Array.Empty<string>();
    }
}