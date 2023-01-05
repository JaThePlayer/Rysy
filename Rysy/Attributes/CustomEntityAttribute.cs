namespace Rysy;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class CustomEntityAttribute : Attribute {
    public readonly string Name;

    public CustomEntityAttribute(string name) {
        Name = name;
    }
}