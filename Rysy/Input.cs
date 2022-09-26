namespace Rysy;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public static class Input
{
    internal static void Update(GameTime gameTime)
    {
        Mouse.UpdateMouseInput(gameTime);
        Keyboard.Update(gameTime);
    }

    public static class Mouse
    {
        public static int ScrollDelta { get; private set; }
        public static MouseInputState MouseLeft { get; private set; }
        public static MouseInputState Right { get; private set; }
        public static MouseInputState MouseX1 { get; private set; }

        public static MouseInputState MouseX2 { get; private set; }

        public static Point Pos { get; private set; }
        public static Point PositionDelta { get; private set; }

        public static float LeftHoldTime => HoldTimes[0];
        public static float RightHoldTime => HoldTimes[1];
        public static float X1HoldTime => HoldTimes[3];
        public static float X2HoldTime => HoldTimes[4];

        private static int lastMouseScroll;
        private static int realMouseScroll;
        // GetState storage
        private static MouseState mousePrevState, mouseState = new();
        private static float[] HoldTimes = new float[5];



        private static MouseInputState GetCorrectState(ButtonState current, ButtonState prev, int index, float timeDeltaSeconds)
        {
            if (current == ButtonState.Released)
            {
                HoldTimes[index] = 0f;
                return MouseInputState.Released;
            }
            else if (prev == ButtonState.Released)
            {
                HoldTimes[index] = 0f;
                return MouseInputState.Clicked;
            }
            else
            {
                HoldTimes[index] += timeDeltaSeconds;
                return MouseInputState.Held;
            }
        }

        public static void UpdateMouseInput(GameTime gameTime)
        {
            // From FNA wiki
            mousePrevState = mouseState;
            mouseState = Microsoft.Xna.Framework.Input.Mouse.GetState();
            var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Easiest route is to 'or' the click with the current state
            ButtonState leftButton = mouseState.LeftButton;
            ButtonState rightButton = mouseState.RightButton;
            ButtonState x1Button = mouseState.XButton1;
            ButtonState x2Button = mouseState.XButton2;

            MouseLeft = GetCorrectState(leftButton, mousePrevState.LeftButton, 0, delta);
            Right = GetCorrectState(rightButton, mousePrevState.RightButton, 1, delta);
            MouseX1 = GetCorrectState(x1Button, mousePrevState.XButton1, 3, delta);
            MouseX2 = GetCorrectState(x2Button, mousePrevState.XButton2, 4, delta);

            lastMouseScroll = realMouseScroll;
            realMouseScroll = mouseState.ScrollWheelValue;

            ScrollDelta = realMouseScroll - lastMouseScroll;

            var lastPos = Pos;
            Pos = new(mouseState.X, mouseState.Y);
            PositionDelta = Pos - lastPos;
        }
    }

    public static class Keyboard
    {
        private static KeyboardState LastState;

        private static Keys[] ClickedKeys = new Keys[32];

        private static Dictionary<Keys, float> HoldTimes = new();

        private static bool Contains(Keys[] keys, Keys key)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i] == key)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns whether a key has just been clicked this frame
        /// </summary>
        public static bool IsKeyClicked(Keys key) => Contains(ClickedKeys, key);
        public static bool IsKeyHeld(Keys key) => LastState.IsKeyDown(key);

        public static bool Ctrl() => LastState.IsKeyDown(Keys.LeftControl) || LastState.IsKeyDown(Keys.RightControl);
        public static bool Shift() => LastState.IsKeyDown(Keys.LeftShift) || LastState.IsKeyDown(Keys.RightShift);

        public static float GetHoldTime(Keys key)
        {
            if (IsKeyHeld(key))
            {
                return HoldTimes[key];
            }
            else
            {
                return 0f;
            }
        }


        public static void ConsumeKeyClick(Keys key)
        {
            var i = Array.IndexOf(ClickedKeys, key);
            ClickedKeys[i] = Keys.None;
        }

        public static void Setup()
        {

        }

        public static void Update(GameTime gameTime)
        {
            var state = Microsoft.Xna.Framework.Input.Keyboard.GetState();
            var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            var clickedKeyIndex = 0;
            Array.Clear(ClickedKeys, 0, ClickedKeys.Length);

            foreach (var key in state.GetPressedKeys())
            {
                if (!LastState.IsKeyDown(key) && clickedKeyIndex < ClickedKeys.Length)
                {
                    ClickedKeys[clickedKeyIndex++] = key;
                    HoldTimes[key] = 0;
                }
                else
                {
                    HoldTimes[key] = HoldTimes.TryGetValue(key, out var timer) ? timer + delta : delta;
                }


            }



            LastState = state;
        }
    }

}

public enum MouseInputState
{
    Released,
    Held,
    Clicked
}

public static class MouseInputStateExt
{
    public static bool Released(this MouseInputState m) => m == MouseInputState.Released;
    public static bool Held(this MouseInputState m) => m == MouseInputState.Held;
    public static bool Clicked(this MouseInputState m) => m == MouseInputState.Clicked;
}