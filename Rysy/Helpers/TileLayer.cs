using Rysy.Graphics;
using Rysy.Layers;
using System.Diagnostics;

namespace Rysy.Helpers;

public sealed class TileLayer : IEquatable<TileLayer> {
    public const string GuidEntityDataName = "__extraTileLayerGuid";
    public const string NameEntityDataName = "__extraTileLayerName";
    
    public TileLayer(string name, Guid guid, BuiltinTypes type) {
        Name = name;
        Guid = guid;
        Type = type;
        Depth = type switch {
            BuiltinTypes.Bg => Depths.BGTerrain,
            _ => Depths.FGTerrain
        };
    }

    public string Name { get; init; }
    
    public Guid Guid { get; init; }

    public BuiltinTypes Type { get; init; }

    private TileEditorLayer? _editorLayer;
    public TileEditorLayer EditorLayer => _editorLayer ??= new(this);

    public int Depth { get; set; }
    
    public bool IsBuiltin => Name is "BG" or "FG";

    public static TileLayer BG { get; } = new("BG", Guid.Parse("00000000-0000-0000-0000-000000000000"), BuiltinTypes.Bg);
    public static TileLayer FG { get; } = new("FG", Guid.Parse("00000000-0000-0000-0000-000000000001"), BuiltinTypes.Fg);

    public override string ToString() {
        return Name;
    }

    public bool Equals(TileLayer? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Name == other.Name;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is TileLayer other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode(StringComparison.Ordinal);
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
}