using Rysy.Graphics;
using Rysy.History;

namespace Rysy;

public static class EditorState {
    public static Camera Camera { get; set; } = new();

    private static Room? _currentRoom = null;
    public static Room? CurrentRoom {
        get => _currentRoom;
        set {
            if (_currentRoom == value)
                return;

            _currentRoom = value;
            RysyEngine.ForceActiveTimer = 0.25f;

            OnCurrentRoomChanged?.Invoke();
        }
    }

    public static Action? OnCurrentRoomChanged;

    private static Map? _currentMap = null;
    public static Map? Map {
        get => _currentMap;
        set {
            if (_currentMap == value) 
                return;

            _currentMap = value;

            OnMapChanged?.Invoke();
        }
    }

    public static Action? OnMapChanged;

    public static HistoryHandler History { get; set; }
}
