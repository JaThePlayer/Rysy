using Rysy.Entities;

namespace Rysy.History;

public class RoomAttributeChangeAction : IHistoryAction {
    private RoomAttributes _orig;
    private RoomResizeAction? _resize;
    private Entity? _removedCheckpoint;
    private Entity? _addedCheckpoint;
    private bool _newRoom;

    private readonly Room _room;
    private readonly RoomAttributes _changed;

    public RoomAttributeChangeAction(Room room, RoomAttributes changed) {
        _room = room;
        _changed = changed.Copy();
    }

    public bool Apply(Map map) {
        _newRoom = !_room.Map.Rooms.Contains(_room);

        _orig = _room.Attributes.Copy();
        if (_orig != _changed) {
            if (_room.Width != _changed.Width || _room.Height != _changed.Height) {
                _resize = new RoomResizeAction(_room, _changed.Width, _changed.Height);
                _resize.Apply(map);
            }

            _room.Attributes = _changed.Copy();

            switch ((_orig.Checkpoint, _changed.Checkpoint)) {
                case (true, false):
                    // Remove checkpoint
                    _removedCheckpoint = _room.Entities[typeof(Checkpoint)].First();
                    _room.Entities.Remove(_removedCheckpoint);
                    break;
                case (false, true):
                    // Add checkpoint
                    var firstSpawnPoint = _room.Entities[typeof(Player)].FirstOrDefault()?.Pos ?? new Vector2();

                    _addedCheckpoint = EntityRegistry.Create(new("checkpoint") {
                        Attributes = new() {
                            ["bg"] = "",
                            ["checkpointID"] = -1,
                            ["allowOrigin"] = true,
                            ["x"] = firstSpawnPoint.X,
                            ["y"] = firstSpawnPoint.Y,
                        }
                    }, _room, false);

                    _room.Entities.Add(_addedCheckpoint);
                    break;
                default:
                    break;
            }

            if (_newRoom) {
                _room.Map.Rooms.Add(_room);
                map.SortRooms();
            }

            return true;
        }

        return false;
    }

    public void Undo(Map map) {
        _room.Attributes = _orig;
        _resize?.Undo(map);

        if (_removedCheckpoint is { } removedCp) {
            _room.Entities.Add(removedCp);
        }
        if (_addedCheckpoint is { } addedCp) {
            _room.Entities.Remove(_room.Entities[typeof(Checkpoint)].First());
        }
        if (_newRoom) {
            _room.Map.Rooms.Remove(_room);
            map.SortRooms();
        }
    }
}
