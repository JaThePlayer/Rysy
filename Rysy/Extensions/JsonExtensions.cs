using Rysy.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Rysy.Extensions;

public static class JsonExtensions {
    /// <summary>
    /// Returns the JSON representation of this object.
    /// </summary>
    public static string ToJson<T>(this T? obj, bool minified = false) {
        return obj is { } ? JsonSerializer.Serialize(obj, Options(minified)) : "";
    }

    /// <summary>
    /// Returns the JSON representation of this object as UTF-8 bytes
    /// </summary>
    public static byte[] ToJsonUTF8<T>(this T? obj, bool minified = true) {
        return obj is { } ? JsonSerializer.SerializeToUtf8Bytes(obj, Options(minified)) : Array.Empty<byte>();
    }

    public static T? TryDeserialize<T>(string str) {
        try {
            return JsonSerializer.Deserialize<T>(str, Options(true));
        } catch {
            //Console.WriteLine(str);
            //Console.WriteLine(e);
            return default;
        }
    }

    public static async ValueTask<T?> TryDeserializeAsync<T>(Stream stream) {
        try {
            return await JsonSerializer.DeserializeAsync<T>(stream, Options(true));
        } catch (Exception e) {
            Console.WriteLine(e);
            return default;
        }
    }

    /// <summary>
    /// Fixes a object-valued dictionary after getting deserialised to not contain JsonElement values, instead replacing them with primitive types.
    /// </summary>
    [return: NotNullIfNotNull(nameof(dict))]
    public static Dictionary<TKey, object>? FixDict<TKey>(this Dictionary<TKey, object>? dict, IEqualityComparer<TKey> comparer)
        where TKey : notnull
        => dict?.ToDictionary(kv => kv.Key, kv => {
            if (kv.Value is JsonElement n) {
                if (n.ValueKind == JsonValueKind.String)
                    return n.GetString()!;
                if (n.ValueKind == JsonValueKind.False)
                    return false;
                if (n.ValueKind == JsonValueKind.True)
                    return true;
                if (n.ValueKind == JsonValueKind.Number) {
                    if (n.TryGetInt32(out int i))
                        return i;
                    return n.GetSingle();
                }
            }

            return kv.Value;
        }) ?? dict;

    private static JsonSerializerOptions Options(bool minified) => minified ? JsonSerializerHelper.DefaultOptionsMinified : JsonSerializerHelper.DefaultOptions;

}
