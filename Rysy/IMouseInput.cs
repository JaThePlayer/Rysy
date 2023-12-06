namespace Rysy;

public interface IMouseInput {
    public Point Offset { get; set; }

    bool AnyClickedOrHeld { get; }
    MouseInputState Left { get; }
    float LeftHoldTime { get; }
    MouseInputState Middle { get; }
    Point Pos { get; }
    Point PositionDelta { get; }
    MouseInputState Right { get; }
    float RightHoldTime { get; }
    int ScrollDelta { get; }
    MouseInputState X1 { get; }
    float X1HoldTime { get; }
    MouseInputState X2 { get; }
    float X2HoldTime { get; }

    bool Clicked(int button);
    void Consume(int button);
    void ConsumeLeft();
    void ConsumeMiddle();
    void ConsumeRight();
    void ConsumeX1();
    void ConsumeX2();
    bool Held(int button);
    bool HeldOrClicked(int button);
    float HeldTime(int button);
    bool LeftDoubleClicked();
    bool RightClickedInPlace();
    
    /// <summary>
    /// Toggles mouse wrapping around screen borders.
    /// </summary>
    bool Wrap { get; set; }
    
    bool WrappedThisFrame { get; }
    
    void Update(float deltaSeconds);
}

public static class MouseInputExt {
    /// <summary>
    /// Gets the location of the mouse in the previous frame
    /// </summary>
    public static Point PrevPos(this IMouseInput mouse) => mouse.Pos - mouse.PositionDelta;
}