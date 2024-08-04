using Rysy.Entities;

namespace Rysy.History;

public class RoomAttributeChangeAction : IHistoryAction {
    private RoomAttributes Orig;
    private RoomResizeAction? Resize;
    private Entity? RemovedCheckpoint;
    private Entity? AddedCheckpoint;
    private bool NewRoom;

    private readonly Room Room;
    private readonly RoomAttributes Changed;

    public RoomAttributeChangeAction(Room room, RoomAttributes changed) {
        Room = room;
        Changed = changed.Copy();
    }

    public bool Apply(Map map) {
        NewRoom = !Room.Map.Rooms.Contains(Room);

        Orig = Room.Attributes.Copy();
        if (Orig != Changed) {
            if (Room.Width != Changed.Width || Room.Height != Changed.Height) {
                Resize = new RoomResizeAction(Room, Changed.Width, Changed.Height);
                Resize.Apply(map);
            }

            Room.Attributes = Changed.Copy();

            switch ((Orig.Checkpoint, Changed.Checkpoint)) {
                case (true, false):
                    // Remove checkpoint
                    RemovedCheckpoint = Room.Entities[typeof(Checkpoint)].First();
                    Room.Entities.Remove(RemovedCheckpoint);
                    break;
                case (false, true):
                    // Add checkpoint
                    var firstSpawnPoint = Room.Entities[typeof(Player)].FirstOrDefault()?.Pos ?? new Vector2();

                    AddedCheckpoint = EntityRegistry.Create(new("checkpoint") {
                        Attributes = new() {
                            ["bg"] = "",
                            ["checkpointID"] = -1,
                            ["allowOrigin"] = true,
                            ["x"] = firstSpawnPoint.X,
                            ["y"] = firstSpawnPoint.Y,
                        }
                    }, Room, false);

                    Room.Entities.Add(AddedCheckpoint);
                    break;
                default:
                    break;
            }

            if (NewRoom) {
                Room.Map.Rooms.Add(Room);
                map.SortRooms();
            }

            return true;
        }

        return false;
    }

    public void Undo(Map map) {
        Room.Attributes = Orig;
        Resize?.Undo(map);

        if (RemovedCheckpoint is { } removedCp) {
            Room.Entities.Add(removedCp);
        }
        if (AddedCheckpoint is { } addedCp) {
            Room.Entities.Remove(Room.Entities[typeof(Checkpoint)].First());
        }
        if (NewRoom) {
            Room.Map.Rooms.Remove(Room);
            map.SortRooms();
        }
    }
}
