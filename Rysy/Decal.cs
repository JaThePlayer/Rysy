using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.History;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Rysy;

public sealed partial class Decal : Entity, IPlaceable {
    [GeneratedRegex(@"\d+$|\.png$", RegexOptions.RightToLeft)]
    internal static partial Regex NumberOrPngExtTrimEnd();
    
    [GeneratedRegex(@"\d+$", RegexOptions.RightToLeft)]
    internal static partial Regex NumberAtEnd();

    [JsonIgnore]
    public bool FG { get; set; }

    public void OnCreated() {
        Texture = Texture.TrimStart("decals/");
    }

    public VirtTexture GetVirtTexture() => GFX.Atlas[MapTextureToPath(Texture)];

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

    /// <summary>
    /// Converts a decal path stored in the map .bin into a texture path that can be used to index <see cref="GFX.Atlas"/>
    /// </summary>
    public static string MapTextureToPath(string textureFromMap) {
        return "decals/" + textureFromMap.RegexReplace(NumberOrPngExtTrimEnd(), string.Empty).Unbackslash();
    }

    /// <summary>
    /// Retrieves the texture path from the given decal placement. This path can be used to index <see cref="GFX.Atlas"/>
    /// </summary>
    public static string GetTexturePathFromPlacement(Placement placement)
        => MapTextureToPath(placement["texture"]?.ToString() ?? "");

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

    public override Entity? TryRotate(RotationDirection dir) {
        var clone = Clone().AsDecal()!;
        clone.Rotation += dir.ToAndleRad().RadToDegrees();
        return clone;
    }

    public override Entity? RotatePreciseBy(float angleRad, Vector2 origin) {
        var clone = Clone().AsDecal()!;
        clone.Rotation = (clone.Rotation + angleRad.RadToDegrees().Floor()).SnapAngleToRightAnglesDegrees(5f);
        return clone;
    }

    public static PlacementList GetPlacements() => new();

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
            attr["color"] = EntityData.Attr("color");

        var rotation = Rotation;
        if (rotation != 0f)
            attr["rotation"] = rotation;

        el.Attributes = attr;

        return el;
    }

    public override void ClearRoomRenderCache() {
        if (Room is { } r) {
            if (FG)
                r.ClearFgDecalsRenderCache();
            else
                r.ClearBgDecalsRenderCache();
        }
    }

    private static Cache<List<string>> _ValidDecalPaths;
    
    /// <summary>
    /// Stores all paths that can be used by decals.
    /// </summary>
    public static Cache<List<string>> ValidDecalPaths {
        get {
            if (_ValidDecalPaths is { } v)
                return v;

            var cacheToken = new CacheToken();
            var cache = new Cache<List<string>>(cacheToken, () => GFX.Atlas.GetTextures()
                .Where(p => p.virtPath.StartsWith("decals/", StringComparison.Ordinal))
                .SelectWhereNotNull(p => {
                    if (NumberAtEnd().Match(p.virtPath) is { Success: true } match) {
                        var frameNumber = int.Parse(match.ValueSpan);
                        if (frameNumber > 0) {
                            return null;
                        }
                        
                        return p.virtPath["decals/".Length..^match.Length];
                    }
                    
                    return p.virtPath["decals/".Length..];
                }).ToList());
            _ValidDecalPaths = cache;

            GFX.Atlas.OnTextureLoad += (path) => {
                if (path.StartsWith("decals/", StringComparison.Ordinal)) {
                    cacheToken.Invalidate();
                }
            };
            GFX.Atlas.OnUnload += cacheToken.Invalidate;

            return cache;
        }
    }
}
