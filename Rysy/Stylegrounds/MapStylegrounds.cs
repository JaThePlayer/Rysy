using Rysy.Helpers;
using Rysy.Shared.Collections;

namespace Rysy.Stylegrounds;

public class MapStylegrounds : IPackable {
    private EntityData _data = new("Style", new Dictionary<string, object>());
    
    public MapStylegrounds() { }

    public List<Style> Backgrounds = [];
    public List<Style> Foregrounds = [];

    public Color BackgroundColor {
        get => _data.GetColor("color", Color.Black, ColorFormat.Rgb);
        set => _data["color"] = value.ToString(ColorFormat.Rgb);
    }

    /// <summary>
    /// Finds all styles in this <see cref="MapStylegrounds"/> object, recursively crawling all <see cref="StyleFolder"/>s
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Style> AllStylesRecursive() {
        var list = AllStylesIn(Backgrounds);
        var enumerator = list.GetEnumerator();
        while (enumerator.MoveNext())
            yield return enumerator.Current;
        enumerator.Dispose();
        list.Dispose();

        list = AllStylesIn(Foregrounds);
        enumerator = list.GetEnumerator();
        while (enumerator.MoveNext())
            yield return enumerator.Current;
        enumerator.Dispose();
        list.Dispose();
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

    private static PooledList<Style> AllStylesIn(List<Style> styles, PooledList<Style>? into = null) {
        PooledList<Style> ret = into ?? new();
        foreach (var style in styles)
        {
            ret.Add(style);
            
            if (style is StyleFolder folder) {
                ret = AllStylesIn(folder.Styles, ret);
            }
        }

        return ret;
    }

    public BinaryPacker.Element Pack() {
        return new("Style") {
            Attributes = _data.Inner,
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
        _data = new EntityData("Style", from.Attributes);
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
