using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Rysy.Helpers;

public static class JsonSerializerHelper {
    /// <summary>
    /// The <see cref="JsonSerializerOptions"/> used by Rysy for all serialization
    /// </summary>
    public static JsonSerializerOptions DefaultOptionsMinified { get; set; } = new() {
        IncludeFields = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Converters = {
            new ObjectToInferredTypesConverter(),
        }
    };

    public static JsonSerializerOptions DefaultOptions { get; private set; } = new(DefaultOptionsMinified) {
        WriteIndented = true,
    };

    /// <summary>
    /// Options used for setting serialization/deserialization
    /// </summary>
    public static JsonSerializerOptions SettingsOptions { get; set; } = new JsonSerializerOptions() {
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

    public class ObjectToInferredTypesConverter : JsonConverter<object> {
        public override object Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) => reader.TokenType switch {
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.Number when reader.TryGetInt32(out int l) => l,
                JsonTokenType.Number => reader.GetSingle(),
                JsonTokenType.String => reader.GetString()!,
                _ => JsonDocument.ParseValue(ref reader).RootElement.Clone()
            };

        public override void Write(
            Utf8JsonWriter writer,
            object value,
            JsonSerializerOptions options) => 
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}

[JsonSourceGenerationOptions(WriteIndented = true,  
    IncludeFields = true,
    IgnoreReadOnlyFields = true,
    IgnoreReadOnlyProperties = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true)]
[JsonSerializable(typeof(BackupIndex))]
[JsonSerializable(typeof(Settings))]
[JsonSerializable(typeof(Profile))]
[JsonSerializable(typeof(Persistence))]
[JsonSerializable(typeof(Placement))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(float))]
public partial class DefaultJsonContext : JsonSerializerContext
{
}


public interface IHasJsonCtx<T> {
    public static abstract JsonTypeInfo<T> JsonCtx { get; }
}