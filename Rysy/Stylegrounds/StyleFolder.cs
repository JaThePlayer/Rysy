namespace Rysy.Stylegrounds;

public abstract class StyleFolder : Style {
    public List<Style> Styles { get; set; }

    public virtual bool CanBeNested => true;

    public override BinaryPacker.Element Pack() {
        return new(Name) {
            Children = Styles.Select(s => s.Pack()).ToArray(),
            Attributes = new(Data.Inner),
        };
    }

    public override void Unpack(BinaryPacker.Element from) {
        Name = from.Name!;
        Data = new(from.Name!, from.Attributes);
        var children = from.Children ?? Array.Empty<BinaryPacker.Element>();

        Styles = new(children.Length);

        foreach (var style in children) {
            var inner = FromElement(style);
            inner.Parent = this;
            Styles.Add(inner);
        }
    }
}
