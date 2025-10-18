using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;
using Rysy.History;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Rysy;

public sealed partial class Decal : Entity, IPlaceable {
    [GeneratedRegex(@"\d+$", RegexOptions.RightToLeft)]
    internal static partial Regex NumberAtEnd();

    [JsonIgnore]
    public bool FG { get; set; }

    public void OnCreated() {
        Texture = Texture.TrimStart("decals/");
    }

    public VirtTexture GetVirtTexture() => GFX.Atlas[MapTextureToPath(Texture)];

    private string? _texture;
    
    public string Texture {
        get => _texture ??= EntityData.Attr("texture");
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
    
    public float Parallax {
        get => EntityData.Float("parallax", 0f);
        set {
            EntityData["parallax"] = value;
            ClearRoomRenderCache();
        }
    }

    public override int Depth => Int("depth", FG ? Depths.FGDecals : Depths.BGDecals); // TODO: Decal registry depth

    public override IEnumerable<ISprite> GetSprites() => GetSprite();

    private object? _template;
    
    public ISprite GetSprite() {
        if (_template is null) {
            var path = MapTextureToPath(Texture);
            if (GFX.Atlas.GetSubtextures(path) is { Count: > 1 }) {
                var animation = SimpleAnimation.FromPathSubtextures(path, 12f);
            
                _template = new AnimatedSpriteTemplate(SpriteTemplate.FromTexture(path, Depth) with {
                    Scale = Scale,
                    Rotation = Rotation.ToRad(),
                    Origin = new(0.5f)
                }, animation);
            } else {
                _template = (SpriteTemplate.FromTexture(path, Depth) with {
                    Scale = Scale,
                    Rotation = Rotation.ToRad(),
                    Origin = new(0.5f)
                }).CreateColoredTemplate(Color);
            }
        }

        return _template switch {
            ColoredSpriteTemplate t => t.Create(Pos),
            AnimatedSpriteTemplate anim => anim.Create(Pos, Color),
            _ => ISprite.Point(Pos, Color.Red),
        };
    }

    public static Decal Create(BinaryPacker.Element from, bool fg, Room room) {
        from.Name = fg ? EntityRegistry.FGDecalSID : EntityRegistry.BGDecalSID;

        var dec = (Decal)EntityRegistry.Create(from, room, false);

        dec.Texture = dec.Texture.TrimEnd(".png", StringComparison.Ordinal);
        
        return dec;
    }

    /// <summary>
    /// Converts a decal path stored in the map .bin into a texture path that can be used to index <see cref="GFX.Atlas"/>
    /// </summary>
    public static string MapTextureToPath(string textureFromMap) {
        if (textureFromMap.EndsWith(".png", StringComparison.Ordinal)) {
            textureFromMap = textureFromMap[..^".png".Length];
        }

        var path = textureFromMap.RegexReplace(NumberAtEnd(), string.Empty).Unbackslash();
        if (!path.StartsWith("decals/", StringComparison.Ordinal))
            path = $"decals/{path}";
        
        return path;
    }

    /// <summary>
    /// Converts a texture path into the .bin format
    /// </summary>
    public static string PathToMapTexture(string texture) {
        var tSpan = texture.AsSpan();
        if (tSpan.StartsWith("decals/")) {
            tSpan = tSpan["decals/".Length..];
        }

        // Lonn requires the .png prefix or it will mess up decal paths
        return tSpan.EndsWith(".png") ? tSpan.ToString() : $"{tSpan}.png";
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
        ["depth"] = new NullableDepthField(),
        ["parallax"] = Fields.Float(0f),
    };

    protected override BinaryPacker.Element DoPack(bool trim) {
        var el = new BinaryPacker.Element(EntityData.SID);
        var attr = new Dictionary<string, object>(EntityData.Inner.Count, StringComparer.Ordinal) {
            ["x"] = X,
            ["y"] = Y,
            ["texture"] = PathToMapTexture(Texture),
            // omitting these crashes the game...
            ["scaleX"] = ScaleX,
            ["scaleY"] = ScaleY
        };

        if (Color != Color.White)
            attr["color"] = EntityData.Attr("color");

        var rotation = Rotation;
        if (rotation != 0f)
            attr["rotation"] = rotation;

        if (EntityData.TryGetValue("depth", out var d))
            attr["depth"] = d;

        var parallax = Parallax;
        if (parallax != 0f)
            attr["parallax"] = parallax;

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

    public override void OnChanged(EntityDataChangeCtx changed) {
        base.OnChanged(changed);

        _template = null;
        
        if (changed.IsChanged("texture"))
            _texture = null;
    }

    private static Cache<List<string>> _validDecalPaths;
    
    /// <summary>
    /// Stores all paths that can be used by decals.
    /// </summary>
    public static Cache<List<string>> ValidDecalPaths {
        get {
            if (_validDecalPaths is { } v)
                return v;

            var cacheToken = new CacheToken();
            var cache = new Cache<List<string>>(cacheToken, () => GFX.Atlas.GetTextures()
                .Where(p => p.virtPath.StartsWith("decals/", StringComparison.Ordinal))
                .SelectWhereNotNull(p => {
                    if (NumberAtEnd().Match(p.virtPath) is { Success: true } match) {
                        var frameNumber = int.Parse(match.ValueSpan, CultureInfo.InvariantCulture);
                        if (frameNumber > 0) {
                            return null;
                        }
                        
                        return p.virtPath["decals/".Length..^match.Length];
                    }
                    
                    return p.virtPath["decals/".Length..];
                }).ToList());
            _validDecalPaths = cache;

            GFX.Atlas.OnTextureLoad += path => {
                if (path.StartsWith("decals/", StringComparison.Ordinal)) {
                    cacheToken.Invalidate();
                }
            };
            GFX.Atlas.OnUnload += cacheToken.Invalidate;

            return cache;
        }
    }
}
