namespace Rysy.Helpers; 

public class MouseDragGesture<T> where T : class, new() {
    private readonly Input _input;

    public bool Begun { get; private set; }

    private Point? _currentStrokeLastPos;

    private bool _forceBeginNextFrame;

    public bool Shift { get; private set; }

    public T? Data { get; private set; }

    public Point? StartingPos { get; private set; }

    public MouseDragGesture(Input input) {
        _input = input;
        CancelStroke();
    }

    public void Update(out bool continueStroke, out bool endStroke, out Point lastMousePos, out T? data) {
        continueStroke = false;
        endStroke = false;
        
        data = null;
        if (_input.Mouse.WrappedThisFrame) {
            // Update the last pos so that we don't make the consumer think the user moved their mouse through the entire screen in 1 frame
            _currentStrokeLastPos = _input.Mouse.Pos;
        }
        lastMousePos = _currentStrokeLastPos ?? _input.Mouse.Pos;
        
        if (!Begun) {
            StartingPos = null;
            
            if (_input.Mouse.Left.Clicked() || _forceBeginNextFrame) {
                Begun = true;
                Shift = _input.Keyboard.Shift();
                Data = new();
                continueStroke = true;
                _forceBeginNextFrame = false;
                StartingPos = _input.Mouse.Pos;
            }
        }

        if (!Begun) {
            return;
        }

        data = Data;
        
        if (Shift != _input.Keyboard.Shift()) {
            endStroke = true;
            CancelStroke();
            _forceBeginNextFrame = true;
            return;
        }

        if (_input.Mouse.Left.Released()) {
            endStroke = true;
            CancelStroke();
            return;
        }

        if (_input.Mouse.PositionDelta == Point.Zero) {
            return;
        }

        continueStroke = true;
        _currentStrokeLastPos = _input.Mouse.Pos;
    }
    
    public void CancelStroke() {
        Begun = false;
        _currentStrokeLastPos = null;
        Data = null;
    }
}