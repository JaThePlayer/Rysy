﻿using Rysy.Graphics;
using Rysy.History;

namespace Rysy;

/// <summary>
/// A singleton containing information about the current state of the editor.
/// </summary>
public static class EditorState {
    private static Camera? _camera;

    /// <summary>
    /// The current camera.
    /// </summary>
    public static Camera Camera {
        get => _camera ??= new();
        set => _camera = value;
    }

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
            RysyState.ForceActiveTimer = 0.25f;

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

            CurrentRoom = null;

            OnMapChanged?.Invoke();
        }
    }

    /// <summary>
    /// Called whenever <see cref="Map"/> gets changed.
    /// </summary>
    public static Action? OnMapChanged { get; set; }

    /// <summary>
    /// The currently used history handler
    /// </summary>
    public static HistoryHandler? History { get; set; }

    public static Colorgrade CurrentColograde {
        get {
            var colorgradeSetting = Persistence.Instance.ColorgradePreview;
            if (colorgradeSetting == Persistence.ColorgradePreviewMapDefaultValue) {
                if (Map is { Meta.ColorGrade: { } colorGrade }) {
                    return Colorgrade.FromPath(colorGrade);
                }
            } else {
                return Colorgrade.FromPath(colorgradeSetting);
            }
            
            return Colorgrade.None;
        }
    }
}
