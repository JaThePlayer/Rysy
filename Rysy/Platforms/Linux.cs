using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rysy.Platforms;

public class Linux : RysyPlatform
{
    private static string SaveLocation = UncachedGetSaveLocation();

    public override string GetSaveLocation() => SaveLocation;

    private static string UncachedGetSaveLocation()
    {
        // from FNA wiki
        string osConfigDir = Environment.GetEnvironmentVariable("XDG_DATA_HOME")!;
        if (string.IsNullOrEmpty(osConfigDir))
        {
            osConfigDir = Environment.GetEnvironmentVariable("HOME")!;
            if (string.IsNullOrEmpty(osConfigDir))
            {
                return "."; // Oh well.
            }
            osConfigDir += "/.local/share";
        }
        return Path.Combine(osConfigDir, "Rysy");
    }
}
