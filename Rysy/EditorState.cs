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
    public static Camera Camera {
        get => field ??= new Camera().ListenToViewportChanges();
        set => field = value.ListenToViewportChanges();
    }

    /// <summary>
    /// If the current scene is <see cref="Scenes.EditorScene"/>, gets/sets the currently selected room.
    /// </summary>
    public static Room? CurrentRoom {
        get;
        set {
            if (field == value)
                return;

            field = value;
            RysyState.ForceActiveTimer = 0.25f;

            OnCurrentRoomChanged?.Invoke();
        }
    } = null;

    /// <summary>
    /// Called whenever <see cref="CurrentRoom"/> gets changed.
    /// </summary>
    public static event Action? OnCurrentRoomChanged;

    /// <summary>
    /// If the current scene is <see cref="Scenes.EditorScene"/>, gets/sets the currently selected map.
    /// </summary>
    public static Map? Map {
        get;
        set {
            if (field == value)
                return;
            field?.Dispose();
            field = value;

            CurrentRoom = null;

            OnMapChanged?.Invoke();
        }
    } = null;

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
