namespace Rysy.History;

public static class TryHelper {

    /// <summary>
    /// Tries to call <paramref name="action"/>, retrying at most <paramref name="retries"/> times in case of exceptions.
    /// If an exception is thrown after <paramref name="retries"/> attempts, returns null.
    /// </summary>
    public static T? Try<T>(Func<T> action, int retries = 0) where T : class {
        start:
        try {
            return action();
        } catch (Exception) {
            retries--;
            if (retries > 0) {
                Thread.Sleep(100 * retries);
                goto start;
            }
        }

        return null;
    }
}
