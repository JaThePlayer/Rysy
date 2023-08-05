using Rysy.Graphics;

namespace Rysy.Stylegrounds;

[CustomEntity("parallax")]
public sealed class Parallax : Style, IPlaceable {
    public override string DisplayName => Texture;

    public string Texture => Data.Attr("texture");

    public Color Color => Data.GetColor("color", Color.White, Helpers.ColorFormat.RGB);

    public static FieldList GetFields() => new(new {
        texture = Fields.AtlasPath("bgs/07/07/bg00", "(bgs/.*)"),
        blendmode = Fields.Dropdown(BlendModes[0], BlendModes, editable: true),
        alpha = 1f,
        color = Fields.RGB(Color.White),
        scrollx = 0f,
        scrolly = 0f,
        speedx = 0f,
        speedy = 0f,
        x = 0f,
        y = 0f,
        fadeIn = false,
        flipx = false,
        flipy = false,
        fadex = Fields.String(null!).AllowNull(), // todo: custom field
        fadey = Fields.String(null!).AllowNull(), // todo: custom field
        instantOut = false,
        instantIn = false,
        loopx = false,
        loopy = false,

    });

    public static PlacementList GetPlacements() => new("parallax");

    public override IEnumerable<ISprite> GetPreviewSprites()
        => string.IsNullOrWhiteSpace(Texture) ? Array.Empty<ISprite>() : ISprite.FromTexture(Texture) with {
            Color = Color,
        };

    public static List<string> BlendModes = new() {
        "alphablend", "additive"
    };
}
