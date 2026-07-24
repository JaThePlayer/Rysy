using Rysy.Graphics;
using Rysy.History;
using Rysy.Layers;
using Rysy.LuaSupport;
using Rysy.Selections;

namespace Rysy.Helpers;

public static class CopypasteHelper {
    public record struct CopiedSelection {
        public CopiedSelection() { }

        public BinaryPacker.Element Data;
        public string Layer;

        public IEditorLayer? ResolveLayer(IReadOnlyList<IEditorLayer> layers) {
            return EditorLayers.EditorLayerFromName(Layer, layers);
        }

        public Entity? TryCreateEntity(Room room, IReadOnlyList<IEditorLayer> layers) {
            var layer = ResolveLayer(layers);
            if (layer is not EntityLayer)
                return null;
            
            var e = EntityRegistry.Create(Data, room, layer.SelectionLayer == SelectionLayer.Triggers, fromBinary: false);
            e.Id = -1; // set the ID to -1 so that it gets auto-assigned later

            return e;
        }

        public Placement CreatePlacement() {
            return new Placement(Data.Name ?? "?") {
                Sid = Data.Name, 
                ValueOverrides = Data.Attributes,
            };
        }
    }

    public static List<CopiedSelection>? GetSelectionsFromString(IReadOnlyList<IEditorLayer> layers, string selectionString) {
        if (JsonExtensions.TryDeserialize<List<CopiedSelection>>(selectionString, out var jsonSelections))
            return jsonSelections;

        if (LuaSerializer.TryGetSelectionsFromLuaString(layers, selectionString) is { } luaSelections)
            return luaSelections;

        return null;
    }

    public static List<CopiedSelection>? GetSelectionsFromClipboard(IReadOnlyList<IEditorLayer> layers) => GetSelectionsFromString(layers, Input.Clipboard.Get());

    public static List<Selection>? PasteSelectionsFromClipboard(IReadOnlyList<IEditorLayer> layers, EditorState editorState, IHistoryHandler? history, Map? map, Room room, Vector2 pos, out bool pastedRooms, int gridSize)
        => PasteSelections(layers, editorState, GetSelectionsFromClipboard(layers), history, map, room, pos, out pastedRooms, gridSize);

    public static List<Selection>? PasteSelections(IReadOnlyList<IEditorLayer> layers, EditorState editorState, List<CopiedSelection>? selections, IHistoryHandler? history, Map? map, Room room, Vector2 pos, out bool pastedRooms, int gridSize) {
        pastedRooms = false;

        var pasted = selections;
        if (pasted is null) {
            return null;
        }

        if (pasted.Any(p => p.ResolveLayer(layers) == EditorLayers.Room)) {
            pastedRooms = true;
            if (map is { })
                return PasteRoomSelections(layers, editorState, history, map, pasted, pos, gridSize);
            else
                return null;
        }

        var entitySelections = CreateSelectionsFromCopied(layers, room, pasted, out var entities);

        var offset = GetCenteringOffset(pos, entities.Select(e => e.Rectangle).Concat(GetTileRectangles(layers, pasted)).ToList(), gridSize);

        var actions = new List<IHistoryAction?>();
        
        var entityPlaceAction = PasteEntitylikeSelections(history, room, entitySelections, entities, offset);
        var tileSelections = PasteTileSelections(layers, room, pasted, offset);

        actions.Add(entityPlaceAction);
        foreach (var s in tileSelections) {
            actions.Add(s.Handler.PlaceClone(room));
        }
        
        history?.ApplyNewAction(actions.MergeActions());

        return entitySelections.Concat(tileSelections).ToList();
    }

    public static void CopyStringToClipboard(string? str) {
        if (str is not null) {
            Input.Clipboard.Set(str);
        }
    }

    public static void CopySelectionsToClipboard(IReadOnlyList<IEditorLayer> layers, List<Selection>? selections) {
        CopyStringToClipboard(CopySelectionsToString(layers, selections));
    }

    public static string? CopySelectionsToString(IReadOnlyList<IEditorLayer> layers, List<Selection>? selections) {
        if (CopySelections(selections) is { } copied) {
            if (LuaSerializer.ConvertSelectionsToLonnString(layers, copied) is { } lonnString)
                return lonnString;
            return copied.ToJson(minified: true);
        }

        return null;

        /*
static string Compress(byte[] input) {
    using (var result = new MemoryStream()) {
        var lengthBytes = BitConverter.GetBytes(input.Length);
        result.Write(lengthBytes, 0, 4);

        using (var compressionStream = new GZipStream(result,
            CompressionLevel.Optimal)) {
            compressionStream.Write(input, 0, input.Length);
            compressionStream.Flush();

        }
        var gzipBytes = result.ToArray();

        return Convert.ToBase64String(gzipBytes);
    }
}*/
    }

    public static void CopySelectionIDsToClipboard(List<Selection>? selections) {
        CopyStringToClipboard(CopySelectionIDsToString(selections));
    }

    public static string? CopySelectionIDsToString(List<Selection>? selections) {
        if (selections is not { }) 
            return null;

        return string.Join(", ", selections
            .DistinctBy(s => s.Handler is NodeSelectionHandler n ? n.Entity : s.Handler.Parent)
            .Select(x => x.Handler.Parent)
            .Where(x => x is Entity)
            .Select(x => ((Entity)x).Id.ToStringInvariant()));
    }

    public static void CopySelectionSIDsToClipboard(List<Selection>? selections) {
        CopyStringToClipboard(CopySelectionSIDsToString(selections));
    }

    public static string? CopySelectionSIDsToString(List<Selection>? selections) {
        if (selections is not { }) 
            return null;

        return string.Join(", ", selections
            .Select(x => x.Handler.Parent)
            .Where(x => x is Entity)
            .Select(x => ((Entity)x).Name)
            .Distinct());
    }

