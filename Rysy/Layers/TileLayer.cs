using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.Selections;

namespace Rysy.Layers; 

public sealed class TileEditorLayer : EditorLayer {
    public TileLayer TileLayer { get; init; }
    
    public TileEditorLayer(TileLayer layer) {
        TileLayer = layer;
    }

    public override string Name => TileLayer.ToString();

    public override SelectionLayer SelectionLayer => TileLayer switch {
        TileLayer.BG => SelectionLayer.BGTiles,
        TileLayer.FG => SelectionLayer.FGTiles,
        _ => throw new ArgumentOutOfRangeException()
    };

    public override IEnumerable<Placement> GetMaterials()
        => Array.Empty<Placement>();

    public Tilegrid GetGrid(Room room) => TileLayer switch {
        TileLayer.BG => room.BG,
        _ => room.FG
    };
}