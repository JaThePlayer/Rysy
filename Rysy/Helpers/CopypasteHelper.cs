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
            
            var e = EntityRegistry.Create(Data, room, layer.SelectionLayer == SelectionLayer.Triggers);
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

    public static List<Selection>? PasteSelectionsFromClipboard(IReadOnlyList<IEditorLayer> layers, EditorState editorState, HistoryHandler? history, Map? map, Room room, Vector2 pos, out bool pastedRooms)
        => PasteSelections(layers, editorState, GetSelectionsFromClipboard(layers), history, map, room, pos, out pastedRooms);

    public static List<Selection>? PasteSelections(IReadOnlyList<IEditorLayer> layers, EditorState editorState, List<CopiedSelection>? selections, HistoryHandler? history, Map? map, Room room, Vector2 pos, out bool pastedRooms) {
        pastedRooms = false;

        var pasted = selections;
        if (pasted is null) {
            return null;
        }

        if (pasted.Any(p => p.ResolveLayer(layers) == EditorLayers.Room)) {
            pastedRooms = true;
            if (map is { })
                return PasteRoomSelections(layers, editorState, history, map, pasted, pos);
            else
                return null;
        }

        var entitySelections = CreateSelectionsFromCopied(layers, room, pasted, out var entities);

        var offset = GetCenteringOffset(pos, entities.Select(e => e.Rectangle).Concat(GetTileRectangles(layers, pasted)).ToList());

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

    public static void CopySelectionsToClipboard(IReadOnlyList<IEditorLayer> layers, List<Selection>? selections) {
        if (CopySelectionsToString(layers, selections) is { } copied) {
            Input.Clipboard.Set(copied);
        }
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

    private static List<Selection> PasteRoomSelections(IReadOnlyList<IEditorLayer> layers, EditorState editorState, HistoryHandler? history, Map map, List<CopiedSelection> pasted, Vector2? pos) {
        var rooms = pasted.Where(s => s.ResolveLayer(layers) == EditorLayers.Room).Select(s => {
            var room = new Room();
            room.Map = map;
            room.Unpack(s.Data);

            return room;
        }).ToList();

        var topLeft = new Vector2(rooms.Min(e => e.X), rooms.Min(e => e.Y)).Snap(8);
        var bottomRight = new Vector2(rooms.Max(e => e.X + e.Width), rooms.Max(e => e.Y + e.Height)).Snap(8);

        var mousePos = pos ?? editorState.Camera.ScreenToReal(Input.Global.Mouse.Pos).ToVector2().Snap(8);

        var offset = (-topLeft + mousePos - ((bottomRight - topLeft) / 2f)).Snap(8);

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

    private static Vector2 GetCenteringOffset(Vector2 pos, List<Rectangle> rectangles) {
        if (rectangles.Count == 0) {
            return pos;
        }

        var topLeft = new Vector2(rectangles.Min(e => e.X), rectangles.Min(e => e.Y)).Snap(8);
        var bottomRight = new Vector2(rectangles.Max(e => e.Right), rectangles.Max(e => e.Bottom)).Snap(8);

        var offset = (-topLeft + pos - ((bottomRight - topLeft) / 2f)).Snap(8);

        return offset;
    }

    private static IHistoryAction? PasteEntitylikeSelections(HistoryHandler? history, Room room, List<Selection> selections, List<Entity> entities, Vector2 centeringOffset) {
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
        return pasted.Where(pasted => pasted.ResolveLayer(layers) is TileEditorLayer).Select(s => {
            var (w, h) = (s.Data.Int("w"), s.Data.Int("h"));
            var (x, y) = (s.Data.Int("x"), s.Data.Int("y"));

            var rPos = new Vector2(x, y).GridPosFloor(8);

            return new Rectangle(rPos.X * 8 - 8, rPos.Y * 8 - 8, w * 8, h * 8);
        }).ToList();
    }

    private static List<Selection> PasteTileSelections(IReadOnlyList<IEditorLayer> layers, Room room, List<CopiedSelection> pasted, Vector2 offset) {
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
            var handler = e.CreateSelection().Handler as EntitySelectionHandler;
            var selections = e.Nodes?.Select<Node, ISelectionHandler>(n => new NodeSelectionHandler(handler!, n)) ?? [];

            return selections.Append(handler);
        }).Select(h => new Selection() { Handler = h }).ToList();

        return newSelections;
    }
}
