using Rysy.Gui.Windows;

namespace Rysy.Gui.WindowManagers;

/// <summary>
/// Contains utility methods for implementing <see cref="INewWindowManager"/>.
/// </summary>
public static class WindowManagerUtils
{
    /// <summary>
    /// Checks whether a window placed at <paramref name="pos"/> with the <paramref name="targetSize"/> would overlap any of the provided windows.
    /// </summary>
    /// <param name="targetSize">The target window size.</param>
    /// <param name="pos">The target window position.</param>
    /// <param name="screenBounds">Screen bounds, method will return true if the window would leave the screen bounds.</param>
    /// <param name="windowsToAvoid">Windows that will be checked for overlap.</param>
    /// <param name="ignoredWindow">A window which will be ignored, even if present in <paramref name="windowsToAvoid"/>.</param>
    /// <returns></returns>
    public static bool WillOverlapAt(Point targetSize, NumVector2 pos, Rectangle screenBounds, IEnumerable<Window> windowsToAvoid, Window? ignoredWindow = null)
    {
        var newBounds = new Rectangle((int)pos.X, (int)pos.Y, targetSize.X, targetSize.Y);

        if (!screenBounds.Contains(newBounds))
            return true;
        
        if (!windowsToAvoid.All(f => f == ignoredWindow || !f.LastBounds.Intersects(newBounds)))
            return true;

        return false;
    }
}