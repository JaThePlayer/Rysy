using Microsoft.Xna.Framework.Input;

namespace Rysy {
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
}