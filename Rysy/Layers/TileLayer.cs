using Rysy.Graphics;
using Rysy.Gui;
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

    public override Searchable GetMaterialSearchable(object material) {
        if (material is not char c)
            return new Searchable(material.ToString()!);
        
        if (c is '0') {
            return new Searchable("Air");
        }

        var map = Registry?.Get<Map>();

        return new Searchable(GetAutotiler(map)?.GetTilesetDisplayName(c) ?? c.ToString());
    }
    
    public override ITooltip? GetMaterialTooltip(object material) {
        if (material is not char c)
            return null;
        var map = Registry?.Get<Map>();

        return new Tooltip($"""
            Id: {c}
            Source: {GetAutotiler(map)?.GetTilesetData(c)?.Filename}
            """);
    }

    public Tilegrid GetGrid(Room room) => TileLayer switch {
        TileLayer.Bg => room.Bg,
        _ => room.Fg
    };
    
    public Autotiler? GetAutotiler(Map? map) {
        if (map is { }) {
            return TileLayer switch {
                TileLayer.Fg => map.FgAutotiler,
                TileLayer.Bg => map.BgAutotiler,
                _ => null,
            };
        }

        return null;
    }

    public override bool SupportsPreciseMoveMode => false;

    public override int? ForcedGridSize => 8;
}