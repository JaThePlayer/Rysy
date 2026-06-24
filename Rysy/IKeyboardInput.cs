using Microsoft.Xna.Framework.Input;

namespace Rysy;

public interface IKeyboardInput {
    bool AnyKeyHeld { get; }

    bool Alt();
    void ConsumeKeyClick(Keys key);
    bool Ctrl();
    float GetHoldTime(Keys key);
    bool HeldOrClicked(Keys key);
    bool IsKeyClicked(Keys key);
    bool IsKeyHeld(Keys key);
    void Setup();
    bool Shift();
    void Update(float deltaSeconds);
}

public static class IKeyboardInputExt {
    extension(IKeyboardInput keyboardInput) {
        /// <summary>
        /// Whether the key has just been clicked, or held.
        /// If held, this function will return true every interval, with the interval decreasing the longer the key is held.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <param name="interval">The interval. Should start at 0, and get passed again to this function every frame.</param>
        public bool HeldOrClickedSmoothInterval(Keys key, ref float interval) {
            if (keyboardInput.IsKeyClicked(key)
                || (keyboardInput.GetHoldTime(key) > 0.2f && RysyEngine.Scene.OnInterval(interval))) {
                interval = NextInterval(interval);
                return true;
            }

            interval = 0f;
            return false;
        }
        
        private static float NextInterval(float holdTime) => 0.50f - (holdTime / 2.5f);
    }
}