namespace Rysy.Platforms;

public class MacOs : RysyPlatform {
    private static string SaveLocation = UncachedGetSaveLocation();

    public override string GetSaveLocation() => RysyState.CmdArguments.Portable ? "portableData" : SaveLocation;

    private static string UncachedGetSaveLocation() {
        // from FNA wiki
        string osConfigDir = Environment.GetEnvironmentVariable("HOME")!;
        if (string.IsNullOrEmpty(osConfigDir)) {
            return "."; // Oh well.
        }
        return Path.Combine(
            osConfigDir,
            "Library/Application Support",
            "Rysy"
        );
    }

    public override void Init() {
        base.Init();

        Logger.UseColorsInConsole = true;
    }
}
