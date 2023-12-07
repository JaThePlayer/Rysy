namespace Rysy.Helpers; 

public class MouseDragGesture<T> where T : class, new() {
    private readonly Input _input;

    private bool _begun;

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
        
        if (!_begun) {
            StartingPos = null;
            
            if (_input.Mouse.Left.Clicked() || _forceBeginNextFrame) {
                _begun = true;
                Shift = _input.Keyboard.Shift();
                Data = new();
                continueStroke = true;
                _forceBeginNextFrame = false;
                StartingPos = _input.Mouse.Pos;
            }
        }

        if (!_begun) {
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
        _begun = false;
        _currentStrokeLastPos = null;
        Data = null;
    }
}