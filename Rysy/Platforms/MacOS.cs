namespace Rysy.Platforms;

public class MacOS : RysyPlatform {
    private static string SaveLocation = UncachedGetSaveLocation();

    public override string GetSaveLocation() => SaveLocation;

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
}
