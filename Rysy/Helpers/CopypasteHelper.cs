using Rysy.Extensions;
using Rysy.Graphics;
using Rysy.History;
using Rysy.LuaSupport;
using System.Linq;

namespace Rysy.Helpers;

public static class CopypasteHelper {
    public struct CopiedSelection {
        public CopiedSelection() { }

        public BinaryPacker.Element Data;
        public SelectionLayer Layer;
    }

    public static List<CopiedSelection>? GetSelectionsFromString(string selectionString) {
        if (JsonExtensions.TryDeserialize<List<CopiedSelection>>(selectionString) is { } jsonSelections)
            return jsonSelections;

        if (LuaSerializer.TryGetSelectionsFromLuaString(selectionString) is { } luaSelections)
            return luaSelections;

        return null;
    }

    public static List<CopiedSelection>? GetSelectionsFromClipboard() => GetSelectionsFromString(Input.Clipboard.Get());

    public static List<Selection>? PasteSelectionsFromClipboard(HistoryHandler? history, Map? map, Room room, Vector2 pos, out bool pastedRooms)
        => PasteSelections(GetSelectionsFromClipboard(), history, map, room, pos, out pastedRooms);

    public static List<Selection>? PasteSelections(List<CopiedSelection>? selections, HistoryHandler? history, Map? map, Room room, Vector2 pos, out bool pastedRooms) {
        pastedRooms = false;

        var pasted = selections;
        if (pasted is null) {
            return null;
        }

        if (pasted.Any(p => p.Layer == SelectionLayer.Rooms)) {
            pastedRooms = true;
            if (map is { })
                return PasteRoomSelections(history, map, pasted, pos);
            else
                return null;
        }

        var entitySelections = CreateSelectionsFromCopied(room, pasted, out var entities);

        var offset = GetCenteringOffset(pos, entities.Select(e => e.Rectangle).Concat(GetTileRectangles(pasted)).ToList());

        var entityPlaceAction = PasteEntitylikeSelections(history, room, entitySelections, entities, offset);
        //var entitySelections = PasteEntitylikeSelections(history, room, pasted, pos);
        var tileSelections = PasteTileSelections(history, room, pasted, offset, out var tileAction);

        history?.ApplyNewAction(new MergedAction(entityPlaceAction, tileAction));

        return entitySelections.Concat(tileSelections).ToList();
    }

    public static void CopySelectionsToClipboard(List<Selection>? selections) {
        if (CopySelectionsToString(selections) is { } copied) {
            Input.Clipboard.Set(copied);
        }
    }

    public static string? CopySelectionsToString(List<Selection>? selections) {
        if (CopySelections(selections) is { } copied) {
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
                Layer = s.Handler.Layer,
            })
            .Where(s => s.Data is { })
            .ToList();

