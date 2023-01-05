using Rysy.Graphics;

namespace Rysy.Scenes;

public class LoadingScene : Scene {
    public string Text;

    public void SetText(string text) {
        Text = text;
    }

    public override void Render() {
        base.Render();

        var windowSize = RysyEngine.Instance.Window.ClientBounds.Size;
        GFX.BeginBatch();
        PicoFont.Print("Rysy (dev)", new Rectangle(0, windowSize.Y / 4, windowSize.X, windowSize.Y / 2), Color.White, 4f);
        PicoFont.Print($"{TimeActive:0.00s}", new Rectangle(0, windowSize.Y / 3, windowSize.X, windowSize.Y / 2), Color.LightSkyBlue, 4f);
        PicoFont.Print(Text, new Rectangle(0, windowSize.Y / 2, windowSize.X, windowSize.Y / 2), Color.LightSkyBlue, 4f);
        GFX.EndBatch();
    }
}
