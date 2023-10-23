namespace Rysy;

public interface IMouseInput {
    public Point Offset { get; set; }

    bool AnyClicked { get; }
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
    void Update(float deltaSeconds);
}