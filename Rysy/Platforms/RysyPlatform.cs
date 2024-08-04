using Rysy.Mods;
using System.Runtime.InteropServices;

namespace Rysy.Platforms;

public abstract class RysyPlatform {
    private static RysyPlatform? _current;
    
    public static RysyPlatform Current => _current ??=
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? new Windows() :
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? new Linux() :
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? new MacOS() :
        throw new NotImplementedException($"Unsupported platform: {RuntimeInformation.RuntimeIdentifier}");

    public virtual bool SupportFileWatchers => true;
    
    public virtual bool SupportImGui => true;
    
    /// <summary>
    /// Forcibly overrides the current platform. Should be called before Rysy initializes!
    /// </summary>
    /// <param name="newPlatform"></param>
    public static void OverridePlatform(RysyPlatform newPlatform) {
        _current = newPlatform;
    }

    protected IModFilesystem? CachedRysyFilesystem;
    
    public virtual IModFilesystem GetRysyFilesystem() {
#if DEBUG
        return CachedRysyFilesystem ??= Directory.Exists("../../../Assets")
        ? new FolderModFilesystem(Path.GetFullPath("../../../Assets"))
        : new FolderModFilesystem("Assets");

#else
        return CachedRysyFilesystem ??= new FolderModFilesystem("Assets");
#endif
    }

    public virtual (string Name, Profile Profile)? ForcedProfile() => null;

    public bool HasForcedProfile => ForcedProfile() is { };
    
    /// <summary>
    /// Gets the location in which Rysy should save its settings
    /// </summary>
    public abstract string GetSaveLocation();

    /// <summary>
    /// Initializes some things needed to properly run Rysy on this platform.
    /// </summary>
    public virtual void Init() {
        
    }

    public virtual void ResizeWindow(int x, int y, int w, int h) {
        var gdm = RysyState.GraphicsDeviceManager;

        gdm.PreferredBackBufferWidth = w;
        gdm.PreferredBackBufferHeight = h;
        gdm.IsFullScreen = false;
        gdm.ApplyChanges();


        var monitorSize = gdm.GraphicsDevice.DisplayMode;
        // just in case persistence got a messed up value, snap these back in range
        if (!x.IsInRange(0, monitorSize.Width - w - 32))
            x = 0;

        // todo: get rid of that hardcoded 32, though that's not easy cross-platform...
        //int taskbarHeight = 30 + 1;
        //var minY = RysyEngine.Instance.Window.IsBorderlessShared() ? 0 : taskbarHeight;
        //if (!y.IsInRange(minY, monitorSize.Height - h - taskbarHeight))
        //    y = taskbarHeight;
        RysyState.Window.SetPosition(new(x, y));
        gdm.ApplyChanges();
    }

    public virtual void ExitProcess() {
        if (RysyEngine.Instance is {} inst)
            inst.Exit();
        else
            Environment.Exit(-1);
    }
}
