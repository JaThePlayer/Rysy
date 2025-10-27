using Rysy.Gui;
using Rysy.Helpers;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Rysy;

public class Input {
    public static Input Global { get; private set; } = new() { Mouse = { Wrap = true } };
    public IMouseInput Mouse { get; private set; }
    public IKeyboardInput Keyboard { get; private set; }

    public Input(IMouseInput? mouse = null, IKeyboardInput? keyboard = null) {
        Mouse = mouse ?? new MouseInput();
        Keyboard = keyboard ?? new KeyboardInput();
    }

    //public static IMouseInput Mouse { get; private set; } = new MouseInput();
    //public static IKeyboardInput Keyboard { get; private set; } = new KeyboardInput();

    public void Update(GameTime gameTime) => Update((float) gameTime.ElapsedGameTime.TotalSeconds);

    public void Update(float deltaSeconds) {
        Mouse.Update(deltaSeconds);
        Keyboard.Update(deltaSeconds);
    }

    public class MouseInput : IMouseInput {
        public const float DOUBLE_CLICK_TIME = .3f;

        public int ScrollDelta { get; private set; }
        public MouseInputState Left { get; private set; }
        public MouseInputState Right { get; private set; }
        public MouseInputState Middle { get; private set; }
        public MouseInputState X1 { get; private set; }
        public MouseInputState X2 { get; private set; }
        
        public bool WrappedThisFrame { get; private set; }

        private Point RealPos { get; set; }

        public Point Offset { get; set; } = new();

        public Point Pos => RealPos + Offset;
        public Point PositionDelta { get; private set; }
        
        public Vector2 TouchpadPan { get; private set; }

        public float LeftHoldTime => _holdTimes[0];
        public float RightHoldTime => _holdTimes[1];
        public float X1HoldTime => _holdTimes[3];
        public float X2HoldTime => _holdTimes[4];

        public bool LeftDoubleClicked() => /*_timeSinceLastClick[0] < DOUBLE_CLICK_TIME &&*/ _doubleClicks[0];

        public bool RightClickedInPlace() => Right.Released() && _mousePrevState.RightButton == ButtonState.Pressed &&
                _clickPositions[1] == RealPos;

        public bool AnyClickedOrHeld =>
            Left is not MouseInputState.Released ||
            Right is not MouseInputState.Released ||
            Middle is not MouseInputState.Released ||
            X1 is not MouseInputState.Released ||
            X2 is not MouseInputState.Released;
        
        /// <summary>
        /// Toggles mouse wrapping around screen borders.
        /// </summary>
        public bool Wrap { get; set; }

        private int _lastMouseScroll;
        private int _realMouseScroll;
        // GetState storage
        private MouseState _mousePrevState, _mouseState = new();
        private readonly float[] _holdTimes = new float[5];
        private readonly bool[] _consumedInputs = new bool[5];
        private readonly float[] _timeSinceLastClick = new float[5];
        private readonly bool[] _doubleClicks = new bool[5];
        private readonly Point[] _clickPositions = new Point[5];

        private MouseInputState GetCorrectState(ButtonState current, ButtonState prev, int index, float timeDeltaSeconds) {
            _doubleClicks[index] = !_consumedInputs[index] && (DateTime.Now - RysyState.MouseDoubleClicks[index]).TotalSeconds <= DOUBLE_CLICK_TIME;
            if (_doubleClicks[index]) {
              //  RysyState.MouseDoubleClicks[index] = default;
            }
            
            if (PositionDelta != Point.Zero) {
                // if the mouse moves, cancel and prevent any double clicks
                //RysyState.MouseDoubleClicks[index] = default;
                _doubleClicks[index] = false;
                _timeSinceLastClick[index] = float.MaxValue;
            }

            if (current == ButtonState.Released) {
                _holdTimes[index] = 0f;
                _consumedInputs[index] = false;
                _timeSinceLastClick[index] += timeDeltaSeconds;

                return MouseInputState.Released;
            }

            // Currently held/clicked

            if (_consumedInputs[index]) {
                RysyState.MouseDoubleClicks[index] = default;
                return MouseInputState.Released;
            }

            if (prev == ButtonState.Released) {
                _clickPositions[index] = new Point(_mouseState.X, _mouseState.Y);
                _holdTimes[index] = 0f;
                _timeSinceLastClick[index] = 0f;
                return MouseInputState.Clicked;
            }

            _holdTimes[index] += timeDeltaSeconds;
            return MouseInputState.Held;
        }

