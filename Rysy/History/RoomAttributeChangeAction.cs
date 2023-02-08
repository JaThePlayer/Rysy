using Rysy.Entities;

namespace Rysy.History;

public record class RoomAttributeChangeAction(Room Room, RoomAttributes Changed) : IHistoryAction {
    private RoomAttributes Orig;
    private RoomResizeAction? Resize;
    private Entity? RemovedCheckpoint;
    private Entity? AddedCheckpoint;
    private bool NewRoom;

    public bool Apply() {
        NewRoom = !Room.Map.Rooms.Contains(Room);

        Orig = Room.Attributes;
        if (Orig != Changed) {
            if (Room.Width != Changed.Width || Room.Height != Changed.Height) {
                Resize = new RoomResizeAction(Room, Changed.Width, Changed.Height);
                Resize.Apply();
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
            }

            return true;
        }

        return false;
    }

    public void Undo() {
        Room.Attributes = Orig;
        Resize?.Undo();

        if (RemovedCheckpoint is { } removedCp) {
            Room.Entities.Add(removedCp);
        }
        if (AddedCheckpoint is { } addedCp) {
            Room.Entities.Remove(Room.Entities[typeof(Checkpoint)].First());
        }
        if (NewRoom) {
            Room.Map.Rooms.Remove(Room);
        }
    }
}
