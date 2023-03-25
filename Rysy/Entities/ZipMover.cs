using Rysy.Graphics;
using Rysy.Helpers;

namespace Rysy.Entities;

[CustomEntity("zipMover")]
public class ZipMover : Entity, ISolid, IPlaceable {
    public enum Themes {
        Normal, Moon
    }

    public override int Depth => Depths.Solids;

    private bool Moon => Attr("theme", "Normal").Equals("moon", StringComparison.OrdinalIgnoreCase);

    public virtual string BaseDirectory => Moon ? "objects/zipmover/moon" : "objects/zipmover";
    public virtual Color FillColor => Moon ? Color.Transparent : Color.Black;

    public virtual string CogPath(string baseDirectory) => $"{baseDirectory}/cog";
    public virtual string LightPath(string baseDirectory) => $"{baseDirectory}/light01";
    public virtual string BlockPath(string baseDirectory) => $"{baseDirectory}/block";
    public virtual string InnerCogPath(string baseDirectory) => $"{baseDirectory}/innercog";

    public override IEnumerable<ISprite> GetSprites() {
        var w = Width;
        var h = Height;
        var baseDir = BaseDirectory;

        yield return ISprite.Rect(Pos - Vector2.One, w + 2, h + 2, FillColor);

        yield return ISprite.NineSliceFromTexture(Rectangle, BlockPath(baseDir));

        yield return ISprite.FromTexture(Pos + new Vector2(w / 2, 0), LightPath(baseDir)) with {
            Origin = new(.5f, 0f)
        };
    }

    public override Point MinimumSize => new(8, 8);

    public static FieldList GetFields() => new() {
        ["theme"] = Fields.EnumNamesDropdown(Themes.Normal)
    };

    public static List<Placement>? GetPlacements() => new() { 
        new("Zip Mover")
    };

    public override Range NodeLimits => 1..1;
}
