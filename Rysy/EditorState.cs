using Rysy.Graphics;
using Rysy.History;

namespace Rysy;

/// <summary>
/// A singleton containing information about the current state of the editor.
/// </summary>
public static class EditorState {
    /// <summary>
    /// The current camera.
    /// </summary>
    public static Camera Camera { get; set; } = new();

    private static Room? _currentRoom = null;

    /// <summary>
    /// If the current scene is <see cref="Scenes.EditorScene"/>, gets/sets the currently selected room.
    /// </summary>
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

    /// <summary>
    /// Called whenever <see cref="CurrentRoom"/> gets changed.
    /// </summary>
    public static event Action? OnCurrentRoomChanged;

    private static Map? _currentMap = null;

    /// <summary>
    /// If the current scene is <see cref="Scenes.EditorScene"/>, gets/sets the currently selected map.
    /// </summary>
    public static Map? Map {
        get => _currentMap;
        set {
            if (_currentMap == value) 
                return;

            _currentMap = value;

            OnMapChanged?.Invoke();
        }
    }

    /// <summary>
    /// Called whenever <see cref="Map"/> gets changed.
    /// </summary>
    public static Action? OnMapChanged;

    /// <summary>
    /// The currently used history handler
    /// </summary>
    public static HistoryHandler? History { get; set; }
}
