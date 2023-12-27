namespace Rysy.Stylegrounds;

public abstract class StyleFolder : Style {
    public const string EditorNameDataKey = "_editorName";

    public string? EditorName => Data.Attr(EditorNameDataKey, null!);
    
    public override string DisplayName => EditorName ?? base.DisplayName;
    
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
        var children = from.Children ?? Array.Empty<BinaryPacker.Element>();
        Styles = new(children.Length);
        Data = new(from.Name!, from.Attributes);

        foreach (var style in children) {
            var inner = FromElement(style);
            inner.Parent = this;
            Styles.Add(inner);
        }
    }

    public override void OnChanged(EntityDataChangeCtx ctx) {
        base.OnChanged(ctx);

        foreach (var child in Styles) {
            child.OnChanged(ctx);
        }
    }
}
