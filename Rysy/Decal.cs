using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.History;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Rysy;

public sealed class Decal : Entity, IPlaceable {
    //[GeneratedRegex("\\d+$|\\.png")]
    internal static Regex NumberTrimEnd { get; set; } = new("\\d+$|\\.png", RegexOptions.Compiled);

    [JsonIgnore]
    public bool FG { get; set; }

    public void OnCreated() {
        Texture = Texture.TrimStart("decals/");
    }

    public string Texture {
        get => Attr("texture");
        set {
            EntityData["texture"] = value;

            ClearRoomRenderCache();
        }
    }

    public float ScaleX {
        get => EntityData.Float("scaleX", 1);
        set {
            EntityData["scaleX"] = value;
            ClearRoomRenderCache();
        }
    }

    public float ScaleY {
        get => EntityData.Float("scaleY", 1);
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

    public float Rotation {
        get => EntityData.Float("rotation");
        set {
            EntityData["rotation"] = value;
            ClearRoomRenderCache();
        }
    }

    public Color Color {
        get => EntityData.RGBA("color", Color.White);
        set {
            EntityData["color"] = value.ToRGBAString();
            ClearRoomRenderCache();
        }
    }

    public sealed override int Depth => FG ? Depths.FGDecals : Depths.BGDecals; // TODO: Decal registry depth

    public sealed override IEnumerable<ISprite> GetSprites() => GetSprite();

    public Sprite GetSprite()
        => ISprite.FromTexture(Pos, MapTextureToPath(Texture)).Centered() with {
            Depth = Depth,
            Scale = Scale,
            Rotation = Rotation.ToRad(),
            Color = Color,
        };

    public static Decal Create(BinaryPacker.Element from, bool fg, Room room) {
        //var d = new Decal();
        //d.FG = fg;
        //d.EntityData = new(fg ? EntityRegistry.FGDecalSID : EntityRegistry.BGDecalSID, from);

        from.Name = fg ? EntityRegistry.FGDecalSID : EntityRegistry.BGDecalSID;

        return (Decal)EntityRegistry.Create(from, room, false);
    }

    private static string MapTextureToPath(string textureFromMap) {
        return "decals/" + textureFromMap.RegexReplace(NumberTrimEnd, string.Empty).Unbackslash();
    }

    public static Placement PlacementFromPath(string path, bool fg, Vector2 scale, Color color, float rotation) {
        return new Placement(path) {
            ValueOverrides = new(StringComparer.Ordinal) {
                ["scaleX"] = scale.X,
                ["scaleY"] = scale.Y,
                ["texture"] = path,
                ["rotation"] = rotation,
                ["color"] = color.ToRGBAString(),
            },
            PlacementHandler = fg ? EntityPlacementHandler.FGDecals : EntityPlacementHandler.BGDecals,
            SID = fg ? EntityRegistry.FGDecalSID : EntityRegistry.BGDecalSID,
        };
    }

    public sealed override Entity? TryFlipHorizontal() {
        var clone = Clone().AsDecal()!;
        clone.ScaleX = -clone.ScaleX;
        return clone;
    }

    public sealed override Entity? TryFlipVertical() {
        var clone = Clone().AsDecal()!;
        clone.ScaleY = -clone.ScaleY;
        return clone;
    }

    public static List<Placement>? GetPlacements() => new();

    public static FieldList GetFields() => new() { 
        ["texture"] = Fields.String(""),
        ["color"] = Fields.RGBA(Color.White),
        ["scaleX"] = Fields.Float(1f),
        ["scaleY"] = Fields.Float(1f),
        ["rotation"] = Fields.Float(0f),
    };

    public override BinaryPacker.Element Pack() {
        var el = new BinaryPacker.Element(EntityData.SID);
        var attr = new Dictionary<string, object>(EntityData.Inner.Count, StringComparer.Ordinal) {
            ["x"] = X,
            ["y"] = Y,
            ["texture"] = Texture,
            // omitting these crashes the game...
            ["scaleX"] = ScaleX,
            ["scaleY"] = ScaleY
        };

        if (Color != Color.White)
            attr["color"] = EntityData.Inner["color"];

        var rotation = Rotation;
        if (rotation != 0f)
            attr["rotation"] = rotation;

        el.Attributes = attr;

        return el;
    }
}
