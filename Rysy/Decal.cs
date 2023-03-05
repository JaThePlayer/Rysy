using Rysy.Graphics;
using Rysy.History;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Rysy;

public sealed partial class Decal : IPackable, ISelectionHandler {
    //[GeneratedRegex("\\d+$|\\.png")]
    public static Regex NumberTrimEnd = new("\\d+$|\\.png", RegexOptions.Compiled);

    public Vector2 Pos;
    public Vector2 Scale;
    public string Texture = "";
    public int EditorLayer;

    /// <summary>
    /// The original texture, as stored in map data. Needed to be able to differenciate between decals ending with 00 and not.
    /// </summary>
    private string OrigTexture;

    [JsonIgnore]
    public Room Room { get; set; }

    [JsonIgnore]
    public bool FG { get; set; }

    public BinaryPacker.Element Pack() {
        var attributes = new Dictionary<string, object>(5 + EditorLayer != 0 ? 1 : 0) {
            ["x"] = Pos.X,
            ["y"] = Pos.Y,
            ["scaleX"] = Scale.X,
            ["scaleY"] = Scale.Y,
            ["texture"] = OrigTexture,
        };

        if (EditorLayer != 0) {
            attributes["_editorLayer"] = EditorLayer;
        }

        return new("decal") {
            Attributes = attributes,
        };
    }

    public void Unpack(BinaryPacker.Element from) {
        Pos = new(from.Float("x"), from.Float("y"));
        Scale = new(from.Float("scaleX", 1), from.Float("scaleY", 1));
        OrigTexture = from.Attr("texture", "");
        EditorLayer = from.Int("_editorLayer", 0);

        Texture = "decals/" + OrigTexture.RegexReplace(NumberTrimEnd, string.Empty).Unbackslash();
    }

    public Sprite GetSprite()
        => ISprite.FromTexture(Pos, Texture).Centered() with {
            Depth = FG ? Depths.FGDecals : Depths.BGDecals, // TODO: Decal registry depth
            Scale = Scale
        };

    public static Decal Create(BinaryPacker.Element from) {
        var d = new Decal();
        d.Unpack(from);
        return d;
    }

    public Selection GetSelection() => Selection.FromSprite(this, GetSprite());

    public IHistoryAction MoveBy(Vector2 offset) {
        return new MoveDecalAction(this, offset);
    }

    public IHistoryAction DeleteSelf() {
        return new RemoveDecalAction(this, Room);
    }
}
