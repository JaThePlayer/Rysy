using System.Text.Encodings.Web;
using System.Text.Json;

namespace Rysy.Helpers;

public static class JsonSerializerHelper {
    /// <summary>
    /// The <see cref="JsonSerializerOptions"/> used by Rysy for all serialization
    /// </summary>
    public static JsonSerializerOptions DefaultOptionsMinified { get; set; } = new() {
        IncludeFields = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static JsonSerializerOptions DefaultOptions { get; private set; } = new(DefaultOptionsMinified) {
        WriteIndented = true,
    };

    /// <summary>
    /// Options used for setting serialization/deserialization
    /// </summary>
    public static JsonSerializerOptions SettingsOptions { get; set; } = new() {
        WriteIndented = true,
        IncludeFields = true,
        IgnoreReadOnlyFields = true,
        IgnoreReadOnlyProperties = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        // The '+' sign needs to be encoded without escaping for hotkeys.
        // This should be safe to use, because we're not using this in the web anyway.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}