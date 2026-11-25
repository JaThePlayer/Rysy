namespace Rysy.LuaSupport;

/// <summary>
/// When returned as "options" for a lua field info entry, generates a custom C# field type instead of default behavior.
/// </summary>
public interface ILuaCustomFieldOptionsProvider : ILuaDictionaryWrapper {
    public Field CreateField(object def);
}
