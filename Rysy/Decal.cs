using Rysy.Graphics;
using Rysy.History;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Rysy;

public sealed class Decal : Entity {
    //[GeneratedRegex("\\d+$|\\.png")]
    public static Regex NumberTrimEnd = new("\\d+$|\\.png", RegexOptions.Compiled);

    [JsonIgnore]
    public bool FG { get; set; }

    public string Texture {
        get => Attr("texture");
        set {
            EntityData["texture"] = value;

            ClearRoomRenderCache();
        }
    }

    public float ScaleX {
        get => EntityData.Float("scaleX");
        set {
            EntityData["scaleX"] = value;
            ClearRoomRenderCache();
        }
    }

    public float ScaleY {
        get => EntityData.Float("scaleY");
        set {
            EntityData["scaleY"] = value;
            ClearRoomRenderCache();
        }
    }

    public Vector2 Scale {
        get => new(EntityData.Float("scaleX"), EntityData.Float("scaleY"));
        set {
            EntityData["scaleX"] = value.X;
            EntityData["scaleY"] = value.Y;

            ClearRoomRenderCache();
        }
    }

    public override int Depth => FG ? Depths.FGDecals : Depths.BGDecals; // TODO: Decal registry depth

    public override IEnumerable<ISprite> GetSprites() => GetSprite();

    public Sprite GetSprite()
        => ISprite.FromTexture(Pos, MapTextureToPath(Texture)).Centered() with {
            Depth = Depth,
            Scale = Scale
    };

    public static Decal Create(BinaryPacker.Element from, bool fg) {
        var d = new Decal();
        d.FG = fg;
        d.EntityData = new(fg ? EntityRegistry.FGDecalSID : EntityRegistry.BGDecalSID, from);
        return d;
    }

    private static string MapTextureToPath(string textureFromMap) {
        return "decals/" + textureFromMap.RegexReplace(NumberTrimEnd, string.Empty).Unbackslash();
    }

    public static Placement PlacementFromPath(string path, bool fg, Vector2 scale) {
        return new Placement(path) {
            ValueOverrides = new(StringComparer.Ordinal) {
                ["scaleX"] = scale.X,
                ["scaleY"] = scale.Y,
                ["texture"] = path,
            },
            PlacementHandler = fg ? EntityPlacementHandler.FGDecals : EntityPlacementHandler.BGDecals,
            SID = fg ? EntityRegistry.FGDecalSID : EntityRegistry.BGDecalSID,
        };
    }

    public override Entity? TryFlipHorizontal() {
        var clone = Clone().AsDecal()!;
        clone.ScaleX = -clone.ScaleX;
        return clone;
    }

    public override Entity? TryFlipVertical() {
        var clone = Clone().AsDecal()!;
        clone.ScaleY = -clone.ScaleY;
        return clone;
    }
}
