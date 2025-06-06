﻿using Rysy.Extensions;

namespace Rysy.Helpers;

/// <summary>
/// Provides an easy way to implement a gesture for selecting a rectangle, to be used for tools.
/// </summary>
public sealed class SelectRectangleGesture {
    public Point? StartPos { get; private set; } = null;

    private Rectangle? CurrentRect = null;
    private Rectangle? LastRect = null;

    public Input Input { get; private set; }

    /// <summary>
    /// Action to be called whenever this gesture is finished. The argument represents the area selected by the user.
    /// </summary>
    public Action<Rectangle>? OnSelectionFinish { get; set; }

    /// <summary>
    /// Transforms the world position into a position in an arbitrary space.
    /// </summary>
    public Func<Point, Point> Transform { get; set; } = (p) => p;

    public SelectRectangleGesture(Input input, Action<Rectangle>? onSelectionFinish = null) {
        RysyState.OnLoseFocus += OnLoseFocus;

        OnSelectionFinish = onSelectionFinish;
        Input = input ?? Input.Global;
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

    public Rectangle? Delta {
        get {
            if (StartPos is null)
                return null;

            if (LastRect is not { } last || CurrentRectangle is not { } current)
                return null;

            return new Rectangle(
                current.X - last.X,
                current.Y - last.Y,
                current.Width - last.Width,
                current.Height - last.Height
            );
        }
    }

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

        LastRect = CurrentRect;
        CurrentRect = CurrentRectangle;

        return ret;
    }

    public Point GetTransformedMousePos() => Transform(Input.Mouse.Pos);

    private static Rectangle SelectionRect(Point start, Point mousePos) {
        return RectangleExt.FromPoints(start, mousePos).AddSize(1, 1);
    }

    private void OnLoseFocus() {
        // After alt-tabbing and such, we should cancel the selection or we'll end up with accidental placements
        CancelGesture();
    }
}
