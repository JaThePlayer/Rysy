using Microsoft.Xna.Framework.Graphics.PackedVector;
using Rysy.Graphics;
using System;

namespace Rysy;

public class MapStylegrounds : IPackable {
    public MapStylegrounds() { }

    public List<Style> Backgrounds = new();
    public List<Style> Foregrounds = new();

    public BinaryPacker.Element Pack() {
        return new("Style") {
            Children = new BinaryPacker.Element[] {
                new("Foregrounds") {
                    Children = Foregrounds.Select(f => f.Pack()).ToArray(),
                },
                new("Backgrounds") {
                    Children = Backgrounds.Select(f => f.Pack()).ToArray(),
                },
            },
        };
    }

    public void Unpack(BinaryPacker.Element from) {
        foreach (var c in from.Children) {
            List<Style> styles = new();

            foreach (var style in c.Children) {
                styles.Add(Style.FromElement(style));
            }

            if (c.Name == "Backgrounds") {
                Backgrounds = styles;
            } else if (c.Name == "Foregrounds") {
                Foregrounds = styles;
            }
        }
    }
}

public abstract class Style : IPackable {
    public string Name { get; set; }

    public EntityData Data { get; set; }

    public string Only => Data.Attr("only", "");

    public Apply? Parent { get; internal set; } = null;

    public virtual IEnumerable<ISprite> GetPreviewSprites() {
        yield return new PicoTextRectSprite("No preview");
    }

    public virtual BinaryPacker.Element Pack() {
        return new(Name) {
            Attributes = new(Data.Inner),
        };
    }

    public virtual void Unpack(BinaryPacker.Element from) {
        Name = from.Name ?? throw new Exception("Style with no name???");
        Data = new(Name, from.Attributes);
    }

    public static Style FromElement(BinaryPacker.Element from) {
        if (from.Name is null) {
            throw new Exception("Style with null name???");
        }

        if (from.Name == "apply") {
            var apply = new Apply();
            apply.Unpack(from);

            return apply;
        }

        if (EntityRegistry.GetTypeForSID(from.Name) is { } t && t.IsSubclassOf(typeof(Style))) { 
            var style = (Style)Activator.CreateInstance(t)!;
            style.Unpack(from);

            return style;
        }

        Logger.Write("Stylegrounds", LogLevel.Warning, $"Unknown style type: {from.Name}");

        var unk = new UnknownStyle();
        unk.Unpack(from);

        return unk;
    }
}

public sealed class Apply : Style {
    public List<Style> Styles;

    public override BinaryPacker.Element Pack() {
        return new("apply") {
            Children = Styles.Select(s => s.Pack()).ToArray(),
            Attributes = new(Data.Inner),
        };
    }

    public override void Unpack(BinaryPacker.Element from) {
        Name = "apply";
        Data = new("apply", from.Attributes);
        Styles = new(from.Children.Length);

        foreach (var style in from.Children) {
            var inner = FromElement(style);
            inner.Parent = this;
            Styles.Add(inner);
        }
    }
}

[CustomEntity("parallax")]
public sealed class Parallax : Style, IPlaceable {
    public string Texture => Data.Attr("texture");

    public static FieldList GetFields() => new(new {
        texture = Fields.AtlasPath("", "(bgs/.*)"),
        only = Fields.String(null!).AllowNull(),
        exclude = Fields.String(null!).AllowNull(),
        tag = Fields.String(null!).AllowNull(),
        flag = Fields.String(null!).AllowNull(),
        notflag = Fields.String(null!).AllowNull(),
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
        => ISprite.FromTexture(Texture);

    public static List<string> BlendModes = new() {
        "alphablend", "additive"
    };
}

public sealed class UnknownStyle : Style {
}