        public void Update(float deltaSeconds) {
            WrappedThisFrame = false;

            TouchpadPan = RysyState.TouchpadPan;
            RysyState.TouchpadPan = default;
            
            // From FNA wiki
            _mousePrevState = _mouseState;
            _mouseState = Microsoft.Xna.Framework.Input.Mouse.GetState();
            var delta = deltaSeconds;

            _lastMouseScroll = _realMouseScroll;
            _realMouseScroll = _mouseState.ScrollWheelValue;

            ScrollDelta = TouchpadPan == default ? (_realMouseScroll - _lastMouseScroll) : default;
            
            var viewport = RysyState.GraphicsDevice.Viewport;
            var lastPos = RealPos;
            RealPos = new(_mouseState.X, _mouseState.Y);
            PositionDelta = RealPos - lastPos;

            if (this.CanWrap() && AnyClickedOrHeld && !ImGuiManager.WantCaptureMouse) {
                var setPos = false;
                
                if (PositionDelta.X > 0 && RealPos.X >= viewport.Width - 3) {
                    RealPos = new(RealPos.X - viewport.Width, RealPos.Y);
                    setPos = true;
                    
                } else if (PositionDelta.X < 0 && RealPos.X <= 3) {
                    RealPos = new(viewport.Width - RealPos.X, RealPos.Y);
                    setPos = true;
                }
                
                if (PositionDelta.Y > 0 && RealPos.Y >= viewport.Height - 3) {
                    RealPos = new(RealPos.X, RealPos.Y  - viewport.Height);
                    setPos = true;
                } else if (PositionDelta.Y < 0 && RealPos.Y <= 1) {
                    RealPos = new(RealPos.X, viewport.Height - RealPos.Y);
                    setPos = true;
                }

                if (setPos) {
                    Microsoft.Xna.Framework.Input.Mouse.SetPosition(RealPos.X, RealPos.Y);
                    WrappedThisFrame = true;
                }
            }
            
            var canInput = viewport.Bounds.Contains(new Point(_mouseState.X, _mouseState.Y));

            // Easiest route is to 'or' the click with the current state
            ButtonState leftButton = canInput ? _mouseState.LeftButton : ButtonState.Released;
            ButtonState rightButton = canInput ? _mouseState.RightButton : ButtonState.Released;
            ButtonState middleButton = canInput ? _mouseState.MiddleButton : ButtonState.Released;
            ButtonState x1Button = canInput ? _mouseState.XButton1 : ButtonState.Released;
            ButtonState x2Button = canInput ? _mouseState.XButton2 : ButtonState.Released;

            Left = GetCorrectState(leftButton, _mousePrevState.LeftButton, 0, delta);
            Right = GetCorrectState(rightButton, _mousePrevState.RightButton, 1, delta);
            Middle = GetCorrectState(middleButton, _mousePrevState.MiddleButton, 2, delta);
            X1 = GetCorrectState(x1Button, _mousePrevState.XButton1, 3, delta);
            X2 = GetCorrectState(x2Button, _mousePrevState.XButton2, 4, delta);

            //RysyState.MouseDoubleClicks.AsSpan().Clear();
        }

        public void ConsumeLeft() {
            Left = MouseInputState.Released;
            _holdTimes[0] = 0f;
            _consumedInputs[0] = true;
            _doubleClicks[0] = false;
        }

        public void ConsumeRight() {
            Right = MouseInputState.Released;
            _holdTimes[1] = 0f;
            _consumedInputs[1] = true;
        }

        public void ConsumeMiddle() {
            Right = MouseInputState.Released;
            _holdTimes[2] = 0f;
            _consumedInputs[2] = true;
        }

        public void ConsumeX1() {
            X1 = MouseInputState.Released;
            _holdTimes[3] = 0f;
            _consumedInputs[3] = true;
        }

        public void ConsumeX2() {
            X2 = MouseInputState.Released;
            _holdTimes[4] = 0f;
            _consumedInputs[4] = true;
        }

        public bool Clicked(int button) {
            return button switch {
                0 => Left is MouseInputState.Clicked,
                1 => Right is MouseInputState.Clicked,
                2 => Middle is MouseInputState.Clicked,
                3 => X1 is MouseInputState.Clicked,
                4 => X2 is MouseInputState.Clicked,
                _ => false,
            };
        }

        public bool Held(int button) {
            return button switch {
                0 => Left is MouseInputState.Held,
                1 => Right is MouseInputState.Held,
                2 => Middle is MouseInputState.Held,
                3 => X1 is MouseInputState.Held,
                4 => X2 is MouseInputState.Held,
                _ => false,
            };
        }

