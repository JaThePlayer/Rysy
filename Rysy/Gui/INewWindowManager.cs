using Rysy.Gui.Windows;

namespace Rysy.Gui;

/// <summary>
/// Allows configuring the position of newly created windows
/// </summary>
public interface INewWindowManager {
    public WindowStartConfig? Layout(Scene scene, Window window, Rectangle bounds);
}

public record struct WindowStartConfig(NumVector2 Position);