namespace Rysy;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class CustomEntityAttribute : Attribute {
    public readonly string Name;
    public readonly string[] AssociatedMods;

    public CustomEntityAttribute(string name, string[]? associatedMods = null) {
        Name = name;
        AssociatedMods = associatedMods ?? Array.Empty<string>();
    }
}