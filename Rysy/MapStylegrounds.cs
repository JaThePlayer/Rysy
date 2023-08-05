using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.Gui.FieldTypes;
using Rysy.Helpers;
using System.Text.Json.Serialization;

namespace Rysy;

public class MapStylegrounds : IPackable {
    public MapStylegrounds() { }

    public List<Style> Backgrounds = new();
    public List<Style> Foregrounds = new();

    /// <summary>
    /// Finds all styles in this <see cref="MapStylegrounds"/> object, recursively crawling all <see cref="StyleFolder"/>s
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Style> AllStylesRecursive() {
        foreach (var item in AllStylesIn(Backgrounds)) {
            yield return item;
        }
        foreach (var item in AllStylesIn(Foregrounds)) {
            yield return item;
        }
    }

    private IEnumerable<Style> AllStylesIn(List<Style> styles) {
        foreach (var style in styles) {
            if (style is StyleFolder folder) {
                yield return folder;

                foreach (var item in AllStylesIn(folder.Styles)) {
                    yield return item;
                }
            } else {
                yield return style;
            }
        }
    }

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

public abstract class Style : IPackable, IName {
    public static FieldList GetDefaultFields() => new FieldList(new {
        only = Fields.String("*").AllowNull().ConvertEmptyToNull(),
        exclude = Fields.String(null!).AllowNull().ConvertEmptyToNull(),
        flag = Fields.String(null!).AllowNull().ConvertEmptyToNull(),
        notflag = Fields.String(null!).AllowNull().ConvertEmptyToNull(),
        tag = Fields.String(null!).AllowNull().ConvertEmptyToNull(),
        _indent = new PaddingField()
    });

    public string Name { get; set; }

    public EntityData Data { get; set; }

    protected BinaryPacker.Element[]? UnhandledChildren;

    [JsonIgnore]
    public string Only => Data.Attr("only", "");

    [JsonIgnore]
    public StyleFolder? Parent { get; internal set; } = null;

    [JsonIgnore]
    public virtual string DisplayName => $"style.effects.{Name}.name".TranslateOrNull() ?? Name[(Name.LastIndexOf('/') + 1)..].Humanize();

    [JsonIgnore]
    public virtual bool CanBeInBackground => true;

    [JsonIgnore]
    public virtual bool CanBeInForeground => true;

    public virtual IEnumerable<ISprite> GetPreviewSprites() {
        yield return new PicoTextRectSprite("No preview") {
            Pos = new(0,0, 100, 150),
        };
    }

    public virtual BinaryPacker.Element Pack() {
        return new(Name) {
            Attributes = new(Data.Inner),
            Children = UnhandledChildren!,
        };
    }

    public virtual void Unpack(BinaryPacker.Element from) {
        Name = from.Name ?? throw new Exception("Style with no name???");
        Data = new(Name, from.Attributes);
        UnhandledChildren = from.Children;
    }

    [JsonIgnore]
    public virtual List<string>? AssociatedMods => null;

    public static Style FromName(string name) {
        return FromElement(new(name) {
            Children = Array.Empty<BinaryPacker.Element>(),
            Attributes = new()
        });
    }

    public static Style FromPlacement(Placement pl) {
        var data = EntityRegistry.GetDataFromPlacement(pl);

        return FromElement(new() {
            Attributes = data,
            Name = pl.SID
        });
    }

    public static Style FromElement(BinaryPacker.Element from) {
        if (from.Name is null) {
            throw new Exception("Style with null name???");
        }

        if (EntityRegistry.GetTypeForSID(from.Name) is { } t && t.IsSubclassOf(typeof(Style))) {
            var style = (Style) Activator.CreateInstance(t)!;
            style.Unpack(from);

            return style;
        }

        Logger.Write("Stylegrounds", LogLevel.Warning, $"Unknown style type: {from.Name}");

        var unk = new UnknownStyle();
        unk.Unpack(from);

        return unk;
    }
}

public abstract class StyleFolder : Style {
    public List<Style> Styles { get; set; }

    public virtual bool CanBeNested => true;

    public override BinaryPacker.Element Pack() {
        return new(Name) {
            Children = Styles.Select(s => s.Pack()).ToArray(),
            Attributes = new(Data.Inner),
        };
    }

    public override void Unpack(BinaryPacker.Element from) {
        Name = from.Name!;
        Data = new(from.Name!, from.Attributes);
        var children = from.Children ?? Array.Empty<BinaryPacker.Element>();

        Styles = new(children.Length);

        foreach (var style in children) {
            var inner = FromElement(style);
            inner.Parent = this;
            Styles.Add(inner);
        }
    }
}

public sealed class UnknownStyle : Style {
}