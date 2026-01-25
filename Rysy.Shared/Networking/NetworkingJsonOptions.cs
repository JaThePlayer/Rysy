using System.Text.Json;

namespace Rysy.Shared.Networking;

public static class NetworkingJsonOptions {
    public static JsonSerializerOptions IncludeFields { get; } = new() {
        IncludeFields = true,
    };
}