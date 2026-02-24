using Rysy.Graphics;
using Rysy.History;
using Rysy.Signals;

namespace Rysy;

/// <summary>
/// An object containing information about the current state of the editor.
/// </summary>
public class EditorState : ISignalEmitter {
    public EditorState() {
        
    }
    
    /// <summary>
    /// Shortcut to get the editor state associated with the current scene.
    /// </summary>
    public static EditorState? Current => RysyEngine.Scene.Get<EditorState>();
    
    /// <summary>
    /// The current camera.
    /// </summary>
    public Camera Camera => field ??= new Camera().ListenToViewportChanges();

    /// <summary>
    /// If the current scene is <see cref="Scenes.EditorScene"/>, gets/sets the currently selected room.
    /// </summary>
    public Room? CurrentRoom {
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
    public event Action? OnCurrentRoomChanged;

    /// <summary>
    /// If the current scene is <see cref="Scenes.EditorScene"/>, gets/sets the currently selected map.
    /// </summary>
    public Map? Map {
        get;
        set {
            var old = field;
            if (old == value)
                return;
            old?.Dispose();
            field = value;

            CurrentRoom = null;

            this.Emit(new MapSwapped(this, old, value));
            OnMapChanged?.Invoke();
        }
    } = null;

    /// <summary>
    /// Called whenever <see cref="Map"/> gets changed.
    /// </summary>
    public Action? OnMapChanged { get; set; }

    /// <summary>
    /// The currently used history handler
    /// </summary>
    public HistoryHandler? History { get; set; }

    public Colorgrade CurrentColograde {
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

    SignalTarget ISignalEmitter.SignalTarget { get; set; }
}
