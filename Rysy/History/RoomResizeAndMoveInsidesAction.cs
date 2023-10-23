using Rysy.Extensions;

namespace Rysy.History;

public record class RoomResizeAndMoveInsidesAction(Room Room, int ResizeX, int ResizeY, Vector2 Move) : IHistoryAction {
    private RoomResizeAction? _resizeAction;
    private RoomMoveAction? _moveAction;

    private List<IHistoryAction>? _moveActions;

    public bool Apply() {
        var shouldMove = Move != Vector2.Zero;

        var fgTiles = shouldMove ? (char[,]) Room.FG.Tiles.Clone() : null;
        var bgTiles = shouldMove ? (char[,]) Room.BG.Tiles.Clone() : null;

        if (ResizeX != 0 || ResizeY != 0) {
            _resizeAction ??= new(Room, Room.Width + ResizeX, Room.Height + ResizeY);
            _resizeAction.Apply();
        }
        
        if (shouldMove) {
            _moveAction ??= new(Room, (int)Move.X.Div(8), (int) Move.Y.Div(8));
            _moveAction.Apply();

            _moveActions = new();
            MoveAll(-Move);

            var tileOffset = (-Move / 8).ToPoint();
            _moveActions.Add(new TilegridMoveActionAfterResize(Room.FG, fgTiles!, tileOffset.X, tileOffset.Y));
            _moveActions.Add(new TilegridMoveActionAfterResize(Room.BG, bgTiles!, tileOffset.X, tileOffset.Y));
        }

        _moveActions = _moveActions?.Where(a => a.Apply()).ToList();

        return true;
    }

    private void MoveAll(Vector2 offset) {
        foreach (var e in Room.Entities.Concat(Room.Triggers).Concat(Room.FgDecals).Concat(Room.BgDecals)) {
            e.Pos += offset;
            foreach (var n in e.Nodes) {
                n.Pos += offset;
            }
        }

        Room.ClearRenderCache();
    }

    public void Undo() {
        if (_moveAction is { }) {
            _moveAction?.Undo();
            MoveAll(Move);
            if (_moveActions is { })
                foreach (var item in _moveActions) {
                    item.Undo();
                }
            _moveActions?.Clear();
        }
        _resizeAction?.Undo();
    }
}
