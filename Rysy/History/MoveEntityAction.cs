namespace Rysy.History;

public record class MoveEntityAction(Entity Entity, Vector2 By) : IHistoryAction, ISerializableAction {
    public bool Apply() {
        Entity.Pos += By;

        Entity.ClearRoomRenderCache();

        return true;
    }

    public void Undo() {
        Entity.Pos -= By;

        Entity.ClearRoomRenderCache();
    }

    public Dictionary<string, object> GetSerializableData() {
        return new() {
            ["x"] = By.X,
            ["y"] = By.Y,
            ["id"] = Entity.ID,
            ["room"] = Entity.RoomName
        };
    }

    public static ISerializableAction FromSerializable(Map map, Dictionary<string, object> data) {
        var room = map.TryGetRoomByName((string) data["room"]);
        var entity = room?.TryGetEntityById(Convert.ToInt32(data["id"], CultureInfo.InvariantCulture));
        var by = new Vector2(Convert.ToSingle(data["x"], CultureInfo.InvariantCulture), Convert.ToSingle(data["y"], CultureInfo.InvariantCulture));

        return new MoveEntityAction(entity!, by);
    }
}
