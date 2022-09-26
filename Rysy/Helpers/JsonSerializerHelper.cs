using System.Text.Json;

namespace Rysy.Helpers;

public static class JsonSerializerHelper
{
    /// <summary>
    /// The <see cref="JsonSerializerOptions"/> used by Rysy for all serialization
    /// </summary>
    public static JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = true,
        IgnoreReadOnlyFields = true,
        IgnoreReadOnlyProperties = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };
}