        public bool HeldOrClicked(int button) {
            return button switch {
                0 => Left is MouseInputState.Held or MouseInputState.Clicked,
                1 => Right is MouseInputState.Held or MouseInputState.Clicked,
                2 => Middle is MouseInputState.Held or MouseInputState.Clicked,
                3 => X1 is MouseInputState.Held or MouseInputState.Clicked,
                4 => X2 is MouseInputState.Held or MouseInputState.Clicked,
                _ => false,
            };
        }

        public float HeldTime(int button) {
            return _holdTimes[button];
        }

        public void Consume(int button) {
            switch (button) {
                case 0:
                    ConsumeLeft();
                    break;
                case 1:
                    ConsumeRight();
                    break;
                case 2:
                    ConsumeMiddle();
                    break;
                case 3:
                    ConsumeX1();
                    break;
                case 4:
                    ConsumeX2();
                    break;
                default:
                    break;
            }
        }
    }

    private sealed class KeyboardInput : IKeyboardInput {
        private KeyboardState _lastState;

        private readonly Keys[] _clickedKeys = new Keys[32];

        private readonly Dictionary<Keys, float> _holdTimes = new();

        /// <summary>
        /// Returns whether a key has just been clicked this frame
        /// </summary>
        public bool IsKeyClicked(Keys key) => _clickedKeys.Contains(key); //HoldTimes.TryGetValue(key, out var time) && time < 1f / 60f;
        public bool IsKeyHeld(Keys key) => _holdTimes.TryGetValue(key, out var timer) && timer > 0f
            && _lastState.IsKeyDown(key) && !IsKeyClicked(key);
        public bool HeldOrClicked(Keys key) => _lastState.IsKeyDown(key);

        public bool Ctrl() => IsKeyHeld(Keys.LeftControl) || IsKeyHeld(Keys.RightControl);//LastState.IsKeyDown(Keys.LeftControl) || LastState.IsKeyDown(Keys.RightControl);
        public bool Shift() => _lastState.IsKeyDown(Keys.LeftShift) || _lastState.IsKeyDown(Keys.RightShift);
        public bool Alt() => _lastState.IsKeyDown(Keys.LeftAlt) || _lastState.IsKeyDown(Keys.RightAlt);

        public bool AnyKeyHeld { get; private set; }

        public float GetHoldTime(Keys key) {
            if (IsKeyHeld(key)) {
                return _holdTimes[key];
            }

            return 0f;
        }


        public void ConsumeKeyClick(Keys key) {
            var i = Array.IndexOf(_clickedKeys, key);
            if (i != -1)
                _clickedKeys[i] = Keys.None;
        }

        public void Setup() {

        }

        public void Update(float deltaSeconds) {
            var state = Microsoft.Xna.Framework.Input.Keyboard.GetState();
            var delta = deltaSeconds;

            var clickedKeyIndex = 0;
            Array.Clear(_clickedKeys, 0, _clickedKeys.Length);

            var keys = state.GetPressedKeys();
            AnyKeyHeld = keys.Length > 0;

            foreach (var key in keys) {
                if (!_lastState.IsKeyDown(key) && clickedKeyIndex < _clickedKeys.Length) {
                    _clickedKeys[clickedKeyIndex++] = key;
                    _holdTimes[key] = 0;
                } else {
                    _holdTimes[key] = _holdTimes.TryGetValue(key, out var timer) ? timer + delta : delta;
                }
            }

            _lastState = state;
        }
    }

    public static class Clipboard {
        public static void Set(string text) {
            SDL2.SDL.SDL_SetClipboardText(text);
        }

        public static void SetAsJson<T>(T obj, JsonSerializerOptions? options = null) {
            Set(obj.ToJson(options, Settings.Instance.MinifyClipboard));
        }

        public static string Get() => SDL2Ext.GetClipboardFixed();

        public static bool TryGetFromJson<T>([NotNullWhen(true)] out T? res, JsonSerializerOptions? options = null)
            => JsonExtensions.TryDeserialize(Get(), out res, options);
    }
}

public enum MouseInputState {
    Released,
    Held,
    Clicked
}

public static class MouseInputStateExt {
    public static bool Released(this MouseInputState m) => m == MouseInputState.Released;
    public static bool Held(this MouseInputState m) => m == MouseInputState.Held;
    public static bool Clicked(this MouseInputState m) => m == MouseInputState.Clicked;
}