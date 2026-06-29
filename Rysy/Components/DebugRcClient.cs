namespace Rysy.Components;

/// <summary>
/// Allows invoking Everest's DebugRC, if the game is currently open.
/// </summary>
public interface IDebugRcClient {
    /// <summary>
    /// Calls the given DebugRC endpoint, and returns the result as a string.
    /// Throws an exception if Celeste is not open or any network error occured.
    /// </summary>
    public Task<HttpResponseMessage> CallAsync(string endpoint);
}

public static class DebugRcClientExt {
    extension(IDebugRcClient client) {
        /// <summary>
        /// Teleports the player to the given room in the map.
        /// </summary>
        /// <param name="sid">The SID of the map.</param>
        /// <param name="side">The side to load.</param>
        /// <param name="roomName">The name of the room.</param>
        /// <param name="forceNewSession">Whether a new session should be started even if the player is already in this map.</param>
        /// <param name="x">X position to load into.</param>
        /// <param name="y">Y position to load into.</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> Tp(string sid, CelesteLevelSide side, string roomName, int? x, int? y, bool forceNewSession) {
            var url = $"tp?area={sid}&side={side}&level={roomName}";
            if (forceNewSession)
                url += "&forcenew=true";
            if (x is not null)
                url += $"&x={x}";
            if (y is not null)
                url += $"&x={y}";
            
            var response = await client.CallAsync(url);
            
            return response;
        }
    }
}

public enum CelesteLevelSide {
    A, B, C
}

public sealed class DebugRcClient(IRysyLogger<DebugRcClient> logger, HttpClient httpClient, ushort port) : IDebugRcClient {
    private readonly string _baseUrl = $"http://localhost:{port}/";
    
    public async Task<HttpResponseMessage> CallAsync(string endpoint) {
        logger.Info($"Requesting {endpoint}");
        try {
            var response = await httpClient.GetAsync($"{_baseUrl}{endpoint.AsSpan().TrimStart('/')}");
            logger.Info($"Response: {response.ReasonPhrase}");
            return response;
        } catch (Exception ex) {
            logger.Info($"Failed to get response: {ex}");
            throw;
        }
    }
}
