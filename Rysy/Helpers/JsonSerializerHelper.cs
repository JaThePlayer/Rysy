using System.Text.Encodings.Web;
using System.Text.Json;

namespace Rysy.Helpers;

public static class JsonSerializerHelper {
    /// <summary>
    /// The <see cref="JsonSerializerOptions"/> used by Rysy for all serialization
    /// </summary>
    public static JsonSerializerOptions DefaultOptions = new() {
        IncludeFields = true,
        WriteIndented = true,
        IgnoreReadOnlyFields = true,
        IgnoreReadOnlyProperties = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        // The '+' sign needs to be encoded without escaping for hotkeys.
        // This should be safe to use, because we're not using this in the web anyway.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}