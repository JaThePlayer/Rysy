using Rysy.Gui;
using Rysy.Helpers;
using Microsoft.Xna.Framework.Input;

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

    internal static void UpdateGlobal(GameTime gameTime) 
        => Global.Update(gameTime);

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

        public float LeftHoldTime => HoldTimes[0];
        public float RightHoldTime => HoldTimes[1];
        public float X1HoldTime => HoldTimes[3];
        public float X2HoldTime => HoldTimes[4];

        public bool LeftDoubleClicked() => TimeSinceLastClick[0] < DOUBLE_CLICK_TIME && DoubleClicks[0];

        public bool RightClickedInPlace() => Right.Released() && mousePrevState.RightButton == ButtonState.Pressed &&
                ClickPositions[1] == RealPos;

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

        private int lastMouseScroll;
        private int realMouseScroll;
        // GetState storage
        private MouseState mousePrevState, mouseState = new();
        private float[] HoldTimes = new float[5];
        private bool[] ConsumedInputs = new bool[5];
        private float[] TimeSinceLastClick = new float[5];
        private bool[] DoubleClicks = new bool[5];
        private Point[] ClickPositions = new Point[5];

        private MouseInputState GetCorrectState(ButtonState current, ButtonState prev, int index, float timeDeltaSeconds) {
            if (PositionDelta != Point.Zero) {
                // if the mouse moves, cancel and prevent any double clicks
                DoubleClicks[index] = false;
                TimeSinceLastClick[index] = float.MaxValue;
            }

            if (current == ButtonState.Released) {
                HoldTimes[index] = 0f;
                ConsumedInputs[index] = false;
                TimeSinceLastClick[index] += timeDeltaSeconds;

                return MouseInputState.Released;
            }

            // Currently held/clicked

            if (ConsumedInputs[index]) {
                return MouseInputState.Released;
            }

            if (prev == ButtonState.Released) {
                // just clicked this frame
                DoubleClicks[index] = TimeSinceLastClick[index] < DOUBLE_CLICK_TIME;
                ClickPositions[index] = new Point(mouseState.X, mouseState.Y);
                HoldTimes[index] = 0f;
                TimeSinceLastClick[index] = 0f;
                return MouseInputState.Clicked;
            }

            HoldTimes[index] += timeDeltaSeconds;
            return MouseInputState.Held;
        }

        public void Update(float deltaSeconds) {
            WrappedThisFrame = false;

            TouchpadPan = RysyState.TouchpadPan;
            RysyState.TouchpadPan = default;
            
            // From FNA wiki
            mousePrevState = mouseState;
            mouseState = Microsoft.Xna.Framework.Input.Mouse.GetState();
            var delta = deltaSeconds;

            lastMouseScroll = realMouseScroll;
            realMouseScroll = mouseState.ScrollWheelValue;

            ScrollDelta = TouchpadPan == default ? (realMouseScroll - lastMouseScroll) : default;
            
            var viewport = RysyState.GraphicsDevice.Viewport;
            var lastPos = RealPos;
            RealPos = new(mouseState.X, mouseState.Y);
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
            
            var canInput = viewport.Bounds.Contains(new Point(mouseState.X, mouseState.Y));

            // Easiest route is to 'or' the click with the current state
            ButtonState leftButton = canInput ? mouseState.LeftButton : ButtonState.Released;
            ButtonState rightButton = canInput ? mouseState.RightButton : ButtonState.Released;
            ButtonState middleButton = canInput ? mouseState.MiddleButton : ButtonState.Released;
            ButtonState x1Button = canInput ? mouseState.XButton1 : ButtonState.Released;
            ButtonState x2Button = canInput ? mouseState.XButton2 : ButtonState.Released;

            Left = GetCorrectState(leftButton, mousePrevState.LeftButton, 0, delta);
            Right = GetCorrectState(rightButton, mousePrevState.RightButton, 1, delta);
            Middle = GetCorrectState(middleButton, mousePrevState.MiddleButton, 2, delta);
            X1 = GetCorrectState(x1Button, mousePrevState.XButton1, 3, delta);
            X2 = GetCorrectState(x2Button, mousePrevState.XButton2, 4, delta);
        }

        public void ConsumeLeft() {
            Left = MouseInputState.Released;
            HoldTimes[0] = 0f;
            ConsumedInputs[0] = true;
        }

        public void ConsumeRight() {
            Right = MouseInputState.Released;
            HoldTimes[1] = 0f;
            ConsumedInputs[1] = true;
        }

        public void ConsumeMiddle() {
            Right = MouseInputState.Released;
            HoldTimes[2] = 0f;
            ConsumedInputs[2] = true;
        }

        public void ConsumeX1() {
            X1 = MouseInputState.Released;
            HoldTimes[3] = 0f;
            ConsumedInputs[3] = true;
        }

        public void ConsumeX2() {
            X2 = MouseInputState.Released;
            HoldTimes[4] = 0f;
            ConsumedInputs[4] = true;
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
            return HoldTimes[button];
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
        private KeyboardState LastState;

        private Keys[] ClickedKeys = new Keys[32];

        private Dictionary<Keys, float> HoldTimes = new();

        private bool Contains(Keys[] keys, Keys key) {
            for (int i = 0; i < keys.Length; i++) {
                if (keys[i] == key)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns whether a key has just been clicked this frame
        /// </summary>
        public bool IsKeyClicked(Keys key) => Contains(ClickedKeys, key); //HoldTimes.TryGetValue(key, out var time) && time < 1f / 60f;
        public bool IsKeyHeld(Keys key) => HoldTimes.TryGetValue(key, out var timer) && timer > 0f
            && LastState.IsKeyDown(key) && !IsKeyClicked(key);
        public bool HeldOrClicked(Keys key) => LastState.IsKeyDown(key);

        public bool Ctrl() => IsKeyHeld(Keys.LeftControl) || IsKeyHeld(Keys.RightControl);//LastState.IsKeyDown(Keys.LeftControl) || LastState.IsKeyDown(Keys.RightControl);
        public bool Shift() => LastState.IsKeyDown(Keys.LeftShift) || LastState.IsKeyDown(Keys.RightShift);
        public bool Alt() => LastState.IsKeyDown(Keys.LeftAlt) || LastState.IsKeyDown(Keys.RightAlt);

        public bool AnyKeyHeld { get; private set; }

        public float GetHoldTime(Keys key) {
            if (IsKeyHeld(key)) {
                return HoldTimes[key];
            } else {
                return 0f;
            }
        }


        public void ConsumeKeyClick(Keys key) {
            var i = Array.IndexOf(ClickedKeys, key);
            if (i != -1)
                ClickedKeys[i] = Keys.None;
        }

        public void Setup() {

        }

        public void Update(float deltaSeconds) {
            var state = Microsoft.Xna.Framework.Input.Keyboard.GetState();
            var delta = deltaSeconds;

            var clickedKeyIndex = 0;
            Array.Clear(ClickedKeys, 0, ClickedKeys.Length);

            var keys = state.GetPressedKeys();
            AnyKeyHeld = keys.Length > 0;

            foreach (var key in keys) {
                if (!LastState.IsKeyDown(key) && clickedKeyIndex < ClickedKeys.Length) {
                    ClickedKeys[clickedKeyIndex++] = key;
                    HoldTimes[key] = 0;
                } else {
                    HoldTimes[key] = HoldTimes.TryGetValue(key, out var timer) ? timer + delta : delta;
                }
            }

            LastState = state;
        }
    }

    public static class Clipboard {
        public static void Set(string text) {
            SDL2.SDL.SDL_SetClipboardText(text);
        }

        public static void SetAsJson<T>(T obj) {
            Set(obj.ToJson(Settings.Instance.MinifyClipboard));
        }

        public static string Get() => SDL2Ext.GetClipboardFixed();

        public static T? TryGetFromJson<T>() => JsonExtensions.TryDeserialize<T>(Get());
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