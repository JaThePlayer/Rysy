using Rysy.Mods;

namespace Rysy.Platforms;

public class Linux : RysyPlatform {
    private static string SaveLocation = UncachedGetSaveLocation();

    public override string GetSaveLocation() => RysyState.CmdArguments.Portable ? "portableData" : SaveLocation;

    private static string UncachedGetSaveLocation() {
        // from FNA wiki
        string osConfigDir = Environment.GetEnvironmentVariable("XDG_DATA_HOME")!;
        if (string.IsNullOrEmpty(osConfigDir)) {
            osConfigDir = Environment.GetEnvironmentVariable("HOME")!;
            if (string.IsNullOrEmpty(osConfigDir)) {
                return "."; // Oh well.
            }
            osConfigDir += "/.local/share";
        }
        return Path.Combine(osConfigDir, "Rysy");
    }

    public override void Init() {
        base.Init();

        Logger.UseColorsInConsole = true;
    }

    private LayeredFilesystem? _fontFilesystem;
    
    public override IModFilesystem? GetSystemFontsFilesystem() {
        if (_fontFilesystem is { })
            return _fontFilesystem;

        _fontFilesystem = new LayeredFilesystem();
        if (Path.Exists("/usr/share/fonts"))
            _fontFilesystem.AddFilesystem(new FolderModFilesystem("/usr/share/fonts"), "/usr/share/fonts");
        if (Path.Exists("/usr/local/share/fonts"))
            _fontFilesystem.AddFilesystem(new FolderModFilesystem("/usr/local/share/fonts"), "/usr/local/share/fonts");
        
        return _fontFilesystem;
    }
}
