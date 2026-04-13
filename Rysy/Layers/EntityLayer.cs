using Rysy.Helpers;
using Rysy.LuaSupport;
using Rysy.Selections;

namespace Rysy.Layers; 

public class EntityLayer : EditorLayer, IPlacementEditorLayer, ISelectionEditorLayer, ILonnSerializableLayer {
    public EntityLayer(SelectionLayer layer) {
        SelectionLayer = layer;
    }

    public override string Name => SelectionLayer.FastToString();

    public override string? MaterialLangPrefix => SelectionLayer switch {
        SelectionLayer.Entities => "entities",
        SelectionLayer.Triggers => "triggers",
        _ => null,
    };

    public override SelectionLayer SelectionLayer { get; }
    public override IEnumerable<Placement> GetMaterials() {
        return SelectionLayer switch {
            SelectionLayer.Entities => EntityRegistry.EntityPlacements,
            SelectionLayer.Triggers => EntityRegistry.TriggerPlacements,
            SelectionLayer.FgDecals => FgDecalPlacements.Value,
            SelectionLayer.BgDecals => BgDecalPlacements.Value,
            _ => throw new NotImplementedException(SelectionLayer.FastToString())
        };
    }

    public override IReadOnlyList<Entity> GetContents(Room room) {
        return SelectionLayer switch {
            SelectionLayer.Entities => room.Entities,
            SelectionLayer.Triggers => room.Triggers,
            SelectionLayer.FgDecals => room.FgDecals,
            SelectionLayer.BgDecals => room.BgDecals,
            _ => throw new ArgumentOutOfRangeException(nameof(SelectionLayer))
        };
    }

    public List<Selection> GetSelectionsInRect(Map map, Room? room, Rectangle? rect) {
        if (room is null)
            return [];

        return room.GetSelectionsInRect(rect, SelectionLayer);
    }

    private static Cache<List<Placement>> FgDecalPlacements { get; }
        = Decal.ValidDecalPaths.Chain((paths) => paths.Select(p => PlacementFromString(p, SelectionLayer.FgDecals)!).ToList());
    private static Cache<List<Placement>> BgDecalPlacements { get; }
        = Decal.ValidDecalPaths.Chain((paths) => paths.Select(p => PlacementFromString(p, SelectionLayer.BgDecals)!).ToList());

    private static Placement? PlacementFromString(string str, SelectionLayer layer) {
        return layer switch {
            SelectionLayer.FgDecals => Decal.PlacementFromPath(str, true, Vector2.One, Color.White, rotation: 0f),
            SelectionLayer.BgDecals => Decal.PlacementFromPath(str, false, Vector2.One, Color.White, rotation: 0f),
            _ => null,
        };
    }

    public string? LonnLayerName => SelectionLayer switch {
        SelectionLayer.Entities => "entities",
        SelectionLayer.Triggers => "triggers",
        SelectionLayer.BgDecals => "decalsBg",
        SelectionLayer.FgDecals => "decalsFg",
        SelectionLayer.FgTiles => "tilesFg",
        SelectionLayer.BgTiles => "tilesBg",
        _ => null,
    };

    public string? LoennInstanceTypeName => SelectionLayer switch {
        SelectionLayer.Entities => "entity",
        SelectionLayer.Triggers => "trigger",
        _ => null,
    };

    public string? DefaultSid => SelectionLayer switch {
        SelectionLayer.BgDecals => EntityRegistry.BgDecalSid,
        SelectionLayer.FgDecals => EntityRegistry.FgDecalSid,
        SelectionLayer.FgTiles or SelectionLayer.BgTiles => "tiles",
        _ => null,
    };

    public BinaryPacker.Element ConvertToLonnFormat(CopypasteHelper.CopiedSelection item) {
        switch (SelectionLayer) {
            case SelectionLayer.FgDecals:
            case SelectionLayer.BgDecals:
                return AppendMissing(new BinaryPacker.Element {
                    Attributes = new Dictionary<string, object> {
                        ["_fromLayer"] = LonnLayerName!,
                        ["texture"] =
                            $"decals/{LuaSerializer.CorrectDecalPathForLonn(item.Data.Attr("texture").TrimStart("decals/"))}",
                        ["scaleX"] = item.Data.Float("scaleX", 1),
                        ["scaleY"] = item.Data.Float("scaleY", 1),
                        ["x"] = item.Data.Float("x", 0),
                        ["y"] = item.Data.Float("y", 0),
                    },
                }, item.Data, blacklist: ["texture", "scaleX", "scaleY", "x", "y"]);
            case SelectionLayer.Entities:
            case SelectionLayer.Triggers:
                return AppendMissing(new BinaryPacker.Element {
                    Attributes = new() {
                        ["_fromLayer"] = LonnLayerName!,
                        ["_name"] = item.Data.Name ?? "",
                        ["_id"] = item.Data.Int("id", 0),
                    }
                }, item.Data, blacklist: ["id"]);
        }

        throw new ArgumentException("Unknown selection layer: " + SelectionLayer);

        BinaryPacker.Element AppendMissing(BinaryPacker.Element target, BinaryPacker.Element source,
            HashSet<string> blacklist) {
            foreach (var (k, v) in source.Attributes) {
                if (blacklist.Contains(k))
                    continue;
                target.Attributes.TryAdd(k, v);
            }

            target.Children = source.Children;
            return target;
        }
    }
}