        return copied;
    }

    private static List<Selection> PasteRoomSelections(HistoryHandler? history, Map map, List<CopiedSelection> pasted, Vector2? pos) {
        var rooms = pasted.Where(s => s.Layer == SelectionLayer.Rooms).Select(s => {
            var room = new Room();
            room.Map = map;
            room.Unpack(s.Data);

            return room;
        }).ToList();

        var topLeft = new Vector2(rooms.Min(e => e.X), rooms.Min(e => e.Y)).Snap(8);
        var bottomRight = new Vector2(rooms.Max(e => e.X + e.Width), rooms.Max(e => e.Y + e.Height)).Snap(8);

        var mousePos = pos ?? EditorState.Camera.ScreenToReal(Input.Global.Mouse.Pos).ToVector2().Snap(8);

        var offset = (-topLeft + mousePos - ((bottomRight - topLeft) / 2f).Snap(8)).ToPoint();

        foreach (var room in rooms) {
            room.X += offset.X;
            room.Y += offset.Y;
            room.Name = room.Name.GetDeduplicatedIn(map.Rooms.Select(s => s.Name));
        }

        var selections = rooms.Select(r => new Selection(r.GetSelectionHandler())).ToList();

        history?.ApplyNewAction(rooms.Select(r => new AddRoomAction(map, r)).MergeActions());

        if (rooms.Count == 1) {
            EditorState.CurrentRoom = rooms[0];
        }

        return selections;
    }

    private static Vector2 GetCenteringOffset(Vector2 pos, List<Rectangle> rectangles) {
        var topLeft = new Vector2(rectangles.Min(e => e.X), rectangles.Min(e => e.Y)).Snap(8);
        var bottomRight = new Vector2(rectangles.Max(e => e.Right), rectangles.Max(e => e.Bottom)).Snap(8);

        var offset = (-topLeft + pos - ((bottomRight - topLeft) / 2f)).Snap(8);

        return offset;
    }

    /*
    private static List<Selection> PasteEntitylikeSelections(HistoryHandler? history, Room room, List<CopiedSelection> pasted, Vector2 pos) {
        List<Selection> newSelections = CreateSelectionsFromCopied(room, pasted, out var entities);

        if (entities.Count > 0) {
            var offset = GetCenteringOffset(pos, entities.Select(e => e.Rectangle).ToList());

            foreach (var entity in entities) {
                entity.Pos += offset;

                if (entity.Nodes is { } nodes)
                    foreach (var node in nodes) {
                        node.Pos += offset;
                    }
            }

            history?.ApplyNewAction(AddEntityAction.AddAll(entities, room));
        }

        return newSelections;
    }*/

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

    private static List<Rectangle> GetTileRectangles(List<CopiedSelection> pasted) {
        return pasted.Where(pasted => pasted.Layer is SelectionLayer.BGTiles or SelectionLayer.FGTiles).Select(s => {
            var (w, h) = (s.Data.Int("w"), s.Data.Int("h"));
            var (x, y) = (s.Data.Int("x"), s.Data.Int("y"));

            var rPos = new Vector2(x, y).GridPosFloor(8);

            return new Rectangle(rPos.X * 8 - 8, rPos.Y * 8 - 8, w * 8, h * 8);
        }).ToList();
    }

    private static List<Selection> PasteTileSelections(HistoryHandler? history, Room room, List<CopiedSelection> pasted, Vector2 offset, out IHistoryAction action) {
        //if (history is null)
        //    return new();

        var actions = new List<IHistoryAction>();

        var newSelections = pasted.Where(pasted => pasted.Layer is SelectionLayer.BGTiles or SelectionLayer.FGTiles).Select(s => {
            var (w, h) = (s.Data.Int("w"), s.Data.Int("h"));
            var (x, y) = (s.Data.Int("x"), s.Data.Int("y"));

            var dest = s.Layer switch {
                SelectionLayer.FGTiles => room.FG,
                _ => room.BG,
            };
            var tilegrid = Tilegrid.FromString(w * 8, h * 8, s.Data.Attr("text"));
            var rPos = new Vector2(x, y).GridPosFloor(8);


            //offset = GetCenteringOffset(pos, new() { new(rPos.X * 8 - 8, rPos.Y * 8 - 8, w * 8, h * 8) });
            rPos += (offset / 8).ToPoint();


            var handler = new Tilegrid.RectSelectionHandler(dest, new(rPos.X * 8 - 8, rPos.Y * 8, w * 8, h * 8), tilegrid.Tiles, s.Layer);
            var move = handler.MoveBy(new(8, 0));
            actions.Add(move);
            //history?.ApplyNewAction(move);
            //move.Apply();

            return new Selection(handler);
        }).SelectWhereNotNull(s => s).ToList();

        action = actions.MergeActions();
        return newSelections;
    }

    private static List<Selection> CreateSelectionsFromCopied(Room room, List<CopiedSelection> pasted, out List<Entity> entities) {
        entities = new();
        var entitiesNotRef = entities;

        var newSelections = pasted.Where(pasted => pasted.Layer is SelectionLayer.Entities or SelectionLayer.Triggers).SelectMany(s => {
            var e = EntityRegistry.Create(s.Data, room, s.Layer == SelectionLayer.Triggers);
            e.ID = 0; // set the ID to 0 so that it gets auto-assigned later
            entitiesNotRef.Add(e);

            var selections = e.Nodes?.Select<Node, ISelectionHandler>(n => new NodeSelectionHandler(e, n)) ?? Array.Empty<ISelectionHandler>();

            return selections.Append(new EntitySelectionHandler(e));
        }).Select(h => new Selection() { Handler = h }).ToList();

        return newSelections;
    }
}
