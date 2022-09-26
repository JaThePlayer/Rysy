using Rysy.Graphics.TextureTypes;
using System.IO.Compression;

namespace Rysy;

public static class ModAssetHelper
{
    /// <summary>
    /// Opens a file from a mod at <paramref name="relativePath"/>.
    /// This function doesn't know which mod the file is in, so it'll look at all mods until it finds it.
    /// </summary>
    public static Stream? OpenModFile(string relativePath)
    {
        foreach (var dir in Directory.GetDirectories(Settings.Instance.ModsDirectory))
        {
            var path = $"{dir}/{relativePath}";
            if (File.Exists(path))
                return File.OpenRead(path);
        }

        foreach (var zip in Directory.EnumerateFiles(Settings.Instance.ModsDirectory, "*.zip"))
        {
            var arch = ZipFile.OpenRead(zip);
            if (arch.GetEntry(relativePath) is { } entry)
            {
                return entry.Open();
            }
        }


        return null;
    }
}