using Rysy.Mods;

namespace Rysy.Platforms;

public class Linux : RysyPlatform {
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
