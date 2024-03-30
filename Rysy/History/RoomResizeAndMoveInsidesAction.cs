using Rysy.Extensions;

namespace Rysy.History;

public record RoomResizeAndMoveInsidesAction(RoomRef Room, int ResizeX, int ResizeY, Vector2 Move) : IHistoryAction {
    private RoomResizeAction? _resizeAction;
    private RoomMoveAction? _moveAction;

    private List<IHistoryAction>? _moveActions;

    public bool Apply(Map map) {
        var room = Room.Resolve(map);
        
        var shouldMove = Move != Vector2.Zero;

        var fgTiles = shouldMove ? (char[,]) room.FG.Tiles.Clone() : null;
        var bgTiles = shouldMove ? (char[,]) room.BG.Tiles.Clone() : null;

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

    private void MoveAll(Room room, Vector2 offset) {
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
    }
}
