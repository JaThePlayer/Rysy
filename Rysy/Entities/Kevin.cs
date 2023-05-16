using Rysy.Extensions;
using Rysy.Graphics;

namespace Rysy.Entities;

[CustomEntity("crushBlock")]
public sealed class Kevin : Entity, IPlaceable {
    public override int Depth => 0;

    public override bool ResizableX => true;
    public override bool ResizableY => true;

    public override IEnumerable<ISprite> GetSprites() {
        var pos = Pos;
        var (w, h) = (Width, Height);

        var frame = Enum("axes", Axes.Both) switch {
            Axes.Horizontal => "objects/crushblock/block01",
            Axes.Vertical => "objects/crushblock/block02",
            Axes.Both => "objects/crushblock/block03",
            _ => "objects/crushblock/block00",
        };

        yield return ISprite.Rect(pos.Add(2, 2), w - 4, h - 4, new(98f / 255f, 34f / 255f, 43f / 255f));
        yield return ISprite.NineSliceFromTexture(pos, w, h, frame);
        yield return ISprite.FromTexture(pos.Add(w / 2, h / 2), "objects/crushblock/idle_face").Centered();
    }

    public static FieldList GetFields() => new(new {
        axes = Fields.EnumNamesDropdown(Axes.Both, k => k.ToLowerInvariant()),
        chillout = false
    });

    public static PlacementList GetPlacements() => PlacementList.FromEnum<Axes>(axis => new(axis.ToLowerInvariant(), new {
        axes = axis.ToLowerInvariant()
    }));

    enum Axes {
        Both,
        Vertical,
        Horizontal
    }
}