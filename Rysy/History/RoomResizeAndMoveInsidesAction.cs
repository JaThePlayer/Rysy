using Rysy.Entities;
using Rysy.Extensions;

namespace Rysy.History;

public record RoomResizeAndMoveInsidesAction(RoomRef Room, int ResizeX, int ResizeY, Vector2 Move, bool pushConnectedRooms) : IHistoryAction {
    private RoomResizeAction? _resizeAction;
    private RoomMoveAction? _moveAction;

    private List<IHistoryAction>? _moveActions;
    
    private readonly List<(RoomRef, Point)> _roomMoves = [];

    public bool Apply(Map map) {
        var room = Room.Resolve(map);
        
        var shouldMove = Move != Vector2.Zero;

        var fgTiles = shouldMove ? (char[,]) room.FG.Tiles.Clone() : null;
        var bgTiles = shouldMove ? (char[,]) room.BG.Tiles.Clone() : null;

        _roomMoves.Clear();
        if (pushConnectedRooms) {
            var origRect = room.Bounds;
            HashSet<Room> movedRooms = [ room ];
            // TODO: Check tiles, not just overlapping coords. Extract to not have so much copy-paste
            if (ResizeX > 0 && Move.X >= 0f) {
                // Push rooms on the right
                foreach (var otherRoom in map.Rooms) {
                    if (otherRoom == room)
                        continue;
                    
                    var otherRect = otherRoom.Bounds;

                    if (otherRoom.X == room.X + room.Width && otherRect.Top < origRect.Bottom && origRect.Top < otherRect.Bottom) {
                        Push(map, otherRoom, new (ResizeX, 0), movedRooms);
                    }
                }
            } else if (ResizeX > 0 && Move.X < 0f) {
                // Push rooms on the left
                foreach (var otherRoom in map.Rooms) {
                    if (otherRoom == room)
                        continue;
                    
                    var otherRect = otherRoom.Bounds;

                    if (otherRoom.Bounds.Right == room.X && otherRect.Top < origRect.Bottom && origRect.Top < otherRect.Bottom) {
                        Push(map, otherRoom, new (-ResizeX, 0), movedRooms);
                    }
                }
            }
            if (ResizeY > 0 && Move.Y >= 0f) {
                // Push rooms on the bottom
                foreach (var otherRoom in map.Rooms) {
                    if (otherRoom == room)
                        continue;
                    
                    var otherRect = otherRoom.Bounds;

                    if (otherRoom.Y == room.Y + room.Height && otherRect.Left < origRect.Right && origRect.Left < otherRect.Right) {
                        Push(map, otherRoom, new (0, ResizeY), movedRooms);
                    }
                }
            } else if (ResizeY > 0 && Move.Y < 0f) {
                // Push rooms on the bottom
                foreach (var otherRoom in map.Rooms) {
                    if (otherRoom == room)
                        continue;
                    
                    var otherRect = otherRoom.Bounds;

                    if (otherRoom.Bounds.Bottom == room.Y && otherRect.Left < origRect.Right && origRect.Left < otherRect.Right) {
                        Push(map, otherRoom, new (0, -ResizeY), movedRooms);
                    }
                }
            }
        }
        
        if (ResizeX != 0 || ResizeY != 0) {
            _resizeAction ??= new(Room, room.Width + ResizeX, room.Height + ResizeY);
            _resizeAction.Apply(map);
        }
        
        if (shouldMove) {
            _moveAction ??= new(Room, (int)Move.X.Div(8), (int) Move.Y.Div(8));
            _moveAction.Apply(map);

            _moveActions = new();
            MoveAll(room, -Move);

            var tileOffset = (-Move / 8).ToPoint();
            _moveActions.Add(new TilegridMoveActionAfterResize(room.FG, fgTiles!, tileOffset.X, tileOffset.Y));
            _moveActions.Add(new TilegridMoveActionAfterResize(room.BG, bgTiles!, tileOffset.X, tileOffset.Y));
        }

        _moveActions = _moveActions?.Where(a => a.Apply(map)).ToList();

        return true;
    }
    
        
    static bool IntersectsInclusive(Rectangle self, Rectangle other) {
        return other.Left <= self.Right && self.Left <= other.Right && other.Top <= self.Bottom && self.Top <= other.Bottom;
    }

    void Push(Map map, Room room, Point delta, HashSet<Room> movedRooms) {
        _roomMoves.Add((room, delta));
        movedRooms.Add(room);
        
        if (room.Entities.Any(e => e is Player))
            foreach (var otherRoom in map.Rooms) {
                if (otherRoom == room || movedRooms.Contains(otherRoom))
                    continue;

                var self = room.Bounds;
                var other = otherRoom.Bounds;

                if (IntersectsInclusive(self, other)) {
                    Push(map, otherRoom, delta, movedRooms);
                }
            }
        room.Pos += delta.ToVector2();
    }

    private void MoveAll(Room room, Vector2 offset) {
        offset = offset.Snap(8);
        
        foreach (var e in room.Entities.Concat(room.Triggers).Concat(room.FgDecals).Concat(room.BgDecals)) {
            e.Pos += offset;
            foreach (var n in e.Nodes) {
                n.Pos += offset;
            }
        }

        room.ClearRenderCache();
    }

    public void Undo(Map map) {
        var room = Room.Resolve(map);
        
        if (_moveAction is { }) {
            _moveAction?.Undo(map);
            MoveAll(room, Move);
            if (_moveActions is { })
                foreach (var item in _moveActions) {
                    item.Undo(map);
                }
            _moveActions?.Clear();
        }
        _resizeAction?.Undo(map);
        
        foreach (var (otherRoomRef, move) in _roomMoves) {
            otherRoomRef.Resolve(map).Pos -= move.ToVector2();
        }
    }
}
