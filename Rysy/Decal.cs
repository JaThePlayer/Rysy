using Rysy.Graphics;
using Rysy.History;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Rysy;

public sealed partial class Decal : IPackable, ISelectionHandler, IConvertibleToPlacement, IDepth {
    //[GeneratedRegex("\\d+$|\\.png")]
    public static Regex NumberTrimEnd = new("\\d+$|\\.png", RegexOptions.Compiled);

    public Vector2 Pos;
    public Vector2 Scale;
    public string Texture = "";
    public int EditorLayer;

    /// <summary>
    /// The original texture, as stored in map data. Needed to be able to differenciate between decals ending with 00 and not.
    /// </summary>
    public string OrigTexture;

    [JsonIgnore]
    public Room Room { get; set; }

    [JsonIgnore]
    public bool FG { get; set; }

    public int Depth => FG ? Depths.FGDecals : Depths.BGDecals; // TODO: Decal registry depth

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
        Texture = MapTextureToPath(OrigTexture);
    }

    private static string MapTextureToPath(string textureFromMap) {
        return "decals/" + textureFromMap.RegexReplace(NumberTrimEnd, string.Empty).Unbackslash();
    }

    public Sprite GetSprite()
        => ISprite.FromTexture(Pos, Texture).Centered() with {
            Depth = Depth,
            Scale = Scale
        };

    public static Decal Create(BinaryPacker.Element from) {
        var d = new Decal();
        d.Unpack(from);
        return d;
    }

    public Selection GetSelection() => Selection.FromSprite(this, GetSprite());

    public void ClearRoomRenderCache() {
        if (FG)
            Room.ClearFgDecalsRenderCache();
        else
            Room.ClearBgDecalsRenderCache();
    }

    #region ISelectionHandler
    object ISelectionHandler.Parent => this;

    IHistoryAction ISelectionHandler.MoveBy(Vector2 offset) {
        return new MoveDecalAction(this, offset);
    }

    IHistoryAction ISelectionHandler.DeleteSelf() {
        return new RemoveDecalAction(this, Room);
    }

    IHistoryAction? ISelectionHandler.TryResize(Point delta) {
        return null;
    }
    #endregion

    Placement IConvertibleToPlacement.ToPlacement() {
        return new Placement(Texture) {
            ValueOverrides = new(StringComparer.Ordinal) {
                ["scale"] = Scale,
                ["texture"] = Texture,
                ["origTexture"] = OrigTexture,
                ["editorLayer"] = EditorLayer,
            },
            PlacementHandler = FG ? DecalPlacementHandler.FGInstance : DecalPlacementHandler.BGInstance
        };
    }

    public static Decal FromPlacement(Placement placement, Vector2 pos, Room room, bool fg) {
        var overrides = placement.ValueOverrides;
        return new Decal() {
            EditorLayer = (int)overrides["editorLayer"],
            Scale = (Vector2)overrides["scale"],
            Texture = (string)overrides["texture"],
            OrigTexture = (string)overrides["origTexture"],
            Pos = pos,
            FG = fg,
            Room = room,
        };
    }

    public static Placement PlacementFromPath(string path, bool fg, Vector2 scale) {
        return new Placement(path) {
            ValueOverrides = new(StringComparer.Ordinal) {
                ["scale"] = scale,
                ["texture"] = MapTextureToPath(path),
                ["origTexture"] = path,
                ["editorLayer"] = Persistence.Instance.EditorLayer ?? 0,
            },
            PlacementHandler = fg ? DecalPlacementHandler.FGInstance : DecalPlacementHandler.BGInstance
        };
    }
}
