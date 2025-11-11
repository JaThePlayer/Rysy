using Rysy.Graphics;
using Rysy.Helpers;
using Rysy.Selections;

namespace Rysy.Layers; 

public sealed class TileEditorLayer : EditorLayer {
    public TileLayer TileLayer { get; init; }
    
    public TileEditorLayer(TileLayer layer) {
        TileLayer = layer;
    }

    public override string Name => TileLayer.FastToString();

    public override SelectionLayer SelectionLayer => TileLayer switch {
        TileLayer.Bg => SelectionLayer.BgTiles,
        TileLayer.Fg => SelectionLayer.FgTiles,
        _ => throw new ArgumentOutOfRangeException()
    };

    public override IEnumerable<Placement> GetMaterials()
        => Array.Empty<Placement>();

    public Tilegrid GetGrid(Room room) => TileLayer switch {
        TileLayer.Bg => room.Bg,
        _ => room.Fg
    };
}