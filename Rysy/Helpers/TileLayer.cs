using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Layers;
using System.Diagnostics;

namespace Rysy.Helpers;

public sealed class TileLayer : IEquatable<TileLayer> {
    public const string GuidEntityDataName = "__extraTileLayerGuid";
    public const string NameEntityDataName = "__extraTileLayerName";

    public string Name { get; set; }

    public string DisplayName => Name.TranslateOrNull("rysy.tileLayers.name") ?? Name;
    
    public string? Tooltip => Name.TranslateOrNull("rysy.tileLayers.tooltip");

    public Guid Guid { get; init; }

    public BuiltinTypes Type { get; init; }

    private TileEditorLayer? _editorLayer;
    public TileEditorLayer EditorLayer => _editorLayer ??= new(this);

    public EntityData EntityData;

    public TileLayer(string name, Guid guid, BuiltinTypes type) {
        Name = name;
        Guid = guid;
        Type = type;
        DefaultDepth = type switch {
            BuiltinTypes.Bg => Depths.BGTerrain,
            _ => Depths.FGTerrain
        };

        EntityData = new(guid.ToString(), Pack());
    }

    public int DefaultDepth { get; set; }

    public Color DefaultColor { get; set; } = Color.White;
    
    public bool IsBuiltin => Name is "BG" or "FG";

    public static TileLayer BG { get; } = new("BG", Guid.Parse("00000000-0000-0000-0000-000000000000"), BuiltinTypes.Bg);
    public static TileLayer FG { get; } = new("FG", Guid.Parse("00000000-0000-0000-0000-000000000001"), BuiltinTypes.Fg);

    public BinaryPacker.Element Pack() {
        return new(Guid.ToString()) {
            Attributes = new() {
                ["name"] = Name,
                ["depth"] = DefaultDepth,
                ["color"] = DefaultColor.ToRGBAString(),
            }
        };
    }

    public bool Update(IReadOnlyDictionary<string, object> changed, Map map) {
        var edited = false;
        var data = new IDictionaryUntypedData(changed);
        
        if (data.Has("name")) {
            edited = true;
            Name = data.Attr("name");
        }
        
        if (data.Has("depth")) {
            edited = true;
            DefaultDepth = data.Int("depth");
        }
            
        if (data.Has("color")) {
            edited = true;
            DefaultColor = data.RGBA("color", Color.White);
        }

        if (edited) {
            foreach (var r in map.Rooms) {
                if (r.GetGrid(this) is { } grid) {
                    ApplyTo(grid.Tilegrid);
                }
            }
        }

        if (EntityData.FakeOverlay is null) {
            EntityData.BulkUpdate(changed);
        }

        return edited;
    }

    public void SetOverlay(Dictionary<string, object>? dictionary, Map map) {
        if (EntityData.FakeOverlay is null)
            EntityData.BulkUpdate(Pack().Attributes);
        
        EntityData.SetOverlay(dictionary);
        if (dictionary is {})
            Update(dictionary, map);
        else
            Update(EntityData, map);
    }

    public void ApplyTo(Tilegrid grid) {
        grid.Color = DefaultColor;
        grid.Depth = DefaultDepth;
    }

    private static readonly ValidationResult DepthNotUniqueResult 
        = new(ValidationMessage.Error(Gui.Tooltip.CreateTranslatedOrNull("rysy.validate.tileLayers.depthNotUnique")));
    
    public FieldList GetFields() => new(new {
        name = Fields.String(Name).WithValidator(x => {
            if (x.IsNullOrWhitespace())
                return ValidationResult.CantBeNull;
            return ValidationResult.Ok;
        }),
        depth = Fields.Depth(DefaultDepth).WithValidator(x => {
            if (x is not int depth)
                return ValidationResult.MustBeInt;

            if (EditorState.Map is { } map) {
                foreach (var layer in map.GetUsedTileLayers()) {
                    if (!ReferenceEquals(layer, this) && layer.DefaultDepth == depth) {
                        return DepthNotUniqueResult;
                    }
                }
            }
            
            return ValidationResult.Ok;
        }),
        color = Fields.RGBA(DefaultColor),
    });
    
    public override string ToString() {
        return Name;
    }

    public bool Equals(TileLayer? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Guid == other.Guid;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is TileLayer other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Guid.GetHashCode();
    }

    public enum BuiltinTypes {
        Fg,
        Bg
    }
}

public static class TileLayerExt {
    public static Autotiler GetAutotiler(this TileLayer.BuiltinTypes type, Map map) => type switch {
        TileLayer.BuiltinTypes.Bg => map.BGAutotiler,
        TileLayer.BuiltinTypes.Fg => map.FGAutotiler,
        _ => throw new UnreachableException()
    };

    public static string FastToString(this TileLayer.BuiltinTypes type) => type switch {
        TileLayer.BuiltinTypes.Bg => "Bg",
        TileLayer.BuiltinTypes.Fg => "Fg",
        _ => type.ToString()
    };
}