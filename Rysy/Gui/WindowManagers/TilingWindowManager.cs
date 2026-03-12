using Rysy.Gui.Windows;

namespace Rysy.Gui.WindowManagers;

public sealed class TilingWindowManager : INewWindowManager {
    public WindowStartConfig? Layout(Scene scene, Window window, Rectangle bounds) {
        var existingForms = scene.GetAll<Window>();
        if (existingForms is [])
            return null;

        if (window.Size is not { } newSize) {
            return null;
        }
        
        foreach (var otherWindow in existingForms) {
            if (otherWindow == window)
                continue;

            if (TryTile(newSize, otherWindow, existingForms, bounds, out var cfg))
                return cfg;
        }

        return null;
    }

    private bool TryTile(NumVector2 newSize, Window basedOn, IReadOnlyList<Window> allForms, Rectangle bounds, out WindowStartConfig cfg)
    {
        var newFormSize = new Point((int)newSize.X, (int)newSize.Y);
        var startPos = basedOn.LastPosition;
        
        var rightStart = startPos + new NumVector2(basedOn.LastSize.X, 0f);
        if (TryTileTo(rightStart)) {
            cfg = new WindowStartConfig(rightStart);
            return true;
        }
        
        var leftStart = startPos - new NumVector2(newFormSize.X, 0f);
        if (TryTileTo(leftStart)) {
            cfg = new WindowStartConfig(leftStart);
            return true;
        }

        var bottomStart = startPos + new NumVector2(0f, basedOn.LastSize.Y);
        if (TryTileTo(bottomStart)) {
            cfg = new WindowStartConfig(bottomStart);
            return true;
        }
        
        var topStart = startPos - new NumVector2(0f, newFormSize.Y);
        if (TryTileTo(topStart)) {
            cfg = new WindowStartConfig(topStart);
            return true;
        }

        cfg = default;
        return false;

        bool TryTileTo(NumVector2 pos)
        {
            return !WindowManagerUtils.WillOverlapAt(newFormSize, pos, bounds, allForms, basedOn);
        }
    }
}