namespace Rysy.Helpers;

/// <summary>
/// Provides an easy way to implement a gesture for selecting a rectangle, to be used for tools.
/// </summary>
public sealed class SelectRectangleGesture {
    private Point? StartPos = null;

    /// <summary>
    /// Action to be called whenever this gesture is finished. The argument represents the area selected by the user.
    /// </summary>
    public Action<Rectangle>? OnSelectionFinish { get; set; }

    /// <summary>
    /// Transforms the world position into a position in an arbitrary space.
    /// </summary>
    public Func<Point, Point> Transform { get; set; } = (p) => p;

    public SelectRectangleGesture(Action<Rectangle>? onSelectionFinish = null) {
        RysyEngine.OnLoseFocus += OnLoseFocus;

        OnSelectionFinish = onSelectionFinish;
    }

    /// <summary>
    /// Returns whether the gesture has started or not
    /// </summary>
    public bool Started => StartPos is { };

    /// <summary>
    /// Returns the currently selected rectangle, or null if <see cref="Started"/> returns false.
    /// </summary>
    public Rectangle? CurrentRectangle => StartPos switch {
        null => null,
        not null => SelectionRect(StartPos.Value, GetTransformedMousePos())
    };

    /// <summary>
    /// Cancels the current gesture, if it has started already.
    /// </summary>
    public void CancelGesture() {
        StartPos = null;
    }

    /// <summary>
    /// Updates the gesture. Needs to be called every frame.
    /// Returns the <see cref="CurrentRectangle"/> if the selection was just finished this frame.
    /// </summary>
    public Rectangle? Update(Func<Point, Point>? transform) {
        Transform = transform ?? ((p) => p);
        Rectangle? ret = null;

        switch (Input.Mouse.Left) {
            case MouseInputState.Released:
                if (Started) {
                    ret = CurrentRectangle;
                    OnSelectionFinish?.Invoke(ret!.Value);

                    CancelGesture();
                }
                break;
            case MouseInputState.Held:
                break;
            case MouseInputState.Clicked:
                StartPos = GetTransformedMousePos();
                break;
            default:
                break;
        }

        return ret;
    }

    private Point GetTransformedMousePos() => Transform(Input.Mouse.Pos);

    private static Rectangle SelectionRect(Point start, Point mousePos) {
        return RectangleExt.FromPoints(start, mousePos).AddSize(1, 1);
    }

    private void OnLoseFocus() {
        // After alt-tabbing and such, we should cancel the selection or we'll end up with accidental placements
        CancelGesture();
    }
}