    public static List<CopiedSelection>? CopySelections(List<Selection>? selections) {
        if (selections is not { })
            return null;

        var copied = selections
            // if you selected multiple nodes of the same entity, don't copy the entity twice
            .DistinctBy(s => s.Handler is NodeSelectionHandler n ? n.Entity : s.Handler.Parent)
            .Select(s => new CopiedSelection() {
                Data = s.Handler.PackParent()!,
                Layer = s.Handler.Layer.Name,
            })
            .Where(s => s.Data is { })
            .ToList();

        return copied;
    }

    private static List<Selection> PasteRoomSelections(IReadOnlyList<IEditorLayer> layers, EditorState editorState, IHistoryHandler? history, Map map, List<CopiedSelection> pasted, Vector2? pos, int gridSize) {
        var rooms = pasted.Where(s => s.ResolveLayer(layers) == EditorLayers.Room).Select(s => {
            var room = new Room();
            room.Map = map;
            room.Unpack(s.Data);

            return room;
        }).ToList();

        var topLeft = new Vector2(rooms.Min(e => e.X), rooms.Min(e => e.Y)).Snap(gridSize);
        var bottomRight = new Vector2(rooms.Max(e => e.X + e.Width), rooms.Max(e => e.Y + e.Height)).Snap(gridSize);

        var mousePos = pos ?? editorState.Camera.ScreenToReal(Input.Global.Mouse.Pos).ToVector2().Snap(gridSize);

        var offset = (-topLeft + mousePos - ((bottomRight - topLeft) / 2f)).Snap(gridSize);

        foreach (var room in rooms) {
            room.Pos += offset;

            room.Name = room.Name.GetDeduplicatedIn(map.Rooms.Select(s => s.Name));
        }

        var selections = rooms.Select(r => new Selection(r.GetSelectionHandler())).ToList();

        history?.ApplyNewAction(rooms.Select(r => new AddRoomAction(r)).MergeActions());

        if (rooms.Count == 1) {
            editorState.CurrentRoom = rooms[0];
        }

        return selections;
    }

    private static Vector2 GetCenteringOffset(Vector2 pos, List<Rectangle> rectangles, int gridSize) {
        if (rectangles.Count == 0) {
            return pos;
        }

        var topLeft = new Vector2(rectangles.Min(e => e.X), rectangles.Min(e => e.Y));
        var bottomRight = new Vector2(rectangles.Max(e => e.Right), rectangles.Max(e => e.Bottom));
        var size = bottomRight - topLeft;

        var offset = pos - topLeft - (size / 2f).Floored();

        return offset.SnapRound(gridSize);
    }

    private static IHistoryAction? PasteEntitylikeSelections(IHistoryHandler? history, Room room, List<Selection> selections, List<Entity> entities, Vector2 centeringOffset) {
        if (entities.Count > 0) {
            var offset = centeringOffset;

            foreach (var entity in entities) {
                entity.Pos += offset;

                if (entity.Nodes is { } nodes)
                    foreach (var node in nodes) {
                        node.Pos += offset;
                    }
            }

            return AddEntityAction.AddAll(entities, room);
        }

        return null;
    }

    private static List<Rectangle> GetTileRectangles(IReadOnlyList<IEditorLayer> layers, List<CopiedSelection> pasted) {
        // Tile grid size is always 8.
        return pasted.Where(pasted => pasted.ResolveLayer(layers) is TileEditorLayer).Select(s => {
            var (w, h) = (s.Data.Int("w"), s.Data.Int("h"));
            var (x, y) = (s.Data.Int("x"), s.Data.Int("y"));

            var rPos = new Vector2(x, y).GridPosRound(8);

            return new Rectangle(rPos.X * 8, rPos.Y * 8, w * 8, h * 8);
        }).ToList();
    }

    private static List<Selection> PasteTileSelections(IReadOnlyList<IEditorLayer> layers, Room room, List<CopiedSelection> pasted, Vector2 offset) {
        // Tile grid size is always 8.
        var newSelections = pasted.Where(pasted => pasted.ResolveLayer(layers) is TileEditorLayer).Select(s => {
            var (w, h) = (s.Data.Int("w"), s.Data.Int("h"));
            var (x, y) = (s.Data.Int("x"), s.Data.Int("y"));
            var layer = (TileEditorLayer)s.ResolveLayer(layers)!;
            var dest = layer.GetGrid(room);
            var tilegrid = Tilegrid.FromString(w * 8, h * 8, s.Data.Attr("text"));
            var rPos = (new Vector2(x, y) + offset).GridPosFloor(8);

            var handler = new TileSelectionHandler(dest, new(rPos.X * 8, rPos.Y * 8, w * 8, h * 8), tilegrid.Tiles, layer);
            return new Selection(handler);
        }).ToList();

        return newSelections;
    }

    private static List<Selection> CreateSelectionsFromCopied(IReadOnlyList<IEditorLayer> layers, Room room, List<CopiedSelection> pasted, out List<Entity> entities) {
        entities = new();
        var entitiesNotRef = entities;

        var newSelections = pasted.SelectWhereNotNull(x => x.TryCreateEntity(room, layers)).SelectMany(e => {
            entitiesNotRef.Add(e);
            var handler = (EntitySelectionHandler)e.CreateSelection().Handler;
            var selections = e.Nodes?.Select<Node, ISelectionHandler>(n => new NodeSelectionHandler(handler, n)) ?? [];

            return selections.Append(handler);
        }).Select(h => new Selection { Handler = h }).ToList();

        return newSelections;
    }
}
