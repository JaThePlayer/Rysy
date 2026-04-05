namespace Rysy.Components;

/// <summary>
/// Allows invoking Everest's DebugRC, if the game is currently open.
/// </summary>
public interface IDebugRcClient {
    /// <summary>
    /// Calls the given DebugRC endpoint, and returns the result as a string.
    /// Throws an exception if Celeste is not open or any network error occured.
    /// </summary>
    public Task<string> GetStringAsync(string message);
}

public sealed class DebugRcClient(HttpClient httpClient, ushort port) : IDebugRcClient {
    private readonly string _baseUrl = $"http://localhost:{port}/";
    
    public async Task<string> GetStringAsync(string message) {
        return await httpClient.GetStringAsync($"{_baseUrl}{message.AsSpan().TrimStart('/')}");
    }
}
