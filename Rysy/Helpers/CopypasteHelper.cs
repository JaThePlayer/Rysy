using Rysy.Extensions;
using Rysy.History;
using Rysy.LuaSupport;

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

        return PasteEntitylikeSelections(history, room, pasted, pos);
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

    private static List<Selection> PasteRoomSelections(HistoryHandler? history, Map map, List<CopiedSelection> pasted, Vector2 pos) {
        var rooms = pasted.Where(s => s.Layer == SelectionLayer.Rooms).Select(s => {
            var room = new Room();
            room.Map = map;
            room.Unpack(s.Data);

            return room;
        }).ToList();

        var topLeft = new Vector2(rooms.Min(e => e.X), rooms.Min(e => e.Y)).Snap(8);
        var bottomRight = new Vector2(rooms.Max(e => e.X + e.Width), rooms.Max(e => e.Y + e.Height)).Snap(8);

        var mousePos = EditorState.Camera.ScreenToReal(Input.Mouse.Pos).ToVector2().Snap(8);

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

    private static List<Selection> PasteEntitylikeSelections(HistoryHandler? history, Room room, List<CopiedSelection> pasted, Vector2 pos) {
        List<Selection> newSelections = CreateSelectionsFromCopied(room, pasted, out var entities);

        if (entities.Count > 0) {
            var topLeft = new Vector2(entities.Min(e => e.X), entities.Min(e => e.Y)).Snap(8);
            var bottomRight = new Vector2(entities.Max(e => e.Rectangle.Right), entities.Max(e => e.Rectangle.Bottom)).Snap(8);

            //var mousePos = room.WorldToRoomPos(EditorState.Camera, Input.Mouse.Pos).ToVector2().Snap(8);

            var offset = (-topLeft + pos - ((bottomRight - topLeft) / 2f)).Snap(8);

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
    }

    private static List<Selection> CreateSelectionsFromCopied(Room room, List<CopiedSelection> pasted, out List<Entity> entities) {
        entities = new();
        var entitiesNotRef = entities;

        var newSelections = pasted.SelectMany(s => {
            var e = EntityRegistry.Create(s.Data, room, s.Layer == SelectionLayer.Triggers);
            e.ID = 0; // set the ID to 0 so that it gets auto-assigned later
            entitiesNotRef.Add(e);

            var selections = e.Nodes?.Select<Node, ISelectionHandler>(n => new NodeSelectionHandler(e, n)) ?? Array.Empty<ISelectionHandler>();

            return selections.Append(new EntitySelectionHandler(e));
        }).Select(h => new Selection() { Handler = h }).ToList();

        return newSelections;
    }
}
