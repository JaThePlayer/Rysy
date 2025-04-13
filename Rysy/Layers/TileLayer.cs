using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.Selections;

namespace Rysy.Layers; 

public sealed class TileEditorLayer(TileLayer layer) : EditorLayer(layer.Name) {
    public TileLayer TileLayer { get; init; } = layer;

    public override string Name => TileLayer.Name;

    public override string LocalizedName => TileLayer.IsBuiltin ? base.LocalizedName : $"{Name}##{TileLayer.Guid}";

    public override SelectionLayer SelectionLayer => TileLayer.Type switch {
        TileLayer.BuiltinTypes.Bg => SelectionLayer.BGTiles,
        TileLayer.BuiltinTypes.Fg => SelectionLayer.FGTiles,
        _ => throw new ArgumentOutOfRangeException()
    };

    public override IEnumerable<Placement> GetMaterials()
        => Array.Empty<Placement>();

    public Tilegrid GetGrid(Room room) {
        return room.GetOrCreateGrid(TileLayer).Tilegrid;
    }

    public Autotiler GetAutotiler(Map map) {
        return TileLayer.Type.GetAutotiler(map);
    }
}