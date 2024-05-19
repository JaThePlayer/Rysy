namespace Rysy;

/// <summary>
/// Contains XNA state such as the current Game instance or GraphicsDevice.
/// </summary>
public static class RysyState {
    public static GraphicsDeviceManager GraphicsDeviceManager { get; set; } = null!;

    public static GraphicsDevice GraphicsDevice => GraphicsDeviceManager.GraphicsDevice;

    public static GameWindow Window => Game.Window;
    
    public static Game Game { get; set; }
}