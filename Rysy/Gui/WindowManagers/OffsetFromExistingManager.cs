using Hexa.NET.ImGui;
using Rysy.Gui.Windows;

namespace Rysy.Gui.WindowManagers;

/// <summary>
/// Places new windows at a slight offset from an existing window.
/// </summary>
public sealed class OffsetFromExistingManager : INewWindowManager
{
    public WindowStartConfig? Layout(Scene scene, Window window, Rectangle bounds)
    {
        if (window.Size is not { } newSize) {
            return null;
        }
        var newFormSize = new Point((int)newSize.X, (int)newSize.Y);
        
        var offset = new NumVector2(ImGui.GetFrameHeightWithSpacing() + ImGui.GetStyle().WindowPadding.Y);
        var similarWindows = scene.GetAll<Window>(window.GetType());
        if (similarWindows is [])
            return null;

        // This logic is not quite correct once you start docking windows and such, but it's good enough for now.
        foreach (var (i, w) in similarWindows.Index().Reverse())
        {
            var pos = w.LastPosition + offset;

            // Skip over i + 1 windows, so windows behind this one don't block window creation.
            // Again, not fully correct in all situations.
            if (!WindowManagerUtils.WillOverlapAt(newFormSize, pos, bounds, similarWindows.Skip(i + 1), w))
            {
                return new WindowStartConfig(pos);
            }
        }

        return null;
    }
}