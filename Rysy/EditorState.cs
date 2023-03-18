using Rysy.Graphics;
using Rysy.History;

namespace Rysy;

public static class EditorState {
    public static Camera Camera { get; set; } = new();

    private static Room _currentRoom = null!;
    public static Room CurrentRoom {
        get => _currentRoom;
        set {
            _currentRoom = value;
            RysyEngine.ForceActiveTimer = 0.25f;

            OnCurrentRoomChanged?.Invoke();
        }
    }

    public static event Action? OnCurrentRoomChanged;

    public static HistoryHandler History { get; set; }
}
