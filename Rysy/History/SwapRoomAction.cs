namespace Rysy.History;

public record class SwapRoomAction(Room Orig, Room New) : IHistoryAction {
    public bool Apply() {
        Swap(Orig, New);

        return true;
    }

    private void Swap(Room orig, Room swapped) {
        var map = orig.Map;

        map.Rooms.Remove(orig);
        map.Rooms.Add(swapped);

        if (EditorState.CurrentRoom == orig) {
            EditorState.CurrentRoom = swapped;
        }
    }

    public void Undo() {
        Swap(New, Orig);
    }
}
