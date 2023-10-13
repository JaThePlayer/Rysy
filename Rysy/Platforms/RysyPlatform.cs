using System.Runtime.InteropServices;

namespace Rysy.Platforms;

public abstract class RysyPlatform {
    public static RysyPlatform Current { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? new Windows() :
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? new Linux() :
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? new MacOS() :
        throw new NotImplementedException($"Unsupported platform: {RuntimeInformation.RuntimeIdentifier}");

    /// <summary>
    /// Gets the location in which Rysy should save its settings
    /// </summary>
    public abstract string GetSaveLocation();

    /// <summary>
    /// Initializes some things needed to properly run Rysy on this platform.
    /// </summary>
    public virtual void Init() {
        
    }
}
