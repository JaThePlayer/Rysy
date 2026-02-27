using Rysy.Graphics;
using Rysy.Gui;
using Rysy.Helpers;
using Rysy.LuaSupport;
using Rysy.Selections;

namespace Rysy.Layers; 

public sealed class TileEditorLayer : EditorLayer, ISelectionEditorLayer, ILonnSerializableLayer {
    public TileLayer TileLayer { get; init; }
    
    public int Depth { get; init; }
    
    public TileEditorLayer(TileLayer layer, int depth) {
        TileLayer = layer;
        Depth = depth;
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

    public List<Selection> GetSelectionsInRect(Map map, Room? room, Rectangle? rect) {
        if (room is null)
            return [];

        return room.GetSelectionsInRect(rect, SelectionLayer);
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

    public string? LonnLayerName => SelectionLayer switch {
        SelectionLayer.FgTiles => "tilesFg",
        SelectionLayer.BgTiles => "tilesBg",
        _ => null,
    };

    public string? LoennInstanceTypeName => null;

    public string DefaultSid => "tiles";
    
    public void ConvertToLonnFormat(TextWriter indentedWriter, CopypasteHelper.CopiedSelection item) {
        indentedWriter.WriteLine($$"""
        {
            _fromLayer = "{{LonnLayerName}}",
            tiles = {{LuaSerializer.ToLuaString(item.Data.Attr("text", ""))}},
            height = {{LuaSerializer.ToLuaString(item.Data.Int("h", 0))}},
            width = {{LuaSerializer.ToLuaString(item.Data.Int("w", 0))}},
            x = {{LuaSerializer.ToLuaString(item.Data.Int("x") / 8 + 1)}},
            y = {{LuaSerializer.ToLuaString(item.Data.Int("y") / 8 + 1)}},
        },
        """);
    }

    public BinaryPacker.Element ConvertToLonnFormat(CopypasteHelper.CopiedSelection item) {
        return new BinaryPacker.Element() {
            Attributes = new Dictionary<string, object>() {
                ["_fromLayer"] = LonnLayerName!,
                ["tiles"] = item.Data.Attr("text", ""),
                ["height"] = item.Data.Int("h"),
                ["width"] = item.Data.Int("w"),
                ["x"] = item.Data.Int("x") / 8 + 1,
                ["y"] = item.Data.Int("y") / 8 + 1,
            }
        };
    }
}