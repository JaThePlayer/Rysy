using Rysy.Mods;
using Rysy.Platforms;

namespace Rysy.Helpers;

/// <summary>
/// Represents a font family, allowing for easily obtaining file paths of various variants of the font.
/// </summary>
public readonly struct FontFamily(string name) {
    /// <summary>
    /// The name of this font family, without the .ttf postfix.
    /// </summary>
    public string Name { get; } = name;
    
    /// <summary>
    /// Creates a family from the given filename.
    /// If the filename points at a variant of a font, an attempt is made to find the source font.
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    public static FontFamily CreateFromFilename(string filename) {
        var name = filename.TrimPostfix(".ttf");

        if (name.EndsWith("-bold", StringComparison.OrdinalIgnoreCase)) {
            name = name.TrimPostfix("-bold", StringComparison.OrdinalIgnoreCase);
        } else if (name.EndsWith("-italic", StringComparison.OrdinalIgnoreCase)) {
            name = name.TrimPostfix("-italic", StringComparison.OrdinalIgnoreCase);
        } else if (name.EndsWith("-bolditalic", StringComparison.OrdinalIgnoreCase)) {
            name = name.TrimPostfix("-bolditalic", StringComparison.OrdinalIgnoreCase);
        }

        return new FontFamily(name);
    }

    /// <summary>
    /// Finds the filepath of the given font variant, returns the default variant if the specified variant is not available.
    /// </summary>
    public string FindFontVariantPath(bool bold, bool italic) {
        var font = Name;
        
        return (bold, italic) switch {
            (true, false) => Try($"{font}b.ttf") ?? Try($"{font}-Bold.ttf"),
            (false, true) => Try($"{font}i.ttf") ?? Try($"{font}-Italic.ttf"),
            (true, true) => Try($"{font}z.ttf") ?? Try($"{font}-BoldItalic.ttf"),
            (false, false) => null
        } ?? $"{font}.ttf";

        string? Try(string path)
            => ModRegistry.Filesystem.FileExists(path.AddPrefixIfNeeded("Rysy/fonts/"))
               || (RysyPlatform.Current.GetSystemFontsFilesystem()?.FileExists(path) ?? false) ? path : null;
    }
}
