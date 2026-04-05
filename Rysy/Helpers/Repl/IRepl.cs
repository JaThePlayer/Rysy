namespace Rysy.Helpers.Repl;

public interface IRepl {
    /// <summary>
    /// Continues the repl session with the new code from the user, and returns the result.
    /// </summary>
    public Task<object?> ContinueWith(string newCode);
}
