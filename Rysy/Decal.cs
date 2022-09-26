using Rysy.Graphics;
using System.Text.RegularExpressions;

namespace Rysy;

public sealed partial class Decal : IPackable
{
    [RegexGenerator("\\d+$|\\.png")]
    public static partial Regex NumberTrimEnd();

    public Vector2 Pos;
    public Vector2 Scale;
    public string Texture = "";

    public BinaryPacker.Element Pack()
    {
        return new()
        {
            Attributes = new()
            {
                ["x"] = Pos.X,
                ["y"] = Pos.Y,
                ["scaleX"] = Scale.X,
                ["scaleY"] = Scale.Y,
                ["texture"] = Texture.TrimStart("decals/"),
            },
        };
    }

    public void Unpack(BinaryPacker.Element from)
    {
        Pos = new(from.Float("x"), from.Float("y"));
        Scale = new(from.Float("scaleX", 1), from.Float("scaleY", 1));
        Texture = "decals/" + from.Attr("texture", "").RegexReplace(NumberTrimEnd(), string.Empty).Unbackslash();
    }

    public Sprite GetSprite(bool fg)
        => ISprite.FromTexture(Pos, Texture).Centered() with
        {
            Depth = fg ? Depths.FGDecals : Depths.BGDecals, // TODO: Decal registry depth
            Scale = Scale
        };

    public static Decal Create(BinaryPacker.Element from)
    {
        var d = new Decal();
        d.Unpack(from);
        return d;
    }
}
