using Rysy.Shared.Collections;

namespace Rysy.Stylegrounds;

public class MapStylegrounds : IPackable {
    public MapStylegrounds() { }

    public List<Style> Backgrounds = [];
    public List<Style> Foregrounds = [];

    /// <summary>
    /// Finds all styles in this <see cref="MapStylegrounds"/> object, recursively crawling all <see cref="StyleFolder"/>s
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Style> AllStylesRecursive() {
        var enumerator = AllStylesIn(Backgrounds).GetEnumerator();
        while (enumerator.MoveNext())
            yield return enumerator.Current;
        enumerator.Dispose();

        enumerator = AllStylesIn(Foregrounds).GetEnumerator();
        while (enumerator.MoveNext())
            yield return enumerator.Current;
        enumerator.Dispose();
    }

    public IEnumerable<Style> AllBackgroundStylesRecursive()
        => AllStylesIn(Backgrounds);

    public IEnumerable<Style> AllForegroundStylesRecursive()
        => AllStylesIn(Foregrounds);
    
    /// <summary>
    /// Returns all tags used by stylegrounds in the map.
    /// </summary>
    public IReadOnlySet<string> AllTags()
        => AllStylesRecursive().SelectMany(s => s.Tags).ToHashSet();

    public void ClearFakePreviewData() {
        foreach (var style in AllStylesRecursive())
            style.Data.SetOverlay(null);
    }

    private static PooledList<Style> AllStylesIn(List<Style> styles) {
        PooledList<Style> ret = new();
        foreach (var style in styles)
        {
            ret.Add(style);
            
            if (style is StyleFolder folder) {
                using var innerStyles = AllStylesIn(folder.Styles).GetEnumerator();
                while (innerStyles.MoveNext()) {
                    ret.Add(folder);
                }
            }
        }

        return ret;
    }

    public BinaryPacker.Element Pack() {
        return new("Style") {
            Children = [
                new("Foregrounds") {
                    Children = Foregrounds.Select(f => f.Pack()).ToArray(),
                },
                new("Backgrounds") {
                    Children = Backgrounds.Select(f => f.Pack()).ToArray(),
                }
            ],
        };
    }

    public void Unpack(BinaryPacker.Element from) {
        foreach (var c in from.Children) {
            List<Style> styles = new(c.Children.Length);

            foreach (var style in c.Children)
                styles.Add(Style.FromElement(style));

            if (c.Name == "Backgrounds")
                Backgrounds = styles;
            else if (c.Name == "Foregrounds")
                Foregrounds = styles;
        }
    }
}
