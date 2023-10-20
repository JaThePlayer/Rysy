namespace Rysy.Stylegrounds;

public class MapStylegrounds : IPackable {
    public MapStylegrounds() { }

    public List<Style> Backgrounds = new();
    public List<Style> Foregrounds = new();

    /// <summary>
    /// Finds all styles in this <see cref="MapStylegrounds"/> object, recursively crawling all <see cref="StyleFolder"/>s
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Style> AllStylesRecursive() {
        foreach (var item in AllStylesIn(Backgrounds))
            yield return item;
        foreach (var item in AllStylesIn(Foregrounds))
            yield return item;
    }

    public IEnumerable<Style> AllBackgroundStylesRecursive()
        => AllStylesIn(Backgrounds);

    public IEnumerable<Style> AllForegroundStylesRecursive()
        => AllStylesIn(Foregrounds);

    public void ClearFakePreviewData() {
        foreach (var style in AllStylesRecursive())
            style.FakePreviewData = null;
    }

    private IEnumerable<Style> AllStylesIn(List<Style> styles) {
        foreach (var style in styles)
            if (style is StyleFolder folder) {
                yield return folder;

                foreach (var item in AllStylesIn(folder.Styles))
                    yield return item;
            } else
                yield return style;
    }

    public BinaryPacker.Element Pack() {
        return new("Style") {
            Children = new BinaryPacker.Element[] {
                new("Foregrounds") {
                    Children = Foregrounds.Select(f => f.Pack()).ToArray(),
                },
                new("Backgrounds") {
                    Children = Backgrounds.Select(f => f.Pack()).ToArray(),
                },
            },
        };
    }

    public void Unpack(BinaryPacker.Element from) {
        foreach (var c in from.Children) {
            List<Style> styles = new();

            foreach (var style in c.Children)
                styles.Add(Style.FromElement(style));

            if (c.Name == "Backgrounds")
                Backgrounds = styles;
            else if (c.Name == "Foregrounds")
                Foregrounds = styles;
        }
    }
}
